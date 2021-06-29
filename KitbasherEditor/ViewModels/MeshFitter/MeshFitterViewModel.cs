using CommonControls.Services;
using Filetypes.RigidModel;
using KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping;
using KitbasherEditor.Views.EditorViews.MeshFitter;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels.MeshFitter
{
    public class MeshFitterViewModel : AnimatedBlendIndexRemappingViewModel
    {
        GameSkeleton _dwarfSkeleton; 
        GameSkeleton _humanoid01Skeleton;

        IComponentManager _componentManager;
        AnimationClip _animationClip;
        AnimationPlayer _animationPlayer;
        AnimationPlayer _oldAnimationPlayer;
        List<Rmv2MeshNode> _meshNodes;
        SkeletonNode _currentSkeletonNode;

        public MeshFitterViewModel(RemappedAnimatedBoneConfiguration configuration, List<Rmv2MeshNode> meshNodes, GameSkeleton targetSkeleton, AnimationFile currentSkeletonFile, IComponentManager componentManager) : base(configuration)
        {
            _meshNodes = meshNodes;
            _dwarfSkeleton = targetSkeleton;
            _componentManager = componentManager;

            _animationPlayer = _componentManager.GetComponent<AnimationsContainerComponent>().RegisterAnimationPlayer(new AnimationPlayer(), "Temp animation rerig");
            _humanoid01Skeleton = new GameSkeleton(currentSkeletonFile, _animationPlayer);
            

            // Build empty animation
            _animationClip = new AnimationClip();
            _animationClip.DynamicFrames.Add(new AnimationClip.KeyFrame());

            for (int i = 0; i < _humanoid01Skeleton.BoneCount; i++)
            {
                _animationClip.DynamicFrames[0].Rotation.Add(_humanoid01Skeleton.Rotation[i]);
                _animationClip.DynamicFrames[0].Position.Add(_humanoid01Skeleton.Translation[i]);
                _animationClip.DynamicFrames[0].Scale.Add(Vector3.One);

                _animationClip.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                _animationClip.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
            }


            _animationPlayer.SetAnimation(_animationClip, _humanoid01Skeleton);
            _animationPlayer.Play();


            var resourceLib = _componentManager.GetComponent<ResourceLibary>();
            _currentSkeletonNode = new SkeletonNode(resourceLib.Content, new SimpleSkeletonProvider(_humanoid01Skeleton));
            _componentManager.GetComponent<SceneManager>().RootNode.AddObject(_currentSkeletonNode);


            _oldAnimationPlayer = _meshNodes.First().AnimationPlayer;
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _animationPlayer;
        }


        //Matrix GetBoneMatrix()
        //{
        //    var offset_m4 = (Matrix.Translation(bone.location) * Quaternion(bone.rotation_quaternion).to_matrix().to_4x4())
        //    return bone.pose_matrix * offset_m4.inverted()
        //}

        public override void ReProcessFucker()
        {
            var mapping = MeshBones.Bones.First().BuildRemappingList();
            
            for (int i = 0; i < _humanoid01Skeleton.BoneCount; i++)
            {
                var mappedIndex = mapping.FirstOrDefault(x => x.OriginalValue == i);
                if (mappedIndex != null)
                {
                    var scale = 1.0f;
                    _animationClip.DynamicFrames[0].Scale[i] = new Vector3(scale);
                    //scale = 1.4f;
                    //if (i == 0)
                    //    _animationClip.DynamicFrames[0].Scale[i] = new Vector3(scale);

                    var parentBoneId = _humanoid01Skeleton.GetParentBone(i);
                    var dwarfParentBoneId = _dwarfSkeleton.GetParentBone(mappedIndex.NewValue);

                    if (parentBoneId != -1 && dwarfParentBoneId != -1)
                    {
                        var boneLength0 = Vector3.Distance(_humanoid01Skeleton.GetWorldTransform(i).Translation, _humanoid01Skeleton.GetWorldTransform(parentBoneId).Translation);
                        var boneLength1 = Vector3.Distance(_dwarfSkeleton.GetWorldTransform(mappedIndex.NewValue).Translation, _dwarfSkeleton.GetWorldTransform(dwarfParentBoneId).Translation);
                        scale =  boneLength1 / boneLength0;
                        if (scale == 0 || float.IsNaN(scale))
                            scale = 1;
                    
                        //scale = 0.4f;
                        _animationClip.DynamicFrames[0].Scale[i] = new Vector3(scale);
                    }

                    //_animationClip.DynamicFrames[0].Position[i] = Vector3.Transform( _dwarfSkeleton.Translation[mappedIndex.NewValue], Matrix.Invert(Matrix.CreateScale(scale)));
                    //_animationClip.DynamicFrames[0].Position[i] = _dwarfSkeleton.Translation[mappedIndex.NewValue];
                   // _animationClip.DynamicFrames[0].Rotation[i] = _dwarfSkeleton.Rotation[mappedIndex.NewValue];
                }

         
            }

            //foreach (var mesh in _meshNodes)
            //    mesh.Scale = new Vector3(0.5f);

        }

        public override void OnMappingCreated(int humanoid01BoneIndex, int dwarfBoneIndex)
        {
            if (_dwarfSkeleton == null)
                return;

            var baseScale = 0.5f;
            _animationClip.DynamicFrames[0].Scale[0] = new Vector3(baseScale);
            //var basechildBones = _humanoid01Skeleton.GetChildBones(0);
            //foreach (var childBoneIndex in basechildBones)
            //{
            //    float invScale = 1 / baseScale;
            //    _animationClip.DynamicFrames[0].Scale[childBoneIndex] = new Vector3(invScale);
            //}


            var desiredParentWorldTransform = _dwarfSkeleton.GetWorldTransform(dwarfBoneIndex);
            var dwarfParentBone = _dwarfSkeleton.GetParentBone(dwarfBoneIndex);
            var dwarfParentWorldPos = _dwarfSkeleton.GetWorldTransform(dwarfParentBone);
            var dwarfBoneLength = Vector3.Distance(desiredParentWorldTransform.Translation, dwarfParentWorldPos.Translation);



            var parentBoneIndex = _humanoid01Skeleton.GetParentBone(humanoid01BoneIndex);
            var parentWorld = _humanoid01Skeleton.GetAnimatedWorldTranform(parentBoneIndex);
            var matrix = desiredParentWorldTransform * Matrix.Invert(parentWorld);
            matrix.Decompose(out var _, out var newRotation, out var newPosition);


            var humanodB0 = _humanoid01Skeleton.GetWorldTransform(humanoid01BoneIndex);
            var humanodB1 = _humanoid01Skeleton.GetWorldTransform(parentBoneIndex);
            var humanodBoneLength = Vector3.Distance(humanodB0.Translation, humanodB1.Translation);

            float scale = dwarfBoneLength / (humanodBoneLength * baseScale);
            scale = 1;
            _animationClip.DynamicFrames[0].Rotation[humanoid01BoneIndex] = newRotation;
            _animationClip.DynamicFrames[0].Position[humanoid01BoneIndex] = newPosition;//;Quaternion.Multiply(_currentSkeleton.Rotation[newBoneIndex], );
            _animationClip.DynamicFrames[0].Scale[humanoid01BoneIndex] *= new Vector3(scale);



            var childBones = _humanoid01Skeleton.GetChildBones(humanoid01BoneIndex);
            foreach (var childBoneIndex in childBones)
            {
                float invScale = 1 / scale;
                _animationClip.DynamicFrames[0].Scale[childBoneIndex] *= new Vector3(invScale);
            }

            //ReProcessFucker();
            return;
            // Reset animation
          //  _animationClip.DynamicFrames[0].Position[dwarfBoneIndex] = _humanoid01Skeleton.Translation[humanoid01BoneIndex];

          //// Get the position the the bone we want to move      
          // var currentWorldTransform = _humanoid01Skeleton.GetAnimatedWorldTranform(humanoid01BoneIndex);
          //var desiredParentWorldTransform= _dwarfSkeleton.GetWorldTransform(dwarfBoneIndex);
          //
          //currentWorldTransform.Decompose(out var _, out var currentRot, out var currentTrans);
          //desiredParentWorldTransform.Decompose(out var _, out var desiredRot, out var desiredTrans);
          //
          // //var relativeRotation = desiredRot * Quaternion.Inverse(currentRot);
          // var relativeRotation = desiredRot* Quaternion.Inverse(currentRot);
          // var relativePosition = currentTrans - desiredTrans;
          //
          // var parentBoneIndex = _dwarfSkeleton.GetParentBone(dwarfBoneIndex);
          // var parentWorld = _dwarfSkeleton.GetWorldTransform(parentBoneIndex);
          //
          // var w = Matrix.CreateWorld(
          //     Vector3.Zero,
          //     parentWorld.Forward,
          //     parentWorld.Up);
          //
          // var invW = Matrix.Invert(w);
          //
          // //relativePosition = Vector3.Transform(relativePosition, invW);
          // relativeRotation = Quaternion.Multiply(relativeRotation, Quaternion.CreateFromRotationMatrix(invW));
          //
          // _animationClip.DynamicFrames[0].Position[dwarfBoneIndex] = currentTrans;// _humanoid01Skeleton.Translation[humanoid01BoneIndex] + relativePosition;
          // //_animationClip.DynamicFrames[0].Rotation[currentSkeletonBoneIndex] =  Quaternion.Multiply(relativeRotation, _currentSkeleton.Rotation[targetSkeletonBoneIndex]);
          // //_animationClip.DynamicFrames[0].Rotation[newBoneIndex] = relativeRotation;//;Quaternion.Multiply(_currentSkeleton.Rotation[newBoneIndex], );

            return;
            //---- Attemp 1
            // Get origianl parent bone
            //var desiredWorldPos = _currentSkeleton.GetWorldTransform(newBoneIndex);
            //
            //var parentBone = _targetSkeleton.GetParentBone(originalBoneIndex);
            //var parentBoneWorldTrans = _targetSkeleton.GetWorldTransform(parentBone);
            //
            //var transDiff = parentBoneWorldTrans * Matrix.Invert(desiredWorldPos);
            ////var transDiff = desiredWorldPos * Matrix.Invert(parentBoneWorldTrans);
            //
            //var result = transDiff.Decompose(out var _, out var rotation, out var trans);
            //
            //var orgTrans = _animationClip.DynamicFrames[0].Position[originalBoneIndex];
            //var orgRot = _animationClip.DynamicFrames[0].Rotation[originalBoneIndex];
            //
            //_animationClip.DynamicFrames[0].Position[newBoneIndex] = trans;
            //_animationClip.DynamicFrames[0].Rotation[newBoneIndex] = rotation;


            

            //return 
            // Convert currentWorld into origina parentbone space




            //var parent 

            //_targetSkeleton.SetBoneFromWorld(55, curretWorld);
            //_targetSkeleton.RebuildSkeletonMatrix();



            // var diff = curretWorld - targetWorld;

            // 

            ;
            //for (int i = 0; i < _currentSkeleton.BoneCount; i++)
            //{
            //    var mappedIndex = _mapping.FirstOrDefault(x => x.OriginalValue == i);
            //    if (mappedIndex != null)
            //    {
            //        var parentBoneId = _currentSkeleton.GetParentBone(i);
            //        var parentBoneMapping = _mapping.FirstOrDefault(x => x.OriginalValue == parentBoneId);
            //_animationClip.DynamicFrames[0].Position[originalBoneIndex] = _currentSkeleton.Translation[newBoneIndex];
            //_animationClip.DynamicFrames[0].Rotation[originalBoneIndex] = _currentSkeleton.Rotation[newBoneIndex];


           // _animationPlayer.GetCurrentFrame().BoneTransforms[0].Translation


           //    }
           //}

            //_currentSkeleton.RebuildSkeletonMatrix();


            //base.OnMappingCreated(originalBoneIndex, newBoneIndex);
        }


        public void Close()
        {
            // Restore animation player
            _componentManager.GetComponent<AnimationsContainerComponent>().Remove(_animationPlayer);
            foreach (var mesh in _meshNodes)
                mesh.AnimationPlayer = _oldAnimationPlayer;

            // Apply changes to mesh

            // Remove the skeleton node
            _componentManager.GetComponent<SceneManager>().RootNode.RemoveObject(_currentSkeletonNode);
        }

        public static void ShowView(List<ISelectable> meshesToFit, IComponentManager componentManager, SkeletonAnimationLookUpHelper skeletonHelper, PackFileService pfs)
        {
            var sceneManager = componentManager.GetComponent<SceneManager>();
            var resourceLib = componentManager.GetComponent<ResourceLibary>();
            var animCollection = componentManager.GetComponent<AnimationsContainerComponent>();

            var meshNodes = meshesToFit
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode)
                .ToList();

            var allSkeltonNames = meshNodes
                .Select(x => x.MeshModel.ParentSkeletonName)
                .Distinct();

            if (allSkeltonNames.Count() != 1)
                throw new Exception("Unexpected number of skeletons. This tool only works for one skeleton");

            var currentSkeletonName = allSkeltonNames.First();
            var currentSkeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, currentSkeletonName);

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x=>(int)x)
                .ToList();

            var targetSkeleton = componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Skeleton;
            var targetSkeletonFile = skeletonHelper.GetSkeletonFileFromName(pfs, targetSkeleton.Name);
            
            RemappedAnimatedBoneConfiguration config = new RemappedAnimatedBoneConfiguration();
            config.ParnetModelSkeletonName= targetSkeleton.Name;
            config.ParentModelBones= AnimatedBone.CreateFromSkeleton(targetSkeletonFile);

            config.MeshSkeletonName = currentSkeletonName;
            config.MeshBones = AnimatedBone.CreateFromSkeleton(currentSkeletonFile, usedBoneIndexes);


            var containingWindow = new Window();
            containingWindow.Title = "Texture Preview Window";
            containingWindow.DataContext = new MeshFitterViewModel(config, meshNodes, targetSkeleton.AnimationProvider.Skeleton, currentSkeletonFile, componentManager);
            containingWindow.Content = new MeshFitterView();
            containingWindow.Closed += ContainingWindow_Closed;
            containingWindow.Show();
        }

        private static void ContainingWindow_Closed(object sender, EventArgs e)
        {
            var window = sender as Window;
            var dataContex = window.DataContext as MeshFitterViewModel;
            dataContex.Close();
            //throw new NotImplementedException();
        }
    }
}
