using AnimationMeta.FileTypes.Definitions;
using AnimationMeta.FileTypes.Parsing;
using AnimationMeta.Visualisation.Instances;
using AnimationMeta.Visualisation.Rules;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using Serilog;
using SharedCore;
using SharedCore.PackFiles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace AnimationMeta.Visualisation
{
    public class MetaDataFactory
    {
        private readonly ILogger _logger = Logging.Create<MetaDataFactory>();
        private readonly ComplexMeshLoader _complexMeshLoader;
        private readonly ResourceLibary _resourceLibrary;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly PackFileService _packFileService;
        private readonly AnimationsContainerComponent _animationsContainerComponent;

        public MetaDataFactory(ComplexMeshLoader complexMeshLoader,
            ResourceLibary resourceLibary, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            PackFileService packFileService,
            AnimationsContainerComponent animationsContainerComponent)
        {
            _complexMeshLoader = complexMeshLoader;
            _resourceLibrary = resourceLibary;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _animationsContainerComponent = animationsContainerComponent;
        }

        public List<IMetaDataInstance> Create(MetaDataFile persistent, MetaDataFile metaData, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment)
        {
            // Clear all
            var output = new List<IMetaDataInstance>();

            if (metaData == null || metaData.GetItemsOfType<DisablePersistant_v10>().Count == 0)
            {
                var metaDataPersistent = ApplyMetaData(persistent, root, skeleton, rootPlayer, fragment);
                output.AddRange(metaDataPersistent);
            }

            var metaDataInstances = ApplyMetaData(metaData, root, skeleton, rootPlayer, fragment);
            output.AddRange(metaDataInstances);
            return output;
        }

        private IEnumerable<IMetaDataInstance> ApplyMetaData(MetaDataFile file, SceneNode root, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, IAnimationBinGenericFormat fragment)
        {
            var output = new List<IMetaDataInstance>();
            if (file == null)
                return output;

            output.AddRange(file.GetItemsOfType<IAnimatedPropMeta>().Select(x => CreateAnimatedProp(x, root, skeleton)));

            output.AddRange(file.GetItemsOfType<ImpactPosition_v10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "ImpactPos")));

            output.AddRange(file.GetItemsOfType<TargetPos_10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "TargetPos")));

            output.AddRange(file.GetItemsOfType<FirePos_v10>().Select(meteDataItem => CreateStaticLocator(meteDataItem, root, meteDataItem.Position, "FirePos")));

            output.AddRange(file.GetItemsOfType<SplashAttack_v10>().Select(meteDataItem => CreateSplashAttack(meteDataItem, root, $"SplashAttack_{Math.Round(meteDataItem.EndTime, 2)}", 0.1f)));

            output.AddRange(file.GetItemsOfType<Effect_v11>().Select(x => CreateEffect(x, root, skeleton)));

            foreach (var meteDataItem in file.GetItemsOfType<DockEquipment>())
                CreateEquipmentDock(meteDataItem, fragment, skeleton, rootPlayer);

            foreach (var meteDataItem in file.GetItemsOfType<Transform_v10>())
                CreateTransform(meteDataItem, rootPlayer);

            return output;
        }

        private void CreateTransform(Transform_v10 transform, AnimationPlayer rootPlayer)
        {
            var rule = new TransformBoneRule(transform);
            rootPlayer.AnimationRules.Add(rule);
        }

        private void CreateEquipmentDock(DockEquipment metaData, IAnimationBinGenericFormat fragment, ISkeletonProvider skeleton, AnimationPlayer rootPlayer)
        {
            var animPath = fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName)?.AnimationFile ??
                           fragment.Entries.FirstOrDefault(x => x.SlotName == metaData.AnimationSlotName + "_2")?.AnimationFile;
            if (animPath == null)
            {
                _logger.Here().Error($"Unable to create docking, as {metaData.AnimationSlotName} animation is missing");
                return;
            }

            int finalBoneIndex = -1;
            foreach (var potentialBoneName in metaData.SkeletonNameAlternatives)
            {
                finalBoneIndex = skeleton.Skeleton.GetBoneIndexByName(potentialBoneName);
                if (finalBoneIndex != -1)
                    break;
            }

            if (finalBoneIndex == -1)
            {
                var boneNames = string.Join(", ", metaData.SkeletonNameAlternatives);
                _logger.Here().Error($"Unable to create docking, as {boneNames} bone is missing");
                return;
            }

            var pf = _packFileService.FindFile(animPath);
            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile, skeleton.Skeleton);

            var rule = new DockEquipmentRule(finalBoneIndex, metaData.PropBoneId, clip, skeleton, metaData.StartTime, metaData.EndTime);
            rootPlayer.AnimationRules.Add(rule);
        }

        private IMetaDataInstance CreateAnimatedProp(IAnimatedPropMeta animatedPropMeta, SceneNode root, ISkeletonProvider rootSkeleton)
        {
            var propName = "Animated_prop";

            var meshPath = _packFileService.FindFile(animatedPropMeta.ModelName);
            var animationPath = _packFileService.FindFile(animatedPropMeta.AnimationName);
            var propPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            // Configure the mesh
            var loadedNode = _complexMeshLoader.Load(meshPath, new GroupNode(propName), propPlayer);

            // Configure animation
            if (animationPath != null)
            {
                var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
                var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_packFileService, skeletonName);
                var skeleton = new GameSkeleton(skeletonFile, propPlayer);
                var animFile = AnimationFile.Create(animationPath);
                var clip = new AnimationClip(animFile, skeleton);
                propPlayer.SetAnimation(clip, skeleton);

                // Add the prop skeleton
                var skeletonSceneNode = new SkeletonNode(_resourceLibrary, skeleton);
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
            var animationRule = new CopyRootTransform(rootSkeleton, animatedPropMeta.BoneId, animatedPropMeta.Position, new Quaternion(animatedPropMeta.Orientation));
            propPlayer.AnimationRules.Add(animationRule);

            // Add to scene
            root.AddObject(loadedNode);

            return new AnimatedPropInstance(loadedNode, propPlayer);
        }

        private IMetaDataInstance CreateStaticLocator(DecodedMetaEntryBase metaData, SceneNode root, Vector3 position, string displayName, float scale = 0.3f)
        {

            var lineRenderer = new LineMeshRender(_resourceLibrary);

            SimpleDrawableNode node = new SimpleDrawableNode(displayName);
            lineRenderer.AddCircle(position, scale, Color.Red);
            node.AddItem(RenderBuckedId.Text, new TextRenderItem(_resourceLibrary, displayName, position));

            node.AddItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            root.AddObject(node);

            return new DrawableMetaInstance(metaData.StartTime, metaData.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateSplashAttack(SplashAttack_v10 splashAttack, SceneNode root, string displayName, float scale = 0.3f)
        {
            float distance = Vector3.Distance(splashAttack.StartPosition, splashAttack.EndPosition);
            if (MathUtil.CompareEqualFloats(distance))
            {
                throw new ConstraintException($"{displayName}: the distance between StartPosition {splashAttack.StartPosition} and EndPosition {splashAttack.EndPosition} is close to 0");
            }


            var lineRenderer = new LineMeshRender(_resourceLibrary);
            Vector3 textPos = (splashAttack.EndPosition + splashAttack.StartPosition) / 2;

            SimpleDrawableNode node = new SimpleDrawableNode(displayName);

            node.AddItem(RenderBuckedId.Text, new TextRenderItem(_resourceLibrary, "StartPos", splashAttack.StartPosition));
            lineRenderer.AddLocator(splashAttack.StartPosition, scale, Color.Red);
            node.AddItem(RenderBuckedId.Text, new TextRenderItem(_resourceLibrary, "EndPos", splashAttack.EndPosition));
            lineRenderer.AddLocator(splashAttack.EndPosition, scale, Color.Red);
            node.AddItem(RenderBuckedId.Text, new TextRenderItem(_resourceLibrary, displayName, textPos));
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

            Matrix rotationM = MathUtil.CreateRotation(new[]
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

            node.AddItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            root.AddObject(node);

            return new DrawableMetaInstance(splashAttack.StartTime, splashAttack.EndTime, node.Name, node);
        }

        private IMetaDataInstance CreateEffect(Effect_v11 effect, SceneNode root, ISkeletonProvider skeleton)
        {
            var lineRenderer = new LineMeshRender(_resourceLibrary);

            SimpleDrawableNode node = new SimpleDrawableNode("Effect:" + effect.VfxName);
            lineRenderer.AddLocator(effect.Position, 0.3f, Color.Red);
            node.AddItem(RenderBuckedId.Text, new TextRenderItem(_resourceLibrary, effect.VfxName, effect.Position));

            node.AddItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = lineRenderer, ModelMatrix = Matrix.Identity });
            root.AddObject(node);

            var instance = new DrawableMetaInstance(effect.StartTime, effect.EndTime, node.Name, node);
            if (effect.Tracking)
                instance.FollowBone(skeleton, effect.NodeIndex);
            return instance;
        }
    }
}
