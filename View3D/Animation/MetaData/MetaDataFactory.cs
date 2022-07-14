using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.MetaData.Definitions;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using View3D.Animation.AnimationChange;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace View3D.Animation.MetaData
{
    public class MetaDataFactory
    {
        private ILogger _logger = Logging.Create<MetaDataFactory>();
        private SceneNode _root;
        private IComponentManager _componentManager;
        private ISkeletonProvider _rootSkeleton;
        private AnimationPlayer _rootPlayer;
        private IAnimationBinGenericFormat _fragment;
        private ApplicationSettingsService _applicationSettingsService;

        public MetaDataFactory(SceneNode root, IComponentManager componentManager, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment, ApplicationSettingsService applicationSettingsService)
        {
            _root = root;
            _componentManager = componentManager;
            _rootSkeleton = skeleton;
            _rootPlayer = rootPlayer;
            _fragment = fragment;
            _applicationSettingsService = applicationSettingsService;
        }

        public List<IMetaDataInstance> Create(MetaDataFile persistent, MetaDataFile metaData)
        {
            // Clear all
            var output = new List<IMetaDataInstance>();

            if (metaData == null || metaData.GetItemsOfType<DisablePersistant_v10>().Count == 0)
            {
                var metaDataPersistent = ApplyMetaData(persistent);
                output.AddRange(metaDataPersistent);
            }

            var metaDataInstances = ApplyMetaData(metaData);
            output.AddRange(metaDataInstances);
            return output;
        }

        private IEnumerable<IMetaDataInstance> ApplyMetaData(MetaDataFile file)
        {
           var output = new List<IMetaDataInstance>();
           if (file == null)
               return output;

           output.AddRange(file.GetItemsOfType<IAnimatedPropMeta>().Select(CreateAnimatedProp));

           output.AddRange(file.GetItemsOfType<ImpactPosition>().Select(meteDataItem => CreateStaticLocator(meteDataItem, meteDataItem.Position, "ImpactPos")));

           output.AddRange(file.GetItemsOfType<TargetPos_10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, meteDataItem.Position, "TargetPos")));

           output.AddRange(file.GetItemsOfType<FirePos>().Select(meteDataItem => CreateStaticLocator(meteDataItem, meteDataItem.Position, "FirePos")));

           output.AddRange(file.GetItemsOfType<SplashAttack_v10>().Select(meteDataItem => CreateSplashAttack(meteDataItem, $"SplashAttack_{Math.Round(meteDataItem.EndTime, 2)}", 0.1f)));

           output.AddRange(file.GetItemsOfType<Effect_v11>().Select(CreateEffect));

           foreach (var meteDataItem in file.GetItemsOfType<DockEquipment>())
               CreateEquipmentDock(meteDataItem);
           
           foreach (var meteDataItem in file.GetItemsOfType<Transform_v10>())
               CreateTransform(meteDataItem);

           return output;
        }

        private void CreateTransform(Transform_v10 transform)
        {
            var rule = new TransformBoneRule(transform);
            _rootPlayer.AnimationRules.Add(rule);
        }

        private void CreateEquipmentDock(DockEquipment metaData)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var pfs = resourceLib.Pfs;
        
            var animPath = _fragment.Entries.FirstOrDefault(x=>x.SlotName == metaData.AnimationSlotName)?.AnimationFile ??
                           _fragment.Entries.FirstOrDefault(x => x.SlotName  == metaData.AnimationSlotName + "_2")?.AnimationFile;
            if (animPath == null)
            {
                _logger.Here().Error($"Unable to create docking, as {metaData.AnimationSlotName} animation is missing");
                return;
            }

            int finalBoneIndex = -1;
            foreach (var potentialBoneName in metaData.SkeletonNameAlternatives)
            {
                finalBoneIndex = _rootSkeleton.Skeleton.GetBoneIndexByName(potentialBoneName);
                if (finalBoneIndex != -1)
                    break;
            }
            
            if (finalBoneIndex == -1)
            {
                var boneNames = string.Join(", ", metaData.SkeletonNameAlternatives);
                _logger.Here().Error($"Unable to create docking, as {boneNames} bone is missing");
                return;
            }

            var pf = pfs.FindFile(animPath);
            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile, _rootSkeleton.Skeleton);
        
            var rule = new DockEquipmentRule(finalBoneIndex, metaData.PropBoneId, clip, _rootSkeleton, metaData.StartTime, metaData.EndTime);
            _rootPlayer.AnimationRules.Add(rule);
        }

        private IMetaDataInstance CreateAnimatedProp(IAnimatedPropMeta animatedPropMeta)
        {
            var propName = "Animated_prop";

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var graphics = _componentManager.GetComponent<DeviceResolverComponent>();
            var pfs = resourceLib.Pfs;

            var meshPath = pfs.FindFile(animatedPropMeta.ModelName);
            var animationPath = pfs.FindFile(animatedPropMeta.AnimationName);
            var propPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            // Configure the mesh
            SceneLoader loader = new SceneLoader(resourceLib, pfs, GeometryGraphicsContextFactory.CreateInstance(graphics.Device), _componentManager, _applicationSettingsService);
            var loadedNode = loader.Load(meshPath, new GroupNode(propName), propPlayer);

            // Configure animation
            if (animationPath != null)
            {
                var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
                var skeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, skeletonName);
                var skeleton = new GameSkeleton(skeletonFile, propPlayer);
                var animFile = AnimationFile.Create(animationPath);
                var clip = new AnimationClip(animFile, skeleton);
                propPlayer.SetAnimation(clip, skeleton);

                // Add the prop skeleton
                var skeletonSceneNode = new SkeletonNode(_componentManager, new SimpleSkeletonProvider(skeleton));
                skeletonSceneNode.NodeColour = Color.Yellow;
                skeletonSceneNode.ScaleMult = animatedPropMeta.Scale;
                loadedNode.AddObject(skeletonSceneNode);
            }

            // Configure scale
            loadedNode.ForeachNodeRecursive((node) =>
            {
                if (node is SceneNode selectable)
                    selectable.ScaleMult = animatedPropMeta.Scale;
            });
            loadedNode.ScaleMult = animatedPropMeta.Scale;

            // Add the animation rules
            var animationRule = new CopyRootTransform(_rootSkeleton, animatedPropMeta.BoneId, animatedPropMeta.Position, new Quaternion(animatedPropMeta.Orientation));
            propPlayer.AnimationRules.Add(animationRule);

            // Add to scene
            _root.AddObject(loadedNode);

            return new AnimatedPropInstance(loadedNode, propPlayer);
        }

        private IMetaDataInstance CreateStaticLocator(DecodedMetaEntryBase metaData, Vector3 position, string displayName, float scale = 0.3f)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var lineRenderer = new LineMeshRender(resourceLib);

            SimpleDrawableNode node = new SimpleDrawableNode(displayName);
            lineRenderer.AddCircle(position, scale, Color.Red);
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, displayName, position));
            
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            _root.AddObject(node);

            return new DrawableMetaInstance(metaData.StartTime, metaData.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateSplashAttack(SplashAttack_v10 splashAttack, string displayName, float scale = 0.3f)
        {
            float distance =  Vector3.Distance(splashAttack.StartPosition, splashAttack.EndPosition);
            if (MathUtil.CompareEqualFloats(distance))
            {
                throw new ConstraintException($"{displayName}: the distance between StartPosition {splashAttack.StartPosition} and EndPosition {splashAttack.EndPosition} is close to 0");
            }

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var lineRenderer = new LineMeshRender(resourceLib);
            Vector3 textPos = (splashAttack.EndPosition + splashAttack.StartPosition) / 2;

            SimpleDrawableNode node = new SimpleDrawableNode(displayName);
            
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, "StartPos", splashAttack.StartPosition));
            lineRenderer.AddLocator(splashAttack.StartPosition, scale, Color.Red);
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, "EndPos", splashAttack.EndPosition));
            lineRenderer.AddLocator(splashAttack.EndPosition, scale, Color.Red);
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, displayName, textPos));
            lineRenderer.AddLine(splashAttack.StartPosition, splashAttack.EndPosition, Color.Red);
            
            Vector3 normal = splashAttack.EndPosition - splashAttack.StartPosition;  // corresponds to Z
            normal.Normalize();
            var random = new Random();
            Func<Random, float> RandomFloat = r => (float)(2 * r.NextDouble() - 1);
            Vector3 vectorP = new Vector3(RandomFloat(random), RandomFloat(random), RandomFloat(random));
            vectorP.Normalize();
            
            Vector3 planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal)); // corresponds to X
            Vector3 planeVectorPN = Vector3.Cross(vectorP, normal); // corresponds to Y
            planeVectorP.Normalize();
            planeVectorPN.Normalize();

            Matrix rotationM = MathUtil.CreateRotation(new []
            {
                planeVectorP,
                planeVectorPN,
                normal
            });
            
            if (splashAttack.AoeShape == 0) // Cone or Sphere
            {
                if (MathUtil.CompareEqualFloats(splashAttack.AngleForCone / 2, tolerance: 0.1f))
                {
                    throw new ConstraintException($"{displayName}: the half-angle {splashAttack.AngleForCone / 2} of the cone is close to 0");
                }
                Matrix transformationM = rotationM * Matrix.CreateScale(distance) * Matrix.CreateTranslation(splashAttack.StartPosition);
                lineRenderer.AddConeSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, splashAttack.AngleForCone, Color.Red);
            }
            if (splashAttack.AoeShape == 1) // Corridor
            {
                if (MathUtil.CompareEqualFloats(splashAttack.WidthForCorridor, tolerance: 0.001f))
                {
                    throw new ConstraintException($"{displayName}: the WidthForCorridor {splashAttack.WidthForCorridor} of the corridor is close to 0");
                }
                Matrix transformationM = rotationM * Matrix.CreateScale(splashAttack.WidthForCorridor / 2) * Matrix.CreateTranslation(splashAttack.StartPosition);
                lineRenderer.AddCorridorSplash(splashAttack.StartPosition, splashAttack.EndPosition, transformationM, Color.Red);
            }
            
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            _root.AddObject(node);

            return new DrawableMetaInstance(splashAttack.StartTime, splashAttack.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateEffect(Effect_v11 effect)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var lineRenderer = new LineMeshRender(resourceLib);
        
            SimpleDrawableNode node = new SimpleDrawableNode("Effect:"+ effect.VfxName);
            lineRenderer.AddLocator(effect.Position, 0.3f, Color.Red);
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, effect.VfxName, effect.Position));
            
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            _root.AddObject(node);
        
            var instance = new DrawableMetaInstance(effect.StartTime, effect.EndTime, node.Name, node);
            if (effect.Tracking)
                instance.FollowBone(_rootSkeleton, effect.NodeIndex);
            return instance;
        }
    }

    public interface IMetaDataInstance
    {
        void CleanUp();
        void Update(float currentTime);
        AnimationPlayer Player { get; }
    }

    public class AnimatedPropInstance : IMetaDataInstance
    {
        private SceneNode _node;

        public AnimationPlayer Player { get; private set; }

        public AnimatedPropInstance(SceneNode node, AnimationPlayer player)
        {
            _node = node;
            Player = player;
        }

        public void Update(float currentTime)
        { }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
            Player.MarkedForRemoval = true;
        }
    }

    public class DrawableMetaInstance : IMetaDataInstance
    {
        private ILogger _logger = Logging.Create<MetaDataFactory>();
        private bool _hasError = false;

        private SceneNode _node;
        private string _description;
        public AnimationPlayer Player => null;

        private SkeletonBoneAnimationResolver _animationResolver;

        public DrawableMetaInstance(float startTime, float endTime, string description, SceneNode node)
        {
            _description = description;
            _node = node;
        }

        public void FollowBone(ISkeletonProvider skeleton, int boneIndex)
        {
            if(boneIndex != -1)
                _animationResolver = new SkeletonBoneAnimationResolver(skeleton, boneIndex);
        }

        public void Update(float currentTime)
        {
            if (_hasError)
                return;

            try
            {
                if (_animationResolver != null)
                    _node.ModelMatrix = _animationResolver.GetWorldTransform();
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error in {nameof(DrawableMetaInstance)} - {e.Message}");
                _hasError = true;
            }
        }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
        }
    }
}