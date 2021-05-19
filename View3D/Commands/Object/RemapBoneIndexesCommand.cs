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

            _scene.RootNode.AddObject(new SkeletonNode(_rl.Content, new TempSkelProv() { Skeleton = _currentSkeleton }, "NewSkelly"));

            int vertexCount = _meshOwner.Geometry.VertexCount();
            for (int i = 0; i < vertexCount; i++)
            {
                /* var vert = (_meshOwner.Geometry as Rmv2Geometry).GetVertexExtented(i);

                 Matrix[] mList = new Matrix[4];
                 var boneIndexes = vert.GetBoneIndexs();
                 var boneWeights = vert.GetBoneIndexs();
                 for (int b = 0; b < boneIndexes.Length; b++)
                 {
                     mList[b] = Matrix.Identity;

                     var originalMatrix = _currentSkeleton.GetWorldTransform(boneIndexes[b]);

                     var mapping = _mapping.FirstOrDefault(x => x.OriginalValue == boneIndexes[b]);
                     if (mapping != null)
                     {
                         var newMatrix = _targetSkeleton.GetWorldTransform(mapping.NewValue);
                         originalMatrix.Decompose(out var _, out var orgRot ,out var orgPos);
                         newMatrix.Decompose(out var _, out var newRot ,out var newPos);

                         var diff = newPos- orgPos;
                         var riderRotationDiff = newRot * Quaternion.Inverse(orgRot);
                         //mList[b] = Matrix.CreateFromQuaternion(riderRotationDiff) *  Matrix.CreateTranslation(diff);
                          mList[b] = (originalMatrix * Matrix.Invert(newMatrix));
                     }

                 }

                 float w1 = vert.BlendWeights.X;
                 float w2 = vert.BlendWeights.Y;
                 float w3 = vert.BlendWeights.Z;
                 float w4 = vert.BlendWeights.W;

                 Matrix transformSum = Matrix.Identity;
                 Matrix m1 = mList[0];
                 Matrix m2 = mList[1];
                 Matrix m3 = mList[2];
                 Matrix m4 = mList[3];
                 transformSum.M11 = (m1.M11 * w1) + (m2.M11 * w2) + (m3.M11 * w3) + (m4.M11 * w4);
                 transformSum.M12 = (m1.M12 * w1) + (m2.M12 * w2) + (m3.M12 * w3) + (m4.M12 * w4);
                 transformSum.M13 = (m1.M13 * w1) + (m2.M13 * w2) + (m3.M13 * w3) + (m4.M13 * w4);
                 transformSum.M21 = (m1.M21 * w1) + (m2.M21 * w2) + (m3.M21 * w3) + (m4.M21 * w4);
                 transformSum.M22 = (m1.M22 * w1) + (m2.M22 * w2) + (m3.M22 * w3) + (m4.M22 * w4);
                 transformSum.M23 = (m1.M23 * w1) + (m2.M23 * w2) + (m3.M23 * w3) + (m4.M23 * w4);
                 transformSum.M31 = (m1.M31 * w1) + (m2.M31 * w2) + (m3.M31 * w3) + (m4.M31 * w4);
                 transformSum.M32 = (m1.M32 * w1) + (m2.M32 * w2) + (m3.M32 * w3) + (m4.M32 * w4);
                 transformSum.M33 = (m1.M33 * w1) + (m2.M33 * w2) + (m3.M33 * w3) + (m4.M33 * w4);
                 transformSum.M41 = (m1.M41 * w1) + (m2.M41 * w2) + (m3.M41 * w3) + (m4.M41 * w4);
                 transformSum.M42 = (m1.M42 * w1) + (m2.M42 * w2) + (m3.M42 * w3) + (m4.M42 * w4);
                 transformSum.M43 = (m1.M43 * w1) + (m2.M43 * w2) + (m3.M43 * w3) + (m4.M43 * w4);

                 _meshOwner.Geometry.TransformVertex(i, transformSum);
                 */

                var vertTransform = meshAnimationHelper.GetVertexTransform(animationFrame, i);
                //vertTransform.Decompose(out var _, out var mountVertexRot, out var mountVertexPos);
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
