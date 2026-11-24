using System.Data;
using System.Reflection;
using Editors.AnimationMeta.SuperView.Editing;
using Editors.AnimationMeta.SuperView.Visualisation.Instances;
using Editors.AnimationMeta.SuperView.Visualisation.Rules;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;

namespace Editors.AnimationMeta.SuperView.Visualisation
{
    public interface IMetaDataBuilder
    {
        // Off, an EFFECT tag draws its locator and no particles.
        bool ShowVfx { get; set; }
        // The battle entity's vfx_filter_id: an EFFECT tag whose id is neither 0 nor this is not drawn. Unset, the lowest id the metas carry.
        int? VfxFilterId { get; set; }
        // As a unit seen from far away: EFFECT tags marked Distance Culled are not drawn.
        bool HideDistanceCulled { get; set; }
        // A spell's wind-up vfx (special_ability_displays wind_up_aura_vfx / wind_up_weapon_vfx), which the game plays at the
        // placeholder EFFECT tags of type ability_aura (1) and ability_weapon (2) in cast animations, named as under vfx/.
        // Unset, those tags draw the vfx they name.
        string? AuraVfx { get; set; }
        string? WeaponVfx { get; set; }
        /// <summary>Lets go of what was loaded from these pack paths, so the next Create reads them again.</summary>
        void Forget(IEnumerable<string> paths);
        /// <summary>What the last Create could not draw, tag by tag.</summary>
        IReadOnlyList<MetaDataBuildProblem> Problems { get; }
        List<IMetaDataInstance> Create(ParsedMetadataFile? persistent, ParsedMetadataFile? metaData, ParsedMetadataAttribute? selectedMetaDataAttribute, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment);
    }

    // IDisposable so the editor's scope frees the effects' textures when SuperView closes.
    public class MetaDataBuilder : IMetaDataBuilder, IDisposable
    {
        private readonly ILogger _logger = Logging.Create<MetaDataBuilder>();
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly RenderEngineComponent _resourceLibrary;
        private readonly ResourceLibrary _shaders;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IPackFileService _packFileService;
        private readonly VfxTextureCache _vfxTextures;
        private readonly VfxMeshCache _vfxMeshes;

        // Every edit in the meta panel rebuilds every effect. What a tag's vfx file resolves to is kept until Forget names a
        // changed file; and as an effect re-simulates up to now it asks its bone for every step, each a sample of the whole
        // skeleton, which effects asking the same time share. 55 effects on a Bale Taurus took 1.3 s to rebuild.
        readonly Dictionary<(string Name, int Seed), VfxResolveResult> _resolved = [];
        readonly Dictionary<float, Matrix[]> _bones = [];
        (AnimationClip? Clip, GameSkeleton? Skeleton) _bonesOf;

        // A slider in the meta panel rebuilds on every step, and building every effect and prop again still took 220 ms of
        // each on that Taurus. One whose tag values, colour, vfx toggle, clip and skeleton are as they were, and which is still
        // in the scene, is kept as built; the caller cleans up only what Create no longer returns.
        record KeptAs(string Values, bool Picked, bool ShowVfx, string? AuraVfx, string? WeaponVfx, AnimationClip? Clip, GameSkeleton? Skeleton);
        record Kept(KeptAs As, ISceneNode Node, IMetaDataInstance Instance, MetaDataBuildProblem[] Problems);
        Dictionary<ParsedMetadataAttribute, Kept> _kept = new(ReferenceEqualityComparer.Instance);
        Dictionary<ParsedMetadataAttribute, Kept> _previous = new(ReferenceEqualityComparer.Instance);
        static readonly Dictionary<Type, PropertyInfo[]> s_tagValues = [];
        private readonly AnimationsContainerComponent _animationsContainerComponent;

        private static Color s_color = Color.Black;
        private static Color s_selectedColor = Color.Red;

        public bool ShowVfx { get; set; } = true;
        public int? VfxFilterId { get; set; }
        public bool HideDistanceCulled { get; set; }
        public string? AuraVfx { get; set; }
        public string? WeaponVfx { get; set; }

        readonly List<MetaDataBuildProblem> _problems = [];
        public IReadOnlyList<MetaDataBuildProblem> Problems => _problems;

        // A broken effect would log again on every rebuild (every click in the tag list); once is enough.
        private static readonly HashSet<string> s_loggedVfxMessages = [];

        public MetaDataBuilder(ComplexMeshLoader complexMeshLoader,
            RenderEngineComponent resourceLibrary,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            IPackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent,
            ResourceLibrary shaders)
        {
            _shaders = shaders;
            _complexMeshLoader = complexMeshLoader;
            _resourceLibrary = resourceLibrary;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
            _vfxTextures = new VfxTextureCache(ParticleRenderItem.PackTextureLoader(packFileService, LogOnce));
            _vfxMeshes = new VfxMeshCache(LoadRigid, VfxMeshCache.PackAnimationLoader(packFileService, LogOnce), LogOnce);
        }

        public void Dispose()
        {
            _vfxTextures.Dispose();
            _vfxMeshes.Dispose();
        }

        // A rigid particle's model, through the scene's mesh loader; null (logged once) when it is missing or broken.
        SceneNode? LoadRigid(string path)
        {
            if (_packFileService.FindFile(path) is not { } file)
            {
                LogOnce($"rigid model {path} is not in the loaded packs");
                return null;
            }
            foreach (var shader in VfxMeshCache.CustomShaders(_packFileService, path))
                LogOnce($"rigid model {path} names {shader}, one of CA's own shaders the preview cannot run: drawn as a plain lit mesh");
            try
            {
                return _complexMeshLoader.Load(file, new AnimationPlayer(), onlyLoadRootNode: true, onlyLoadFirstMesh: false);
            }
            catch (Exception e)
            {
                LogOnce($"rigid model {path} did not load: {e.Message}");
                return null;
            }
        }

        public void Forget(IEnumerable<string> paths)
        {
            // An effect resolves through its file's references too, so any changed vfx file may change any of them.
            _resolved.Clear();
            _bones.Clear();
            _kept.Clear();
            foreach (var path in paths)
            {
                _vfxTextures.Forget(path);
                _vfxMeshes.Forget(path);
            }
        }

        public List<IMetaDataInstance> Create(ParsedMetadataFile? persistent, 
            ParsedMetadataFile? metaData, ParsedMetadataAttribute? selectedMetaDataAttribute,
            SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment)
        {
            var output = new List<IMetaDataInstance>();
            (_previous, _kept) = (_kept, _previous);
            _kept.Clear();
            _problems.Clear();

            // Apply persistent meta data, if no disable is given.
            var usePersistent = metaData == null || metaData.GetItemsOfType<DisablePersistant_v10>().Count == 0;
            // DISABLE_PERSISTENT_VFX drops the persistent meta's effects and keeps the rest of it.
            var usePersistentVfx = metaData == null || metaData.GetItemsOfType<DisablePersistantVfx_v10>().Count == 0;
            var vfxFilter = VfxFilterId ?? VfxFilterIds(persistent, metaData).FirstOrDefault();
            if (usePersistent)
            {
                var metaDataPersistent = ApplyMetaData(persistent, selectedMetaDataAttribute, root, skeleton, rootPlayer, fragment, vfxFilter, usePersistentVfx, persistent: true);
                output.AddRange(metaDataPersistent);
            }

            var metaDataInstances = ApplyMetaData(metaData, selectedMetaDataAttribute,root, skeleton, rootPlayer, fragment, vfxFilter);
            output.AddRange(metaDataInstances);
            return output;
        }

        // The vfx filter ids the two metas' EFFECT tags carry, lowest first, 0 (every entity) left out.
        public static List<int> VfxFilterIds(ParsedMetadataFile? persistent, ParsedMetadataFile? metaData) =>
            [.. new[] { persistent, metaData }.SelectMany(x => x?.GetItemsOfType<IEffectMeta>() ?? []).Select(FilterOf).Where(x => x != 0).Distinct().Order()];

        // EFFECT v11 and v12 carry the id; the older versions have none and play on every entity.
        static int FilterOf(IEffectMeta effect) => effect is DecodedMetaEntryBase tag ? tag.Id : 0;

        // Distance Culled is on EFFECT v7 to v12.
        static bool IsDistanceCulled(IEffectMeta effect) => effect switch
        {
            Effect_v7 v => v.DistanceCulled,
            Effect_v10 v => v.DistanceCulled,
            Effect_v11 v => v.DistanceCulled,
            Effect_v12 v => v.DistanceCulled,
            _ => false,
        };

        // The vfx the tag plays: its own, or for a wind-up placeholder (EFFECT v11 and v12, type 1 or 2) the spell vfx set in its place.
        string VfxNameOf(IEffectMeta effect)
        {
            var type = effect switch { Effect_v11 v => v.EffectType, Effect_v12 v => v.EffectType, _ => 0 };
            return (type == 1 ? AuraVfx : type == 2 ? WeaponVfx : null) ?? effect.VfxName;
        }

        private IEnumerable<IMetaDataInstance> ApplyMetaData(ParsedMetadataFile? file, ParsedMetadataAttribute? selectedAttribute, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment, int vfxFilter, bool includeEffects = true, bool persistent = false)
        {
            var output = new List<IMetaDataInstance>();
            if (file == null)
                return output;

            output.AddRange(file.GetItemsOfType<IAnimatedPropMeta>().Select(x => Keep((ParsedMetadataAttribute)x, selectedAttribute, root, skeleton, rootPlayer, () => CreateAnimatedProp(x, root, skeleton, selectedAttribute, rootPlayer))).Where(x => x != null));

            // PROP: a model on a bone, drawn the way ANIMATED_PROP is.
            output.AddRange(file.Attributes.OfType<ParsedMetadataAttribute>().Where(x => x.Name == "PROP")
                .Select(x => Keep(x, selectedAttribute, root, skeleton, rootPlayer, () => CreateStaticProp(x, root, skeleton, selectedAttribute, rootPlayer))).Where(x => x != null));

            output.AddRange(file.GetItemsOfType<ImpactPosition_v10>().Select(meteDataItem => Guard(meteDataItem, root, () => CreateStaticLocator(meteDataItem, root, () => meteDataItem.Position, "ImpactPos", selectedAttribute))).Where(x => x != null));

            output.AddRange(file.GetItemsOfType<TargetPos_10>().Select(meteDataItem => Guard(meteDataItem, root, () => CreateStaticLocator(meteDataItem, root, () => meteDataItem.Position, "TargetPos", selectedAttribute))).Where(x => x != null));

            output.AddRange(file.GetItemsOfType<FirePos_v10>().Select(meteDataItem => Guard(meteDataItem, root, () => CreateStaticLocator(meteDataItem, root, () => meteDataItem.Position, "FirePos", selectedAttribute))).Where(x => x != null));

            output.AddRange(file.GetItemsOfType<SplashAttack_v10>().Select(meteDataItem => Guard(meteDataItem, root, () => CreateSplashAttack(meteDataItem, root, $"SplashAttack_{Math.Round(meteDataItem.EndTime, 2)}", 0.1f, selectedAttribute))).Where(x => x != null));

            // BLOOD, CAMERA_SHAKE_POS, CREW_LOCATION, SOUND_TRIGGER, SOUND_BUILDING and TRANSFORM: a marker at the tag's
            // position, on its bone where the tag names one.
            output.AddRange(file.Attributes.OfType<ParsedMetadataAttribute>()
                .Select(x => SpatialMetaDataCatalog.TryCreate(x, out var binding) && binding.UsesGenericMarker && binding.Kind != SpatialMetaDataKind.Prop ? binding : null)
                .Where(x => x != null)
                .Select(x => Guard(x!.Source, root, () => CreateSpatialMarker(x!, root, skeleton, selectedAttribute))).Where(x => x != null));

            if (includeEffects)
                output.AddRange(file.GetItemsOfType<IEffectMeta>().Where(x => FilterOf(x) == 0 || FilterOf(x) == vfxFilter).Where(x => !(HideDistanceCulled && IsDistanceCulled(x))).Select(x => Keep((ParsedMetadataAttribute)x, selectedAttribute, root, skeleton, rootPlayer, () => CreateEffect(x, root, skeleton, selectedAttribute, rootPlayer, persistent))).Where(x => x != null));

            foreach (var meteDataItem in file.GetItemsOfType<DockEquipment>())
                Guard(meteDataItem, null, () => { CreateEquipmentDock(meteDataItem, fragment, skeleton, rootPlayer); return (object?)null; });

            foreach (var meteDataItem in file.GetItemsOfType<Transform_v10>())
                CreateTransform(meteDataItem, rootPlayer);

            return output;
        }

        void Problem(ParsedMetadataAttribute tag, string reasonKey, string detail = "") => _problems.Add(new MetaDataBuildProblem(tag, reasonKey, detail));

        // One tag that will not draw is a problem on that tag, and what it had added to the scene is taken out again; the
        // rest of the scene is still built.
        T? Guard<T>(ParsedMetadataAttribute tag, SceneNode? root, Func<T?> build) where T : class
        {
            var before = root?.Children.Count ?? 0;
            try
            {
                return build();
            }
            catch (Exception e)
            {
                while (root != null && root.Children.Count > before)
                    root.RemoveObject(root.Children[^1]);
                LogOnce($"{tag.DisplayName}: {e.Message}");
                Problem(tag, "SuperView.Problems.PreviewFailed", e.Message);
                return null;
            }
        }

        private void CreateTransform(Transform_v10 transform, AnimationPlayer rootPlayer)
        {
            var rule = new TransformBoneRule(transform);
            rootPlayer.AnimationRules.Add(rule);
        }

        private void CreateEquipmentDock(DockEquipment metaData, IAnimationBinGenericFormat fragment, ISkeletonProvider skeleton, AnimationPlayer rootPlayer)
        {
            if (skeleton.Skeleton == null)
                return;
            if (fragment == null)
            {
                Problem(metaData, "SuperView.Problems.NoAnimationBin");
                return;
            }

            var animPath = fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName)?.AnimationFile ??
                           fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName + "_2")?.AnimationFile;
            if (animPath == null)
            {
                Problem(metaData, "SuperView.Problems.MissingDockSlot", metaData.AnimationSlotName);
                return;
            }

            var finalBoneIndex = -1;
            foreach (var potentialBoneName in metaData.SkeletonNameAlternatives)
            {
                finalBoneIndex = skeleton.Skeleton.GetBoneIndexByName(potentialBoneName);
                if (finalBoneIndex != -1)
                    break;
            }

            if (finalBoneIndex == -1)
            {
                Problem(metaData, "SuperView.Problems.MissingDockBone", string.Join(", ", metaData.SkeletonNameAlternatives));
                return;
            }

            var pf = _packFileService.FindFile(animPath);
            if (pf == null)
            {
                Problem(metaData, "SuperView.Problems.MissingAnimation", animPath);
                return;
            }

            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile, skeleton.Skeleton);

            var rule = new DockEquipmentRule(finalBoneIndex, metaData.PropBoneId, clip, skeleton, metaData.StartTime, metaData.EndTime);
            rootPlayer.AnimationRules.Add(rule);
        }

        private IMetaDataInstance? CreateAnimatedProp(IAnimatedPropMeta animatedPropMeta, SceneNode root, ISkeletonProvider rootSkeleton, ParsedMetadataAttribute? selectedMetaDataAttribute, AnimationPlayer rootPlayer) =>
            CreateProp((ParsedMetadataAttribute)animatedPropMeta, "Animated_prop", animatedPropMeta.ModelName, animatedPropMeta.AnimationName, animatedPropMeta.Scale, root, rootSkeleton, selectedMetaDataAttribute, rootPlayer);

        // PROP v2 to v15 name their model and animation alike; Scale is on v13 and later.
        private IMetaDataInstance? CreateStaticProp(ParsedMetadataAttribute prop, SceneNode root, ISkeletonProvider rootSkeleton, ParsedMetadataAttribute? selectedMetaDataAttribute, AnimationPlayer rootPlayer)
        {
            string? Text(string name) => prop.GetType().GetProperty(name)?.GetValue(prop) as string;
            var scale = prop.GetType().GetProperty("Scale")?.GetValue(prop) is float value ? value : 1;
            return CreateProp(prop, "Prop", Text("ModelName") ?? "", Text("AnimationName") ?? "", scale, root, rootSkeleton, selectedMetaDataAttribute, rootPlayer);
        }

        private IMetaDataInstance? CreateProp(ParsedMetadataAttribute tag, string propName, string modelName, string animationName, float scale, SceneNode root, ISkeletonProvider rootSkeleton, ParsedMetadataAttribute? selectedMetaDataAttribute, AnimationPlayer rootPlayer)
        {
            var color = selectedMetaDataAttribute == tag ? s_selectedColor : s_color;
            // Where the prop sits: its bone, and its position and orientation on it, read from the tag as it is edited.
            if (!SpatialMetaDataCatalog.TryCreate(tag, out var binding))
                return null;

            var meshPath = _packFileService.FindFile(modelName);
            if (meshPath == null)
            {
                Problem(tag, "SuperView.Problems.MissingModel", modelName);
                return null;
            }

            // A prop whose animation or skeleton is missing is still drawn, at the tag, without its own animation.
            var animationPath = string.IsNullOrWhiteSpace(animationName) ? null : _packFileService.FindFile(animationName);
            if (animationPath == null && !string.IsNullOrWhiteSpace(animationName))
                Problem(tag, "SuperView.Problems.MissingPropAnimation", animationName);
            var propPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            // Configure the mesh
            var loadedNode = _complexMeshLoader.Load(meshPath, new GroupNode(propName), propPlayer, true, true); 

            // Configure animation
            var skeletonName = animationPath == null ? null : SceneNodeHelper.GetSkeletonName(loadedNode);
            var skeletonFile = skeletonName == null ? null : _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(skeletonName);
            if (skeletonName != null && skeletonFile == null)
            {
                Problem(tag, "SuperView.Problems.MissingPropSkeleton", skeletonName);
                animationPath = null;
            }

            if (animationPath != null && skeletonFile != null)
            {
                var skeleton = new GameSkeleton(skeletonFile, propPlayer);
                var animFile = AnimationFile.Create(animationPath);
                var clip = new AnimationClip(animFile, skeleton);
                propPlayer.SetAnimation(clip, skeleton);

                // Add the prop skeleton
                var skeletonSceneNode = new SkeletonNode(skeleton)
                {
                    NodeColour = color,
                    ScaleMult = scale
                };
                loadedNode.AddObject(skeletonSceneNode);
            }

            // Configure scale
            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is ISelectable selectableNode)
                    selectableNode.IsSelectable = false;

                if (node is SceneNode sceneNode)
                    sceneNode.ScaleMult = scale;
            });
            loadedNode.ScaleMult = scale;

            // A prop with its own animation has its root bone put on the tag's bone by the player's rule; one without
            // has no frames for a rule to act on, so the instance places its node.
            var placedByAnimation = animationPath != null;
            if (placedByAnimation)
            {
                var animationRule = new CopyRootTransform(rootSkeleton, binding.BoneIndex ?? -1, () => binding.Position, () => binding.Orientation ?? Quaternion.Identity);
                propPlayer.AnimationRules.Add(animationRule);
            }
            if(rootPlayer.IsPlaying)
                propPlayer.Play();
            propPlayer.Refresh();

            // Add to scene
            root.AddObject(loadedNode);

            var instance = new AnimatedPropInstance(loadedNode, propPlayer, binding, rootSkeleton, placedByAnimation);
            instance.Update(rootPlayer.GetTimeUs() / 1_000_000f);
            return instance;
        }

        private IMetaDataInstance CreateStaticLocator(DecodedMetaEntryBase metaData, SceneNode root, Func<Vector3> positionOf, string displayName, ParsedMetadataAttribute? selectedMetaDataAttribute, float scale = 0.3f)
        {
            var color = selectedMetaDataAttribute == metaData ? s_selectedColor : s_color;
            var position = positionOf();

            var node = new SimpleDrawableNode(displayName);
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, displayName, position, color));
            node.AddItem(LineHelper.AddCircle(position, scale, color));
            root.AddObject(node);

            return new DrawableMetaInstance(metaData, node, () => [new(positionOf(), MetaDataMarkerPoint.Default)]);
        }

        // A tag's name and a cross at its position; one that can turn also gets its X, Y and Z axes. Drawn in the tag's
        // frame, and placed from its values on its bone on every update.
        private IMetaDataInstance CreateSpatialMarker(SpatialMetaDataBinding binding, SceneNode root, ISkeletonProvider skeleton, ParsedMetadataAttribute? selectedAttribute)
        {
            const float Scale = 0.2f;
            var color = selectedAttribute == binding.Source ? s_selectedColor : s_color;
            var node = new SimpleDrawableNode(binding.Source.DisplayName);
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, binding.Source.DisplayName, Vector3.Zero, color));
            node.AddItem(LineHelper.AddLocator(Vector3.Zero, Scale, color));
            if (binding.CanRotate)
            {
                node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitX * Scale, Color.Red));
                node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitY * Scale, Color.Green));
                node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitZ * Scale, Color.Blue));
            }
            root.AddObject(node);

            Matrix Local() => Matrix.CreateFromQuaternion(binding.Orientation ?? Quaternion.Identity) * Matrix.CreateTranslation(binding.Position);
            var instance = new DrawableMetaInstance(binding.Source, node, () => [new(binding.Position, MetaDataMarkerPoint.Default)], Local);
            var bone = binding.BoneIndex ?? -1;
            if (binding.AttachToBone && bone >= 0 && bone < (skeleton.Skeleton?.BoneCount ?? 0))
                instance.FollowBone(skeleton, bone);
            instance.Update(0);
            return instance;
        }

        private IMetaDataInstance CreateSplashAttack(SplashAttack_v10 splashAttack, SceneNode root, string displayName, float scale, ParsedMetadataAttribute? selectedAttribute)
        {
            var distance = Vector3.Distance(splashAttack.StartPosition, splashAttack.EndPosition);
            if (MathUtil.CompareEqualFloats(distance))
                throw new ConstraintException($"{displayName}: the distance between StartPosition {splashAttack.StartPosition} and EndPosition {splashAttack.EndPosition} is close to 0");

            var color = selectedAttribute == splashAttack ? s_selectedColor : s_color;

            var node = new SimpleDrawableNode(displayName);
            var textPos = (splashAttack.EndPosition + splashAttack.StartPosition) / 2;
            
            node.AddItem( new WorldTextRenderItem(_resourceLibrary, "StartPos", splashAttack.StartPosition, color));
            node.AddItem(LineHelper.AddLocator(splashAttack.StartPosition, scale, color));
            
            node.AddItem( new WorldTextRenderItem(_resourceLibrary, "EndPos", splashAttack.EndPosition, color));
            node.AddItem(LineHelper.AddLocator(splashAttack.EndPosition, scale, color));
            
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, displayName, textPos, color));
            node.AddItem(LineHelper.AddLine(splashAttack.StartPosition, splashAttack.EndPosition, color));

            var normal = splashAttack.EndPosition - splashAttack.StartPosition;  // corresponds to Z
            normal.Normalize();
            var random = new Random(1);
            Func<Random, float> RandomFloat = r => (float)(2 * r.NextDouble() - 1);
            var vectorP = new Vector3(RandomFloat(random), RandomFloat(random), RandomFloat(random));
            vectorP.Normalize();

            var planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal)); // corresponds to X
            var planeVectorPN = Vector3.Cross(vectorP, normal); // corresponds to Y
            planeVectorP.Normalize();
            planeVectorPN.Normalize();

            var rotationM = MathUtil.CreateRotation(
            [
                planeVectorP,
                planeVectorPN,
                normal
            ]);

            if (splashAttack.AoeShape == 0) // Cone or Sphere
            {
                if (MathUtil.CompareEqualFloats(splashAttack.AngleForCone / 2, tolerance: 0.1f))
                {
                    throw new ConstraintException($"{displayName}: the half-angle {splashAttack.AngleForCone / 2} of the cone is close to 0");
                }
                var transformationM = rotationM * Matrix.CreateScale(distance) * Matrix.CreateTranslation(splashAttack.StartPosition);
                node.AddItem(LineHelper.AddConeSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, splashAttack.AngleForCone, color));
            }
            if (splashAttack.AoeShape == 1) // Corridor
            {
                if (MathUtil.CompareEqualFloats(splashAttack.WidthForCorridor, tolerance: 0.001f))
                {
                    throw new ConstraintException($"{displayName}: the WidthForCorridor {splashAttack.WidthForCorridor} of the corridor is close to 0");
                }
                var transformationM = rotationM * Matrix.CreateScale(splashAttack.WidthForCorridor / 2) * Matrix.CreateTranslation(splashAttack.StartPosition);
                node.AddItem(LineHelper.AddCorridorSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, color));
            }
            
            root.AddObject(node);

            return new DrawableMetaInstance(splashAttack, node, () =>
            [
                new(splashAttack.StartPosition, MetaDataMarkerPoint.SplashStart, scale * 2),
                new(splashAttack.EndPosition, MetaDataMarkerPoint.SplashEnd, scale * 2),
            ]);
        }

        private IMetaDataInstance CreateEffect(IEffectMeta effect, SceneNode root, ISkeletonProvider skeleton, ParsedMetadataAttribute? selectedAttribute, AnimationPlayer rootPlayer, bool persistent)
        {
            var vfxName = VfxNameOf(effect);
            var resolved = ShowVfx ? Resolve(effect, vfxName) : null;
            if (resolved is { Found: false })
            {
                LogOnce($"EFFECT {vfxName}: {VfxResolver.PathOf(vfxName)} is not in any loaded pack, so only its locator is drawn");
                Problem((ParsedMetadataAttribute)effect, "SuperView.Problems.MissingEffect", VfxResolver.PathOf(vfxName));
            }
            foreach (var problem in resolved?.Problems ?? [])
                LogOnce($"EFFECT {vfxName}: {problem}");

            var color = selectedAttribute == effect ? s_selectedColor : s_color;
            var locatorScale = 0.3f;
            var textOffset = locatorScale * 1.5f + 0.01f;

            var rotMatrix = Matrix.CreateFromQuaternion(new Quaternion(effect.Orientation));

            // Drawn in the tag's own frame: the instance places it from the tag's Position and Orientation on the bone.
            var node = new SimpleDrawableNode("Effect:" + vfxName);
            node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitX * locatorScale, Color.Red));
            node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitY * locatorScale, Color.Green));
            node.AddItem(LineHelper.AddLine(Vector3.Zero, Vector3.UnitZ * locatorScale, Color.Blue));

            var label = resolved is { Found: false } ? vfxName + " (not found)" : vfxName;
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, label, Vector3.Zero, color));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "X", Vector3.UnitX * textOffset, Color.Red));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "Y", Vector3.UnitY * textOffset, Color.Green));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "Z", Vector3.UnitZ * textOffset, Color.Blue));

            root.AddObject(node);

            // An untracked effect stays where its bone was at the tag's Start Time.
            var startTime = effect.EffectStartTime;
            var boneAt = BoneAt(effect.NodeIndex, skeleton, rootPlayer);
            if (!effect.Tracking)
            {
                var atStart = boneAt(startTime);
                boneAt = _ => atStart;
            }

            if (resolved is not { Found: true })
            {
                var locatorOnly = new VfxEffectInstance(node, boneAt, startTime, effect: effect);
                locatorOnly.Update(rootPlayer.GetTimeUs() / 1_000_000f);
                return locatorOnly;
            }

            var settings = resolved.Emitters.Select(EmitterSettings.From).ToList();
            foreach (var emitter in settings)
            {
                foreach (var gap in emitter.Skipped)
                    LogOnce($"EFFECT {vfxName}, emitter {emitter.Name}: {gap}");
            }

            var offset = Matrix.CreateScale(ScaleOf(effect)) * rotMatrix * Matrix.CreateTranslation(effect.Position);
            // A 0/0 tag (or one ending before it starts) spawns for the whole animation.
            var spawnEnd = effect.EffectEndTime > startTime ? effect.EffectEndTime - startTime : float.PositiveInfinity;
            // A persistent effect's clock runs past the clip's end; its bone is read where the looping clip is then.
            var loop = persistent ? rootPlayer.AnimationClip?.PlayTimeInSec ?? 0 : 0;
            float MainTime(float effectTime) => loop > 0 ? ((effectTime + startTime) % loop + loop) % loop : effectTime + startTime;
            var simulator = new ParticleSimulator(settings, StableSeed(effect), t => offset * boneAt(MainTime(t)), spawnEnd,
                message => LogOnce($"EFFECT {vfxName}, {message}"));
            var particles = root.AddObject(new VfxEffectNode("Vfx:" + vfxName,
                new ParticleRenderItem(simulator, _vfxTextures, () => _shaders.GetStaticEffect(ShaderTypes.Particle), _vfxMeshes)));

            var instance = new VfxEffectInstance(node, boneAt, startTime, particles, simulator, loop, effect);
            instance.Update(rootPlayer.GetTimeUs() / 1_000_000f);
            return instance;
        }

        // The tag's bone at a time on the main clock, sampled from the main clip, so a re-simulation can ask
        // for any time. -1, or a bone the skeleton lacks, is the model's origin.
        // Sampled without the player's rules (TRANSFORM, for one); pass rootPlayer.AnimationRules
        // if an effect is ever placed on a bone a rule moves.
        Func<float, Matrix> BoneAt(int boneIndex, ISkeletonProvider skeleton, AnimationPlayer rootPlayer) => mainTime =>
        {
            var gameSkeleton = skeleton.Skeleton;
            if (gameSkeleton == null || boneIndex < 0 || boneIndex >= gameSkeleton.BoneCount)
                return Matrix.Identity;
            var clip = rootPlayer.AnimationClip;
            if (clip == null || clip.PlayTimeInSec <= 0)
                return gameSkeleton.GetWorldTransform(boneIndex);
            return BonesAt(Math.Clamp(mainTime / clip.PlayTimeInSec, 0, 1), gameSkeleton, clip)[boneIndex];
        };

        // Every bone in the world at a time through the clip, sampled once for all the effects that ask.
        // One entry per distinct time asked, dropped wholesale past 2048 or when the clip or skeleton changes.
        Matrix[] BonesAt(float t, GameSkeleton skeleton, AnimationClip clip)
        {
            if (_bonesOf != (clip, skeleton) || _bones.Count > 2048)
            {
                _bones.Clear();
                _bonesOf = (clip, skeleton);
            }
            if (_bones.TryGetValue(t, out var bones))
                return bones;
            var frame = AnimationSampler.Sample(t, skeleton, clip);
            bones = new Matrix[skeleton.BoneCount];
            for (var i = 0; i < bones.Length; i++)
                bones[i] = frame.GetSkeletonAnimatedWorld(skeleton, i);
            return _bones[t] = bones;
        }

        IMetaDataInstance? Keep(ParsedMetadataAttribute tag, ParsedMetadataAttribute? picked, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, Func<IMetaDataInstance?> build)
        {
            var type = tag.GetType();
            if (!s_tagValues.TryGetValue(type, out var values))
                s_tagValues[type] = values = type.GetProperties().Where(x => x.GetCustomAttribute<MetaDataTagAttribute>() != null).ToArray();
            var keptAs = new KeptAs(string.Join("\u001f", values.Select(x => x.GetValue(tag))), tag == picked, ShowVfx, AuraVfx, WeaponVfx, rootPlayer.AnimationClip, skeleton.Skeleton);
            if (_previous.TryGetValue(tag, out var kept) && kept.As == keptAs && root.Children.Contains(kept.Node))
            {
                _kept[tag] = kept;
                _problems.AddRange(kept.Problems);
                return kept.Instance;
            }

            // What a build adds to the scene starts with the node its instance cleans up; the problems it finds are kept
            // with it, as a kept tag is not built again to find them.
            var before = root.Children.Count;
            var problemsBefore = _problems.Count;
            var instance = Guard(tag, root, build);
            if (instance != null && root.Children.Count > before)
                _kept[tag] = new Kept(keptAs, root.Children[before], instance, [.. _problems.Skip(problemsBefore)]);
            return instance;
        }

        VfxResolveResult Resolve(IEffectMeta effect, string vfxName)
        {
            var key = (vfxName, StableSeed(effect));
            if (!_resolved.TryGetValue(key, out var resolved))
                _resolved[key] = resolved = new VfxResolver(_packFileService).Resolve(vfxName, new Random(key.Item2));
            return resolved;
        }

        // Scale is on EFFECT v3, v5, v7, v11 and v12, not on IEffectMeta; the other versions draw at 1.
        static float ScaleOf(IEffectMeta effect) => effect switch
        {
            Effect_v3 e => e.Scale,
            Effect_v7 e => e.Scale,
            Effect_v11 e => e.Scale,
            Effect_v12 e => e.Scale,
            _ => 1,
        };

        // The same tag gets the same particles on every rebuild; string.GetHashCode differs per process.
        static int StableSeed(IEffectMeta effect)
        {
            var hash = 17;
            foreach (var c in effect.VfxName)
                hash = hash * 31 + c;
            return hash ^ (effect.NodeIndex * 7919) ^ BitConverter.SingleToInt32Bits(effect.EffectStartTime);
        }

        void LogOnce(string message)
        {
            lock (s_loggedVfxMessages)
            {
                if (!s_loggedVfxMessages.Add(message))
                    return;
            }
            _logger.Here().Warning(message);
        }
    }
}
