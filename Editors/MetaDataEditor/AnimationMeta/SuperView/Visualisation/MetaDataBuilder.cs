using System.Data;
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
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
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
        List<IMetaDataInstance> Create(ParsedMetadataFile? persistent, ParsedMetadataFile? metaData, ParsedMetadataAttribute? selectedMetaDataAttribute, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment);
    }

    public class MetaDataBuilder : IMetaDataBuilder
    {
        private readonly ILogger _logger = Logging.Create<MetaDataBuilder>();
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly RenderEngineComponent _resourceLibrary;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IPackFileService _packFileService;
        private readonly AnimationsContainerComponent _animationsContainerComponent;

        private static Color s_color = Color.Black;
        private static Color s_selectedColor = Color.Red;

        public MetaDataBuilder(ComplexMeshLoader complexMeshLoader,
            RenderEngineComponent resourceLibrary,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            IPackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent)
        {
            _complexMeshLoader = complexMeshLoader;
            _resourceLibrary = resourceLibrary;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
        }

        public List<IMetaDataInstance> Create(ParsedMetadataFile? persistent,
            ParsedMetadataFile? metaData, ParsedMetadataAttribute? selectedMetaDataAttribute,
            SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment)
        {
            var output = new List<IMetaDataInstance>();

            if (metaData == null || metaData.GetItemsOfType<DisablePersistant_v10>().Count == 0)
            {
                var metaDataPersistent = ApplyMetaData(persistent, selectedMetaDataAttribute, root, skeleton, rootPlayer, fragment);
                output.AddRange(metaDataPersistent);
            }

            var metaDataInstances = ApplyMetaData(metaData, selectedMetaDataAttribute, root, skeleton, rootPlayer, fragment);
            output.AddRange(metaDataInstances);
            return output;
        }

        private IEnumerable<IMetaDataInstance> ApplyMetaData(ParsedMetadataFile? file, ParsedMetadataAttribute? selectedAttribute, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment)
        {
            var output = new List<IMetaDataInstance>();
            if (file == null) return output;

            output.AddRange(file.GetItemsOfType<IAnimatedPropMeta>().Select(x => CreateAnimatedProp(x, root, skeleton, selectedAttribute, rootPlayer)));
            output.AddRange(file.GetItemsOfType<ImpactPosition_v10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "ImpactPos", selectedAttribute)));
            output.AddRange(file.GetItemsOfType<TargetPos_10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "TargetPos", selectedAttribute)));
            output.AddRange(file.GetItemsOfType<FirePos_v10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "FirePos", selectedAttribute)));
            output.AddRange(file.GetItemsOfType<SplashAttack_v10>().Select(meteDataItem => CreateSplashAttack(meteDataItem, root, $"SplashAttack_{Math.Round(meteDataItem.EndTime, 2)}", 0.1f, selectedAttribute)));
            output.AddRange(file.GetItemsOfType<IEffectMeta>().Select(x => CreateEffect(x, root, skeleton, selectedAttribute)));

            foreach (var meteDataItem in file.GetItemsOfType<DockEquipment>()) CreateEquipmentDock(meteDataItem, fragment, skeleton, rootPlayer);
            foreach (var meteDataItem in file.GetItemsOfType<Transform_v10>()) CreateTransform(meteDataItem, rootPlayer);

            return output;
        }

        private void CreateTransform(Transform_v10 transform, AnimationPlayer rootPlayer)
        {
            var rule = new TransformBoneRule(transform);
            rootPlayer.AnimationRules.Add(rule);
        }

        private void CreateEquipmentDock(DockEquipment metaData, IAnimationBinGenericFormat fragment, ISkeletonProvider skeleton, AnimationPlayer rootPlayer)
        {
            if (fragment == null) return;
            var animPath = fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName)?.AnimationFile ?? fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName + "_2")?.AnimationFile;
            if (animPath == null) return;

            var finalBoneIndex = -1;
            foreach (var potentialBoneName in metaData.SkeletonNameAlternatives)
            {
                finalBoneIndex = skeleton.Skeleton.GetBoneIndexByName(potentialBoneName);
                if (finalBoneIndex != -1) break;
            }
            if (finalBoneIndex == -1) return;

            var pf = _packFileService.FindFile(animPath);
            if (pf == null) return;

            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile, skeleton.Skeleton);

            var rule = new DockEquipmentRule(finalBoneIndex, metaData.PropBoneId, clip, skeleton, metaData.StartTime, metaData.EndTime);
            rootPlayer.AnimationRules.Add(rule);
        }

        private IMetaDataInstance CreateAnimatedProp(IAnimatedPropMeta animatedPropMeta, SceneNode root, ISkeletonProvider rootSkeleton, ParsedMetadataAttribute? selectedMetaDataAttribute, AnimationPlayer rootPlayer)
        {
            var propName = "Animated_prop";
            var color = selectedMetaDataAttribute == animatedPropMeta ? s_selectedColor : s_color;

            var meshPath = _packFileService.FindFile(animatedPropMeta.ModelName);
            if (meshPath == null) throw new Exception($"Unable to find model for animated prop.");

            var animationPath = _packFileService.FindFile(animatedPropMeta.AnimationName);
            var propPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            var loadedNode = _complexMeshLoader.Load(meshPath, new GroupNode(propName), propPlayer, true, true);

            if (animationPath != null)
            {
                var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
                var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(skeletonName);
                if (skeletonFile == null) throw new Exception($"Unable to find skeleton for animated prop.");

                var skel = new GameSkeleton(skeletonFile, propPlayer);
                var animFile = AnimationFile.Create(animationPath);
                var clip = new AnimationClip(animFile, skel);
                propPlayer.SetAnimation(clip, skel);

                var skeletonSceneNode = new SkeletonNode(skel) { NodeColour = color, ScaleMult = animatedPropMeta.Scale };
                loadedNode.AddObject(skeletonSceneNode);
            }

            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is ISelectable selectableNode) selectableNode.IsSelectable = false;
                if (node is SceneNode sceneNode) sceneNode.ScaleMult = animatedPropMeta.Scale;
            });
            loadedNode.ScaleMult = animatedPropMeta.Scale;

            var animationRule = new CopyRootTransform(rootSkeleton, animatedPropMeta.BoneId, animatedPropMeta.Position, new Quaternion(animatedPropMeta.Orientation));
            propPlayer.AnimationRules.Add(animationRule);
            if (rootPlayer.IsPlaying) propPlayer.Play();
            propPlayer.Refresh();

            root.AddObject(loadedNode);
            return new AnimatedPropInstance(loadedNode, propPlayer);
        }

        private IMetaDataInstance CreateStaticLocator(DecodedMetaEntryBase metaData, SceneNode root, Vector3 position, string displayName, ParsedMetadataAttribute? selectedMetaDataAttribute, float scale = 0.3f)
        {
            var color = selectedMetaDataAttribute == metaData ? s_selectedColor : s_color;
            var node = new SimpleDrawableNode(displayName);
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, displayName, position, color));
            node.AddItem(LineHelper.AddCircle(position, scale, color));
            root.AddObject(node);

            return new DrawableMetaInstance(metaData.StartTime, metaData.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateSplashAttack(SplashAttack_v10 splashAttack, SceneNode root, string displayName, float scale, ParsedMetadataAttribute? selectedAttribute)
        {
            var distance = Vector3.Distance(splashAttack.StartPosition, splashAttack.EndPosition);
            if (MathUtil.CompareEqualFloats(distance))
                throw new ConstraintException($"{displayName}: the distance between StartPosition {splashAttack.StartPosition} and EndPosition {splashAttack.EndPosition} is close to 0");

            var color = selectedAttribute == splashAttack ? s_selectedColor : s_color;
            var node = new SimpleDrawableNode(displayName);
            var textPos = (splashAttack.EndPosition + splashAttack.StartPosition) / 2;

            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "StartPos", splashAttack.StartPosition, color));
            node.AddItem(LineHelper.AddLocator(splashAttack.StartPosition, scale, color));

            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "EndPos", splashAttack.EndPosition, color));
            node.AddItem(LineHelper.AddLocator(splashAttack.EndPosition, scale, color));

            node.AddItem(new WorldTextRenderItem(_resourceLibrary, displayName, textPos, color));
            node.AddItem(LineHelper.AddLine(splashAttack.StartPosition, splashAttack.EndPosition, color));

            var normal = splashAttack.EndPosition - splashAttack.StartPosition;
            normal.Normalize();
            var random = new Random(1);
            Func<Random, float> RandomFloat = r => (float)(2 * r.NextDouble() - 1);
            var vectorP = new Vector3(RandomFloat(random), RandomFloat(random), RandomFloat(random));
            vectorP.Normalize();

            var planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal));
            var planeVectorPN = Vector3.Cross(vectorP, normal);
            planeVectorP.Normalize();
            planeVectorPN.Normalize();

            var rotationM = MathUtil.CreateRotation([planeVectorP, planeVectorPN, normal]);

            if (splashAttack.AoeShape == 0)
            {
                if (MathUtil.CompareEqualFloats(splashAttack.AngleForCone / 2, tolerance: 0.1f)) throw new ConstraintException($"{displayName}: half-angle is 0");
                var transformationM = rotationM * Matrix.CreateScale(distance) * Matrix.CreateTranslation(splashAttack.StartPosition);
                node.AddItem(LineHelper.AddConeSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, splashAttack.AngleForCone, color));
            }
            if (splashAttack.AoeShape == 1)
            {
                if (MathUtil.CompareEqualFloats(splashAttack.WidthForCorridor, tolerance: 0.001f)) throw new ConstraintException($"{displayName}: WidthForCorridor is 0");
                var transformationM = rotationM * Matrix.CreateScale(splashAttack.WidthForCorridor / 2) * Matrix.CreateTranslation(splashAttack.StartPosition);
                node.AddItem(LineHelper.AddCorridorSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, color));
            }

            root.AddObject(node);
            return new DrawableMetaInstance(splashAttack.StartTime, splashAttack.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateEffect(IEffectMeta effect, SceneNode root, ISkeletonProvider skeleton, ParsedMetadataAttribute? selectedAttribute)
        {
            var color = selectedAttribute == effect ? s_selectedColor : s_color;
            var node = new SimpleDrawableNode("Effect:" + effect.VfxName);

            var locatorScale = 0.3f;
            var pos = effect.Position;

            // [FIX Bug 3] 完全重写 Effect 的渲染！让 XYZ 线段严格跟随四元数旋转矩阵！
            var rotMatrix = Matrix.CreateFromQuaternion(new Quaternion(effect.Orientation));

            node.AddItem(LineHelper.AddLine(pos, pos + Vector3.Transform(new Vector3(locatorScale, 0, 0), rotMatrix), Color.Red));
            node.AddItem(LineHelper.AddLine(pos, pos + Vector3.Transform(new Vector3(0, locatorScale, 0), rotMatrix), Color.Green));
            node.AddItem(LineHelper.AddLine(pos, pos + Vector3.Transform(new Vector3(0, 0, locatorScale), rotMatrix), Color.Blue));

            node.AddItem(new WorldTextRenderItem(_resourceLibrary, effect.VfxName, pos, color));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "X", pos + Vector3.Transform(new Vector3(locatorScale * 1.1f, 0, 0), rotMatrix), Color.Red));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "Y", pos + Vector3.Transform(new Vector3(0, locatorScale * 1.1f, 0), rotMatrix), Color.Green));
            node.AddItem(new WorldTextRenderItem(_resourceLibrary, "Z", pos + Vector3.Transform(new Vector3(0, 0, locatorScale * 1.1f), rotMatrix), Color.Blue));

            root.AddObject(node);

            var instance = new DrawableMetaInstance(effect.EffectStartTime, effect.EffectEndTime, node.Name, node);
            if (effect.Tracking)
                instance.FollowBone(skeleton, effect.NodeIndex);
            return instance;
        }
    }
}
