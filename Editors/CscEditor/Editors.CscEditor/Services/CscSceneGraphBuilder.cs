using System;
using System.Collections.Generic;
using System.Linq;
using Editors.CscEditor.Data;
using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.CoreLog;
using Shared.GameFormats.Animation;

namespace Editors.CscEditor.Services
{
    /// <summary>One ANIMATION_ELEMENT child bound to a loaded clip: a model can have several
    /// (sequenced by their own Begin/End windows, blended across an overlap), not just one.</summary>
    public class CscAnimationBinding
    {
        public required CscElement Element { get; set; }
        public required AnimationClip Clip { get; init; }
        public required GameSkeleton Skeleton { get; init; }
        public float ClipLengthSeconds { get; init; }
        public int FrameCount { get; init; }

        /// <summary>Model-space transform the animroot bone accumulates over one full clip pass
        /// (identity when the clip ends where it started). Walk cycles and travelling idles put
        /// their ground movement on animroot, so this is composed once per completed loop into
        /// the binding's placed pose (see <see cref="CscAnimationComponent"/>'s ComputePlacedPose)
        /// to make the clip self-propel instead of teleporting back on every wrap.</summary>
        public Matrix LoopRootMotion { get; set; } = Matrix.Identity;
    }

    /// <summary>Per-model-element animation state: the player its meshes render with, plus the
    /// skeleton resolved from the model itself (bind pose) or from the currently active binding's
    /// .anim (which takes over when one exists). Implements <see cref="ISkeletonProvider"/> so
    /// attach_point meshes can resolve their bone through the standard
    /// <see cref="GameWorld.Core.Utility.SkeletonBoneAnimationResolver"/> mechanism.</summary>
    public class CscModelContent : ISkeletonProvider
    {
        public required AnimationPlayer Player { get; init; }
        public GameSkeleton? Skeleton { get; set; }

        /// <summary>Every ANIMATION_ELEMENT child bound so far (nested or attached). Sequenced by
        /// their own Begin/End at playback time; when two overlap they're cross-blended - see
        /// <see cref="CscAnimationComponent.DriveSkeletalPlayers"/>.</summary>
        public List<CscAnimationBinding> Bindings { get; } = [];
    }

    /// <summary>A .csc referenced by a ROOT_REF_ELEMENT, loaded for display: its elements are
    /// remapped to private ids, marked <see cref="CscElement.IsExternal"/> and rendered under the
    /// host element's transform. They are never part of the host scene's save data.</summary>
    public class CscSubScene
    {
        public required CscElement Host { get; set; }
        public required CscScene Scene { get; init; }
        public List<int> ElementIds { get; } = [];
    }

    /// <summary>
    /// Builds the 3D scene for a <see cref="CscScene"/>: one flat transform node per element
    /// (world matrices are composed by <see cref="CscAnimationComponent"/> and pushed onto the
    /// mesh/dummy nodes directly, because both the engine's picking and its selection highlight
    /// ignore parent-chain transforms), with the element's visual (model mesh, dummy box, light
    /// shape, camera frustum) as children of that node.
    /// </summary>
    public class CscSceneGraphBuilder
    {
        readonly ILogger _logger = Logging.Create<CscSceneGraphBuilder>();
        readonly IPackFileService _packFileService;
        readonly SceneManager _sceneManager;
        readonly ComplexMeshLoader _complexMeshLoader;
        readonly AnimationsContainerComponent _animationsContainer;
        readonly ISkeletonAnimationLookUpHelper _skeletonLookUp;
        readonly CscPlaybackContext _context;

        GroupNode? _sceneRoot;
        public Dictionary<int, CscElementSceneNode> ElementNodes { get; } = [];
        public Dictionary<int, CscModelContent> ModelContents { get; } = [];
        public List<CscSubScene> SubScenes { get; } = [];

        /// <summary>Ids handed to elements of referenced sub-scenes, so they can share the
        /// node/content dictionaries with the host scene. They count down from int.MaxValue and
        /// skip every id the host scene uses, so no host id can be overwritten.</summary>
        int _nextExternalId = int.MaxValue;
        readonly HashSet<int> _usedElementIds = [];
        readonly HashSet<string> _subSceneLoadStack = new(StringComparer.OrdinalIgnoreCase);

        public CscSceneGraphBuilder(
            IPackFileService packFileService,
            SceneManager sceneManager,
            ComplexMeshLoader complexMeshLoader,
            AnimationsContainerComponent animationsContainer,
            ISkeletonAnimationLookUpHelper skeletonLookUp,
            CscPlaybackContext context)
        {
            _packFileService = packFileService;
            _sceneManager = sceneManager;
            _complexMeshLoader = complexMeshLoader;
            _animationsContainer = animationsContainer;
            _skeletonLookUp = skeletonLookUp;
            _context = context;
        }

        public void Build(CscScene scene)
        {
            Clear();
            foreach (var element in scene.AllElementsIncludingNested())
                _usedElementIds.Add(element.Id);

            _sceneRoot = _sceneManager.RootNode.AddObject(new GroupNode("CSC_Scene") { IsEditable = false });
            foreach (var element in scene.AllElementsIncludingNested())
                AddElement(element);
            RefreshAnimationBindings(scene);
        }

        /// <summary>
        /// Points the built 3D scene at a new <see cref="CscScene"/> object for the same document
        /// (an undo/redo snapshot) without reloading what didn't change. An element whose id, kind
        /// and asset path are unchanged keeps its loaded model, animation player, bindings and
        /// referenced sub-scene; only cheap visuals (dummies, light shapes, camera frustums) are
        /// rebuilt for it. Everything else is removed or added. A full <see cref="Build"/>
        /// reloads every model, which takes as long as opening the file.
        /// </summary>
        public void Rebind(CscScene scene)
        {
            if (_sceneRoot == null)
            {
                Build(scene);
                return;
            }

            var elements = scene.AllElementsIncludingNested().ToList();
            var byId = elements.ToDictionary(e => e.Id);

            // A kept sub-scene's private ids must not collide with an id the new snapshot uses;
            // if one does, that sub-scene is loaded again (with fresh ids) via its host.
            foreach (var sub in SubScenes.ToList())
            {
                if (sub.ElementIds.Any(byId.ContainsKey))
                    RemoveElement(sub.Host.Id);
            }

            var externalIds = SubScenes.SelectMany(s => s.ElementIds).ToHashSet();
            foreach (var (id, node) in ElementNodes.ToList())
            {
                if (externalIds.Contains(id) || !ElementNodes.ContainsKey(id))
                    continue;
                if (!byId.TryGetValue(id, out var element) || !CanKeepContent(node.Element, element))
                    RemoveElement(id);
            }

            _usedElementIds.Clear();
            _usedElementIds.UnionWith(byId.Keys);
            _usedElementIds.UnionWith(SubScenes.SelectMany(s => s.ElementIds));

            foreach (var element in elements)
            {
                if (!ElementNodes.TryGetValue(element.Id, out var node))
                {
                    AddElement(element);
                    continue;
                }

                var previous = node.Element;
                node.Element = element;
                foreach (var sub in SubScenes.Where(s => s.Host == previous))
                    sub.Host = element;

                if (!HasExpensiveContent(element.Kind))
                {
                    // Light/camera visuals hold the element itself; dummies are free to rebuild.
                    foreach (var child in node.Children.ToList())
                        node.RemoveObject(child);
                    AddContent(node, element);
                }
            }

            // Bindings point at the previous snapshot's animation elements: move them to the
            // matching new ones so RefreshAnimationBindings sees them as unchanged.
            foreach (var content in ModelContents.Values)
            {
                foreach (var binding in content.Bindings)
                {
                    if (byId.TryGetValue(binding.Element.Id, out var element) && element.AssetPath == binding.Element.AssetPath)
                        binding.Element = element;
                }
            }

            RefreshAnimationBindings(scene);
        }

        static bool HasExpensiveContent(CscElementKind kind) =>
            kind is CscElementKind.Model or CscElementKind.VariantModel or CscElementKind.Animation or CscElementKind.RootRef;

        static bool CanKeepContent(CscElement previous, CscElement next) =>
            previous.Kind == next.Kind && previous.AssetPath == next.AssetPath;

        public void Clear()
        {
            var existing = _sceneManager.RootNode.Children.Where(x => x.Name == "CSC_Scene").ToList();
            foreach (var node in existing)
                _sceneManager.RootNode.RemoveObject(node);

            foreach (var content in ModelContents.Values)
                _animationsContainer.Remove(content.Player);

            _sceneRoot = null;
            ElementNodes.Clear();
            ModelContents.Clear();
            SubScenes.Clear();
            _usedElementIds.Clear();
            _nextExternalId = int.MaxValue;
        }

        public void AddElement(CscElement element)
        {
            if (_sceneRoot == null)
                return;

            _usedElementIds.Add(element.Id);
            var node = _sceneRoot.AddObject(new CscElementSceneNode(element, _context));
            ElementNodes[element.Id] = node;
            AddContent(node, element);
        }

        public void RemoveElement(int elementId)
        {
            if (_sceneRoot != null && ElementNodes.TryGetValue(elementId, out var node))
                _sceneRoot.RemoveObject(node);
            ElementNodes.Remove(elementId);

            if (ModelContents.Remove(elementId, out var content))
                _animationsContainer.Remove(content.Player);

            RemoveSubScenesOf(elementId);
        }

        /// <summary>Rebuilds an element's visual children (e.g. after its model path changed).</summary>
        public void RefreshContent(CscElement element)
        {
            if (!ElementNodes.TryGetValue(element.Id, out var node))
                return;

            foreach (var child in node.Children.ToList())
                node.RemoveObject(child);
            if (ModelContents.Remove(element.Id, out var content))
                _animationsContainer.Remove(content.Player);
            RemoveSubScenesOf(element.Id);

            AddContent(node, element);
        }

        /// <summary>Tears down the sub-scene(s) a ROOT_REF element loaded (recursively - a
        /// sub-scene's own root refs are hosted by ids inside its ElementIds list).</summary>
        void RemoveSubScenesOf(int hostId)
        {
            foreach (var sub in SubScenes.Where(s => s.Host.Id == hostId).ToList())
            {
                SubScenes.Remove(sub);
                foreach (var id in sub.ElementIds)
                    RemoveElement(id);
            }
        }

        /// <summary>Maps a scene node (e.g. a clicked mesh) back to its owning element.</summary>
        public CscElement? FindOwningElement(ISceneNode node)
        {
            ISceneNode? current = node;
            while (current != null)
            {
                if (current is CscElementSceneNode elementNode)
                    return elementNode.Element;
                current = current.Parent;
            }
            return null;
        }

        // ---------------------------------------------------------------------
        // Skeletal animation (.anim on props)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Binds each model element's player to the .anim of every ANIMATION_ELEMENT child (nested
        /// or attached) - a model can carry several, sequenced/blended at playback time by their
        /// own Begin/End windows (see <see cref="CscAnimationComponent.DriveSkeletalPlayers"/>).
        /// A standalone ANIMATION_ELEMENT (one that's itself an attach-tree parent, not merely
        /// attached under a Model - real "building" scenes attach destruct pieces straight to the
        /// .anim's own element) binds to its OWN asset path instead, via
        /// <see cref="GetAnimationSources"/>. The skeleton of each is resolved from its own anim
        /// file's header. Call after any structural change.
        /// </summary>
        public void RefreshAnimationBindings(CscScene scene)
        {
            foreach (var element in scene.AllElementsIncludingNested())
            {
                if (!ModelContents.TryGetValue(element.Id, out var content))
                    continue;

                var animationElements = GetAnimationSources(element);

                // Rebuild only when the set of animation children actually changed - loading .anim
                // files is not free and this runs on every structural edit, not just once.
                var unchanged = animationElements.Count == content.Bindings.Count &&
                    animationElements.All(a => content.Bindings.Any(b => b.Element == a));
                if (unchanged)
                    continue;

                if (animationElements.Count == 0)
                {
                    content.Bindings.Clear();
                    content.Player.SetAnimation(null, content.Skeleton);
                    continue;
                }

                content.Bindings.Clear();
                foreach (var animationElement in animationElements)
                {
                    try
                    {
                        var animPackFile = _packFileService.FindFile(animationElement.AssetPath);
                        if (animPackFile == null)
                        {
                            _logger.Warning("CSC editor: animation file not found: {Path}", animationElement.AssetPath);
                            continue;
                        }

                        var animFile = AnimationFile.Create(animPackFile);
                        var skeletonFile = _skeletonLookUp.GetSkeletonFileFromName(animFile.Header.SkeletonName);

                        // No skeleton file means an ad-hoc skeleton ("building" and other rigidmodel
                        // anims): the .anim's own bone table is the skeleton definition.
                        var skeleton = skeletonFile != null
                            ? new GameSkeleton(skeletonFile, content.Player)
                            : GameSkeleton.CreateFromAnimationFile(animFile, content.Player);

                        var clip = new AnimationClip(animFile, skeleton);
                        content.Bindings.Add(new CscAnimationBinding
                        {
                            Element = animationElement,
                            Clip = clip,
                            Skeleton = skeleton,
                            ClipLengthSeconds = clip.PlayTimeInSec,
                            FrameCount = clip.DynamicFrames.Count,
                            LoopRootMotion = ComputeLoopRootMotion(skeleton, clip),
                        });
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "CSC editor: failed to bind animation '{Path}'", animationElement.AssetPath);
                    }
                }

                if (content.Bindings.Count > 0)
                {
                    var first = content.Bindings[0];
                    content.Skeleton = first.Skeleton;
                    content.Player.SetAnimation(first.Clip, first.Skeleton, allowAnimationsFromDifferentSkeletons: true);
                    content.Player.IsEnabled = true;
                    content.Player.Pause(); // time is driven by the editor timeline, not wall clock
                }

                if (ElementNodes.TryGetValue(element.Id, out var node))
                    WireAttachmentResolvers(node, content);
            }

            foreach (var sub in SubScenes.Where(s => scene.FindElement(s.Host.Id) == s.Host).ToList())
                RefreshAnimationBindings(sub.Scene);
        }

        /// <summary>An element's animation source(s): a standalone ANIMATION_ELEMENT (one that's
        /// itself an attach-tree parent - real "building" scenes attach destruct pieces straight
        /// to the .anim's own element rather than to a wrapping Model) binds to its own asset
        /// path; any other element binds to its attached ANIMATION_ELEMENT children, as before.
        /// The two cases are mutually exclusive since an element's Kind can't be both.</summary>
        static List<CscElement> GetAnimationSources(CscElement element)
        {
            if (element.Kind == CscElementKind.Animation && element.AssetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                return [element];

            return element.Children
                .Where(c => c.Kind == CscElementKind.Animation && c.AssetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// The animroot bone's model-space displacement over one full clip pass: X such that
        /// animrootWorld(lastFrame) = animrootWorld(frame0) * X. Composed once per completed loop
        /// so travelling animations (walks, and idles that drift) continue from where the previous
        /// loop ended instead of snapping back. Identity for clips whose animroot returns to its
        /// start and for skeletons without an animroot bone.
        /// </summary>
        static Matrix ComputeLoopRootMotion(GameSkeleton skeleton, AnimationClip clip)
        {
            if (clip.DynamicFrames.Count < 2)
                return Matrix.Identity;

            var animRootIndex = skeleton.GetBoneIndexByName("animroot");
            if (animRootIndex < 0)
                return Matrix.Identity;

            var firstFrame = AnimationSampler.Sample(0, 0, skeleton, clip);
            var lastFrame = AnimationSampler.Sample(clip.DynamicFrames.Count - 1, 0, skeleton, clip);
            if (firstFrame == null || lastFrame == null)
                return Matrix.Identity;

            var start = firstFrame.GetSkeletonAnimatedWorld(skeleton, animRootIndex);
            var end = lastFrame.GetSkeletonAnimatedWorld(skeleton, animRootIndex);
            return Matrix.Invert(start) * end;
        }

        /// <summary>Points every attach_point mesh (variantmesh SLOT weapons/shields, loaded with
        /// <see cref="Rmv2MeshNode.AttachmentPointName"/> set) at its bone on this model's current
        /// skeleton, and every rigid-bone mesh (RMV2's own <see cref="Rmv2MeshNode.AnimationMatrixOverride"/>
        /// - "building" destruction pieces) at the same bone on this model's OWN skeleton, so its
        /// origin becomes the bone regardless of where its vertices were authored. Re-run whenever
        /// the skeleton changes.</summary>
        void WireAttachmentResolvers(CscElementSceneNode node, CscModelContent content)
        {
            node.ForeachNodeRecursive(child =>
            {
                if (child is not Rmv2MeshNode mesh)
                    return;

                if (!string.IsNullOrWhiteSpace(mesh.AttachmentPointName))
                {
                    var boneIndex = content.Skeleton?.GetBoneIndexByName(mesh.AttachmentPointName) ?? -1;
                    mesh.AttachmentBoneResolver = boneIndex >= 0
                        ? new SkeletonBoneAnimationResolver(content, boneIndex)
                        : null;
                    if (mesh.AttachmentBoneResolver != null)
                        mesh.ModelMatrix = Matrix.Identity; // stop stale pushed world from also landing here - see AttachmentOuterWorld
                }
                else if (mesh.AnimationMatrixOverride >= 0)
                {
                    mesh.AttachmentBoneResolver = content.Skeleton != null && mesh.AnimationMatrixOverride < content.Skeleton.BoneCount
                        ? new SkeletonBoneAnimationResolver(content, mesh.AnimationMatrixOverride)
                        : null;
                    if (mesh.AttachmentBoneResolver != null)
                        mesh.ModelMatrix = Matrix.Identity;
                }
            });
        }

        // ---------------------------------------------------------------------
        // Content
        // ---------------------------------------------------------------------

        void AddContent(CscElementSceneNode node, CscElement element)
        {
            switch (element.Kind)
            {
                case CscElementKind.Model:
                case CscElementKind.VariantModel:
                    AddModelContent(node, element);
                    break;

                case CscElementKind.Vfx:
                    node.AddObject(new CscBoxDummyNode("VFX_Dummy", Color.MediumPurple, 0.5f));
                    break;

                case CscElementKind.Sfx:
                    node.AddObject(new CscBoxDummyNode("SFX_Dummy", Color.Lime, 0.4f));
                    break;

                case CscElementKind.SoundSphere:
                    node.AddObject(new CscSoundSphereNode
                    {
                        InnerRadius = ReadTypeRecordFloat(element, 1, 1),
                        OuterRadius = ReadTypeRecordFloat(element, 2, 2),
                    });
                    break;

                case CscElementKind.PointLight:
                    node.AddObject(new CscPointLightNode(element, _context));
                    break;

                case CscElementKind.SpotLight:
                    node.AddObject(new CscSpotLightNode(element, _context));
                    break;

                case CscElementKind.Camera:
                    node.AddObject(new CscCameraFrustumNode(element, _context));
                    break;

                case CscElementKind.Animation:
                    node.AddObject(new CscBoxDummyNode("Animation_Dummy", Color.Cyan, 0.25f));
                    AddAnimationCarrierContent(element);
                    break;

                case CscElementKind.AnimationNodeTransform:
                    node.AddObject(new CscBoxDummyNode("NodeTransform_Dummy", Color.Turquoise, 0.2f));
                    break;

                case CscElementKind.Prefab:
                    node.AddObject(new CscBoxDummyNode("Prefab_Dummy", Color.SaddleBrown, 0.7f));
                    break;

                case CscElementKind.RootRef:
                    node.AddObject(new CscBoxDummyNode("RootRef_Dummy", Color.Orange, 0.7f));
                    LoadSubScene(element);
                    break;

                case CscElementKind.CameraShake:
                    node.AddObject(new CscBoxDummyNode("CameraShake_Dummy", Color.HotPink, 0.3f));
                    break;

                case CscElementKind.Locator:
                    // The element node itself already draws a locator cross.
                    break;

                default:
                    node.AddObject(new CscBoxDummyNode("Unknown_Dummy", Color.Gray, 0.5f));
                    break;
            }
        }

        float ReadTypeRecordFloat(CscElement element, int fieldIndex, float fallback)
        {
            var fields = element.TypeRecord?.Children.ToList();
            if (fields != null && fieldIndex < fields.Count && fields[fieldIndex].Value is float f)
                return f;
            return fallback;
        }

        /// <summary>Registers a bare <see cref="ModelContents"/> entry (player only, no mesh) for
        /// a standalone ANIMATION_ELEMENT so other elements can bone-attach directly to IT via
        /// ATTACH_TO_PARAMETERS - some real "building" scenes (destruct pieces) attach straight to
        /// the .anim's own element instead of to a Model element that merely carries the .anim as
        /// a child. <see cref="GetAnimationSources"/> then binds this same element to its own asset
        /// path when <see cref="RefreshAnimationBindings"/> runs, and
        /// <see cref="CscAnimationComponent.GetAttachBoneMatrix"/> resolves against it exactly like
        /// it would a Model's skeleton.</summary>
        void AddAnimationCarrierContent(CscElement element)
        {
            var player = _animationsContainer.RegisterAnimationPlayer(new AnimationPlayer(), $"CscElement_{element.Id}");
            ModelContents[element.Id] = new CscModelContent { Player = player };
        }

        /// <summary>
        /// A Model/VariantModel element always gets a <see cref="ModelContents"/> entry, even when
        /// it has no asset path or fails to load - some "building" scenes carry a MODEL_ELEMENT with
        /// a blank rigid_model_v2 path whose sole purpose is hosting an ANIMATION_ELEMENT child (the
        /// .anim supplies an ad-hoc bone table, no named skeleton file) that OTHER elements attach
        /// to via ATTACH_TO_PARAMETERS bone ids. Bailing out early without registering content (the
        /// previous behaviour) meant that .anim never got bound and every bone-attached child on it
        /// silently fell back to identity - i.e. unpositioned - since
        /// <see cref="CscAnimationComponent.GetAttachBoneMatrix"/> and
        /// <see cref="RefreshAnimationBindings"/> both key off this same dictionary.
        /// </summary>
        void AddModelContent(CscElementSceneNode node, CscElement element)
        {
            var player = _animationsContainer.RegisterAnimationPlayer(new AnimationPlayer(), $"CscElement_{element.Id}");
            var content = new CscModelContent { Player = player };
            ModelContents[element.Id] = content;

            var path = element.AssetPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                node.AddObject(new CscBoxDummyNode("Model_Missing", Color.Red, 0.5f));
                return;
            }

            try
            {
                var file = _packFileService.FindFile(path);
                if (file == null)
                {
                    _logger.Warning("CSC editor: model file not found: {Path}", path);
                    node.AddObject(new CscBoxDummyNode("Model_NotFound", Color.Red, 0.5f));
                    return;
                }

                var loaded = _complexMeshLoader.Load(file, player, onlyLoadRootNode: true, onlyLoadFirstMesh: false);
                if (loaded == null)
                {
                    node.AddObject(new CscBoxDummyNode("Model_LoadFailed", Color.Red, 0.5f));
                    return;
                }

                node.AddObject(loaded);
                ResolveBindSkeleton(content, loaded);
                WireAttachmentResolvers(node, content);
            }
            catch (Exception e)
            {
                _logger.Error(e, "CSC editor: failed to load model '{Path}'", path);
                node.AddObject(new CscBoxDummyNode("Model_LoadFailed", Color.Red, 0.5f));
            }
        }

        /// <summary>Resolves the model's own skeleton (bind pose) from its RMV2 header, so
        /// bone-attached children and attach_point meshes are positioned correctly even before -
        /// or without - an animation being bound. Ad-hoc skeletons ("building") have no skeleton
        /// file; they stay null here and get their skeleton from the .anim when one is bound.</summary>
        void ResolveBindSkeleton(CscModelContent content, SceneNode loaded)
        {
            try
            {
                var skeletonName = SceneNodeHelper.GetSkeletonName(loaded);
                if (string.IsNullOrWhiteSpace(skeletonName))
                    return;

                var skeletonFile = _skeletonLookUp.GetSkeletonFileFromName(skeletonName);
                if (skeletonFile == null)
                    return;

                content.Skeleton = new GameSkeleton(skeletonFile, content.Player);
            }
            catch (Exception e)
            {
                _logger.Warning(e, "CSC editor: failed to resolve bind skeleton");
            }
        }

        // ---------------------------------------------------------------------
        // ROOT_REF sub-scenes
        // ---------------------------------------------------------------------

        /// <summary>Loads the .csc referenced by a ROOT_REF element for display: elements get
        /// private ids and the IsExternal mark, then join the normal node/content dictionaries so
        /// animation and picking work; they are never added to the host scene's save data.</summary>
        void LoadSubScene(CscElement host)
        {
            var path = host.AssetPath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            var pathKey = path.Replace('/', '\\').ToLowerInvariant();
            if (!_subSceneLoadStack.Add(pathKey))
            {
                _logger.Warning("CSC editor: root ref cycle detected at '{Path}' - not loading again", path);
                return;
            }

            try
            {
                var file = _packFileService.FindFile(path);
                if (file == null)
                {
                    _logger.Warning("CSC editor: root ref scene not found: {Path}", path);
                    return;
                }

                var scene = CscScene.Load(file.DataSource.ReadData());
                var sub = new CscSubScene { Host = host, Scene = scene };
                foreach (var element in scene.AllElementsIncludingNested())
                {
                    element.Id = AllocateExternalId(_usedElementIds, ref _nextExternalId);
                    element.IsExternal = true;
                    sub.ElementIds.Add(element.Id);
                }

                SubScenes.Add(sub);
                foreach (var element in scene.AllElementsIncludingNested())
                    AddElement(element);

                RefreshAnimationBindings(scene);
            }
            catch (Exception e)
            {
                _logger.Error(e, "CSC editor: failed to load root ref scene '{Path}'", path);
            }
            finally
            {
                _subSceneLoadStack.Remove(pathKey);
            }
        }

        internal static int AllocateExternalId(HashSet<int> usedIds, ref int nextCandidate)
        {
            while (usedIds.Contains(nextCandidate))
            {
                if (nextCandidate == int.MinValue)
                    throw new InvalidOperationException("No free CSC element ids remain.");
                nextCandidate--;
            }

            var allocated = nextCandidate;
            usedIds.Add(allocated);
            if (nextCandidate != int.MinValue)
                nextCandidate--;
            return allocated;
        }
    }
}
