using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Components.Component;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Commands.Object
{
    class TempSkelProv : IAnimationProvider
    {
        public bool IsActive => true;

        public GameSkeleton Skeleton { get; set; }
    }


    public class RemapBoneIndexesCommand : CommandBase<RemapBoneIndexesCommand>
    {
        List<IndexRemapping> _mapping;
        string _newSkeletonName;

        Rmv2MeshNode _meshOwner;
        IGeometry _originalGeometry;
        string _originalSkeletonName;

        bool _moveMeshToFit;
        GameSkeleton _targetSkeleton;
        GameSkeleton _currentSkeleton;

        public RemapBoneIndexesCommand(Rmv2MeshNode meshOwner, List<IndexRemapping> mapping, string newSkeletonName, bool moveMeshToFit, GameSkeleton currentSkeleton, GameSkeleton targetSkeleton)
        {
            _meshOwner = meshOwner;
            _mapping = mapping;
            _newSkeletonName = newSkeletonName;
            _moveMeshToFit = moveMeshToFit;
            _currentSkeleton = currentSkeleton;
            _targetSkeleton = targetSkeleton;
        }


        public override string GetHintText()
        {
            return "Remap skeleton";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _scene = componentManager.GetComponent<SceneManager>();
            _rl = componentManager.GetComponent<ResourceLibary>();
        }

        SceneManager _scene;
        ResourceLibary _rl;


        protected override void ExecuteCommand()
        {
            _originalGeometry = _meshOwner.Geometry.Clone();
            _originalSkeletonName = _meshOwner.MeshModel.ParentSkeletonName;



            AnimationClip c = new AnimationClip();
            c.DynamicFrames.Add(new AnimationClip.KeyFrame());

            for (int i = 0; i < _currentSkeleton.BoneCount; i++)
            {
                c.DynamicFrames[0].Rotation.Add(Quaternion.Identity);
                c.DynamicFrames[0].Position.Add(Vector3.Zero);

                c.RotationMappings.Add(new Filetypes.RigidModel.AnimationFile.AnimationBoneMapping(i));
                c.TranslationMappings.Add(new Filetypes.RigidModel.AnimationFile.AnimationBoneMapping(i));
            }

                // for (int i = 0; i < _currentSkeleton.BoneCount; i++)
                //     _currentSkeleton.SetBoneTransform(i, Quaternion.Identity, Vector3.Zero, false);


          for (int i = 0; i < _currentSkeleton.BoneCount; i++)
          {
        


                var mappedIndex = _mapping.FirstOrDefault(x => x.OriginalValue == i);
              if (mappedIndex != null)
              {
                  var parentBoneId = _currentSkeleton.GetParentBone(i);
                  var parentBoneMapping = _mapping.FirstOrDefault(x => x.OriginalValue == parentBoneId);
                  if (parentBoneMapping == null && false)
                  {
                        var targetBoneWorldMatrix = _targetSkeleton.GetWorldTransform(mappedIndex.NewValue);
                        targetBoneWorldMatrix.Decompose(out var _, out var rot, out var trans);
                        //_currentSkeleton.SetBoneTransform(i, rot, trans, false);

                        c.DynamicFrames[0].Position[i] = trans;
                        c.DynamicFrames[0].Rotation[i] = rot;
                    }
                  else
                  {
                        c.DynamicFrames[0].Position[i] = _targetSkeleton.Translation[mappedIndex.NewValue];
                        c.DynamicFrames[0].Rotation[i] = _targetSkeleton.Rotation[mappedIndex.NewValue];

                        //_currentSkeleton.SetBoneTransform(i, _targetSkeleton.Rotation[mappedIndex.NewValue], _targetSkeleton.Translation[mappedIndex.NewValue], false);
                        //_currentSkeleton.SetBoneTransform(i, Quaternion.Identity, _targetSkeleton.Translation[mappedIndex.NewValue], false);
                    }
              }
          }
          
           _currentSkeleton.RebuildSkeletonMatrix();

            
  

            MeshAnimationHelper meshAnimationHelper = new MeshAnimationHelper(_meshOwner, Matrix.Identity);
            var animationFrame = _currentSkeleton.CreateAnimationFrame();
            animationFrame = AnimationSampler.Sample(0, 0, _currentSkeleton, c);

            _currentSkeleton.SetAnimationFrame(animationFrame);
            //_scene.RootNode.AddObject(new SkeletonNode(_rl.Content, new TempSkelProv() { Skeleton = _currentSkeleton }, "NewSkelly"));

            int vertexCount = _meshOwner.Geometry.VertexCount();
            for (int i = 0; i < vertexCount; i++)
            {
               
                var vertTransform = meshAnimationHelper.GetVertexTransform(animationFrame, i);
                _meshOwner.Geometry.TransformVertex(i, (vertTransform));
            }

            _meshOwner.Geometry.RebuildVertexBuffer();
            _meshOwner.Geometry.UpdateAnimationIndecies(_mapping);
            _meshOwner.MeshModel.ParentSkeletonName = _newSkeletonName;

        }



        protected override void UndoCommand()
        {
            _meshOwner.Geometry = _originalGeometry;
            _meshOwner.MeshModel.ParentSkeletonName = _originalSkeletonName;
        }
    }    
}
