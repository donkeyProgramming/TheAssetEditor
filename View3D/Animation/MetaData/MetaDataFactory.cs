using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.AnimationPack;
using FileTypes.DB;
using FileTypes.MetaData;
using FileTypes.MetaData.Instances;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation.AnimationChange;
using View3D.Components.Component;
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
        AnimationFragment _fragment;

        public MetaDataFactory(SceneNode root, IComponentManager componentManager, ISkeletonProvider skeleton, AnimationPlayer rootPlayer, AnimationFragment fragment)
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

            var metaDataInstances = ApplyMetaData(metaData);
            output.AddRange(metaDataInstances);
            return output;
        }

        List<IMetaDataInstance> ApplyMetaData(MetaDataFile file)
        {
            var output = new List<IMetaDataInstance>();
            
            // Props
            var props = file.GetItemsOfType("animated_prop");
            for(int i = 0; i < props.Count; i++ )
                output.Add(CreateAnimatedProp(AnimatedProp.Create(props[i]), i));

            // Dock
           var dockRightSlots = file.GetItemsOfType("dock_eqpt_rhand");
           foreach(var slot in dockRightSlots)
               CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.RightHand);
           
           var dockleftSlots = file.GetItemsOfType("dock_eqpt_lhand");
           foreach (var slot in dockleftSlots)
               CreateEquimentDock(DockEquipment.Create(slot), DockEquipmentRule.DockSlot.LeftHand);
           
            return output;
        }


        void CreateEquimentDock(DockEquipment animatedPropMeta, DockEquipmentRule.DockSlot slot)
        {
            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var pfs = resourceLib.Pfs;
            
            var animPath = "";
            if (slot == DockEquipmentRule.DockSlot.LeftHand)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_LEFT_HAND").AnimationFile;
            if (slot == DockEquipmentRule.DockSlot.RightHand)
                animPath = _fragment.Fragments.First(x => x.Slot.Value == "DOCK_EQUIPMENT_RIGHT_HAND").AnimationFile;

            var pf = pfs.FindFile(animPath);
            var animFile = AnimationFile.Create(pf);
            var clip = new AnimationClip(animFile);



            var rule = new DockEquipmentRule(slot, animatedPropMeta.PropBoneId, clip, _rootSkeleton);
            _rootPlayer.AnimationRules.Add(rule);
        }

        IMetaDataInstance CreateAnimatedProp(AnimatedProp animatedPropMeta, int index)
        {
            var propName = "animated_prop_" + index;

            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            var skeletonHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            var pfs = resourceLib.Pfs;

            var meshPath = pfs.FindFile(animatedPropMeta.MeshName);
            var animationPath = pfs.FindFile(animatedPropMeta.AnimationName);

            var propPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), propName + Guid.NewGuid());

            // Configure the mesh
            string skeletonName = "";
            SceneLoader loader = new SceneLoader(resourceLib);
            var result = loader.Load(meshPath, new GroupNode(propName), propPlayer, ref skeletonName);

            // Configure animation
            var skeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, skeletonName);
            var skeleton = new GameSkeleton(skeletonFile, propPlayer);
            var animFile = AnimationFile.Create(animationPath);
            var clip = new AnimationClip(animFile);

            var rule = new CopyRootTransform(_rootSkeleton, animatedPropMeta.BoneId, animatedPropMeta.Position, animatedPropMeta.Orientation);
            propPlayer.AnimationRules.Add(rule);

            // Apply animation
            propPlayer.SetAnimation(clip, skeleton);

            // Add to scene
            _root.AddObject(result);

            var skeletonSceneNode = new SkeletonNode(resourceLib.Content, new SimpleSkeletonProvider(skeleton));
            skeletonSceneNode.NodeColour = Color.Yellow;

            result.AddObject(skeletonSceneNode);

            return new AnimatedPropInstance(result, propPlayer);
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
}


/*
 
 if (model.MetaData != null)
            {
                var f = MetaDataFileParser.Open(model.MetaData, _schemaManager);
                var props = f.GetItemsOfType("animated_prop");
                foreach(var prop in props)
                {
                    var animatedPropMeta = AnimatedProp.Create(prop);
                    var mesh = _pfs.FindFile(animatedPropMeta.MeshName);

                    var PropPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new View3D.Animation.AnimationPlayer(), "propPlayer"+Guid.NewGuid());

                    SceneLoader loader = new SceneLoader(_pfs, _componentManager.GetComponent<ResourceLibary>());


                    string skelName = "";
                    var result = loader.Load(mesh, new GroupNode("The dogNode"), PropPlayer, ref skelName);


                    var ske = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, skelName);
                    GameSkeleton s = new GameSkeleton(ske, PropPlayer);

                    var anim = _pfs.FindFile(animatedPropMeta.AnimationName);
                    AnimationFile animFile = AnimationFile.Create(anim);
                    var clip = new AnimationClip(animFile);

                    var rule = new CopyRootTransform(model, animatedPropMeta.BoneId,  animatedPropMeta.Position, animatedPropMeta.Orientation);
                    clip.AnimationRules.Add(rule);

                    //for (int i = 0; i < clip.DynamicFrames.Count; i++)
                    //{
                    //
                    //    var headPos = model.AnimationClip.GetPosition(model.Skeleton, i, 22);
                    //    clip.SetPosition(i, 0, headPos);
                    //}

                    //clip.CopyRootMovementFrom(model.AnimationClip 22);


                    PropPlayer.SetAnimation(clip, s);

                    model.MainNode.AddObject(result);

                    var skeletonSceneNode = new SkeletonNode(_componentManager.GetComponent<ResourceLibary>().Content, new SimpleSkeletonProvider(s));
                    skeletonSceneNode.NodeColour = Color.Yellow;

                    result.AddObject(skeletonSceneNode);

                    var test = new AnimatedPropInstance(result as SceneNode, PropPlayer, model, animatedPropMeta.BoneId, animatedPropMeta.Position, animFile);

                    model.AttachedItems.Add(test);
                }
 
 */