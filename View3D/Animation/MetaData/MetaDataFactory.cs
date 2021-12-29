using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.MetaData.Instances;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation.AnimationChange;
using View3D.Components;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;

namespace View3D.Animation.MetaData
{
    public class MetaDataFactory
    {
        SceneNode _root;
        IComponentManager _componentManager;
        ISkeletonProvider _rootSkeleton;
        AnimationPlayer _rootPlayer;
        AnimationSetFile _fragment;

        public MetaDataFactory(SceneNode root, IComponentManager componentManager, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, AnimationSetFile fragment)
        {
            _root = root;
            _componentManager = componentManager;
            _rootSkeleton = skeleton;
            _rootPlayer = rootPlayer;
            _fragment = fragment;
        }

        public List<IMetaDataInstance> Create(MetaDataFile persistenet, MetaDataFile metaData)
        {
            // Clear all
            var output = new List<IMetaDataInstance>();


            // animated prop
            // dock
            // effect
            if (metaData == null || metaData.GetItemsOfType("DISABLE_PERSISTENT").Count == 0)
            {
                var meteaDataPersiste = ApplyMetaData(persistenet);
                output.AddRange(meteaDataPersiste);
            }

            var metaDataInstances = ApplyMetaData(metaData);
            output.AddRange(metaDataInstances);
            return output;
        }

        List<IMetaDataInstance> ApplyMetaData(MetaDataFile file)
        {
            var output = new List<IMetaDataInstance>();
            if (file == null)
                return output;

            // Props
            var props = file.GetItemsOfType("animated_prop");
            for (int i = 0; i < props.Count; i++)
                output.Add(CreateAnimatedProp(AnimatedProp.Create(props[i]), i));

            // World locations
            foreach (var meta in file.GetItemsOfType("impact_pos").Where(x => x.Version == 10))
                output.Add(CreateImpactPos(ImpactPosition.Create(meta)));

            foreach (var meta in file.GetItemsOfType("target_pos"))
                output.Add(CreateTargetPos(TargetPos.Create(meta)));

            foreach (var meta in file.GetItemsOfType("fire_pos"))
                output.Add(CreateFirePos(FirePos.Create(meta)));

            // Effects
            var effects = file.GetItemsOfType("effect").Where(x => x.Version == 11);
            foreach (var effect in effects)
                output.Add(CreateEffect(Effect.Create(effect)));

            // Dock
            foreach (var slot in file.GetItemsOfType("dock_eqpt_rhand"))
                CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.RightHand);

            foreach (var slot in file.GetItemsOfType("dock_eqpt_lhand"))
                CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.LeftHand);

            foreach (var slot in file.GetItemsOfType("dock_eqpt_lwaist"))
                CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.LeftWaist);

            foreach (var slot in file.GetItemsOfType("dock_eqpt_rwaist"))
                CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.RightWaist);

            foreach (var slot in file.GetItemsOfType("dock_eqpt_back"))
                CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.Back);

            //foreach (var slot in file.GetItemsOfType("WEAPON_LHAND"))
            //    CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.LeftHand);
            //
            //foreach (var slot in file.GetItemsOfType("WEAPON_RHAND"))
            //    CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.RightHand);

            // Transform
            foreach (var transform in file.GetItemsOfType("Transform"))
                CreateTransform(Transform.Create(transform));

                return output;
        }

        private void CreateTransform(Transform transform)
        {
            var rule = new TransformBoneRule(transform);
            _rootPlayer.AnimationRules.Add(rule);
        }

        void CreateEquimentDock(DockEquipment animatedPropMeta, DockEquipmentRule.DockSlot slot)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var pfs = resourceLib.Pfs;

            var animPath = "";
            if (slot == DockEquipmentRule.DockSlot.LeftHand)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_LEFT_HAND").AnimationFile;
            else if (slot == DockEquipmentRule.DockSlot.RightHand)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_RIGHT_HAND").AnimationFile;

            else if (slot == DockEquipmentRule.DockSlot.LeftWaist)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_LEFT_WAIST").AnimationFile;
            else if (slot == DockEquipmentRule.DockSlot.RightWaist)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_RIGHT_WAIST").AnimationFile;

            else if (slot == DockEquipmentRule.DockSlot.Back)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_BACK").AnimationFile;

            var pf = pfs.FindFile(animPath);
            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile);

            var rule = new DockEquipmentRule(slot, animatedPropMeta.PropBoneId, clip, _rootSkeleton, animatedPropMeta.StartTime, animatedPropMeta.EndTime);
            _rootPlayer.AnimationRules.Add(rule);
        }

        IMetaDataInstance CreateAnimatedProp(AnimatedProp animatedPropMeta, int index)
        {
            var propName = "animated_prop_" + index;

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var graphics = _componentManager.GetComponent<DeviceResolverComponent>();
            var pfs = resourceLib.Pfs;

            var meshPath = pfs.FindFile(animatedPropMeta.MeshName);
            var animationPath = pfs.FindFile(animatedPropMeta.AnimationName);

            var propPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            // Configure the mesh
            SceneLoader loader = new SceneLoader(resourceLib, pfs, GeometryGraphicsContextFactory.CreateInstance(graphics.Device));
            var loadedNode = loader.Load(meshPath, new GroupNode(propName), propPlayer);

            // Configure animation
            var skeletonName = SceneNodeHelper.GetSkeletonName(loadedNode);
            var skeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, skeletonName);
            var skeleton = new GameSkeleton(skeletonFile, propPlayer);
            var animFile = AnimationFile.Create(animationPath);
            var clip = new AnimationClip(animFile);

            var rule = new CopyRootTransform(_rootSkeleton, animatedPropMeta.BoneId, animatedPropMeta.Position, animatedPropMeta.Orientation);
           

            // Apply animation
            propPlayer.SetAnimation(clip, skeleton);
            propPlayer.AnimationRules.Add(rule);

            // Add to scene
            _root.AddObject(loadedNode);

            var skeletonSceneNode = new SkeletonNode(_componentManager, new SimpleSkeletonProvider(skeleton));
            skeletonSceneNode.NodeColour = Color.Yellow;

            loadedNode.AddObject(skeletonSceneNode);

            return new AnimatedPropInstance(loadedNode, propPlayer);
        }

        IMetaDataInstance CreateImpactPos(ImpactPosition impactMetaData)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();

            SimpleDrawableNode node = new SimpleDrawableNode("ImpactPos");
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new CricleRenderItem(resourceLib.GetEffect(ShaderTypes.Line), impactMetaData.Position, 0.3f));
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, "ImpactPos", impactMetaData.Position));
            _root.AddObject(node);

            return new DrawableMetaInstance(impactMetaData.StartTime, impactMetaData.EndTime, node.Name, node);
        }

        IMetaDataInstance CreateFirePos(FirePos metaData)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();

            SimpleDrawableNode node = new SimpleDrawableNode("FirePos");
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new CricleRenderItem(resourceLib.GetEffect(ShaderTypes.Line), metaData.Position, 0.3f));
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, "FirePos", metaData.Position));
            _root.AddObject(node);

            return new DrawableMetaInstance(metaData.StartTime, metaData.EndTime, node.Name, node);
        }

        IMetaDataInstance CreateTargetPos(TargetPos metaData)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();

            SimpleDrawableNode node = new SimpleDrawableNode("TargetPos");
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new CricleRenderItem(resourceLib.GetEffect(ShaderTypes.Line), metaData.Position, 0.3f));
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, "TargetPos", metaData.Position));
            _root.AddObject(node);

            return new DrawableMetaInstance(metaData.StartTime, metaData.EndTime, node.Name, node);
        }



        IMetaDataInstance CreateEffect(Effect effect)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();

            SimpleDrawableNode node = new SimpleDrawableNode("Effect:"+ effect.Name);
            node.AddItem(Components.Rendering.RenderBuckedId.Line, new LocatorRenderItem(resourceLib.GetEffect(ShaderTypes.Line), effect.Position, 0.3f));
            node.AddItem(Components.Rendering.RenderBuckedId.Text, new TextRenderItem(resourceLib, effect.Name, effect.Position));
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
        SceneNode _node;

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
        SceneNode _node;
        string _description;
        public AnimationPlayer Player => null;

        SkeletonBoneAnimationResolver _animationResolver;

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
            if(_animationResolver != null)
                _node.ModelMatrix = _animationResolver.GetWorldTransform();
        }

        public void CleanUp()
        {
            _node.Parent.RemoveObject(_node);
        }
    }
}