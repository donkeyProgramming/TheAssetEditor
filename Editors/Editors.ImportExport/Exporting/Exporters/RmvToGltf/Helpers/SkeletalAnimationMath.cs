using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using Editors.ImportExport.Exporting.Exporters.GltfSkeleton;
using Shared.GameFormats.Animation;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using SharpDX.Direct3D9;
using SharpGLTF.Schema2;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Scenes;
using SharpGLTF.Animations;
using SysNum = System.Numerics;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Common;

namespace Editors.ImportExport.Exporting.Exporters.GltfSkeleton
{
    public class TransformData
    {

        public Vector3 LocalTranslation { get; set; } = new Vector3(0, 0, 0);
        public Quaternion LocalRotation { get; set; } = Quaternion.Identity;

        public Vector3 GlobalTranslation { get; set; } = new Vector3(0, 0, 0);
        public Quaternion GlobalRotation { get; set; } = Quaternion.Identity;
    }

    public class SkeletonFrameNode
    {
        public string Name { get; set; } = "";
        public int ParentId { get; set; }
        public int Id { get; set; }
        public TransformData Transform { get; set; } = new TransformData();

        public Matrix GlobalTransform
        {
            get
            {
                var translationMatrix = Matrix.CreateTranslation(Transform.GlobalTranslation);
                var rotationMatrix = Matrix.CreateFromQuaternion(Transform.GlobalRotation);
                var framePoseMatrix = translationMatrix * rotationMatrix;
                return framePoseMatrix;
            }
        }
    }

    internal class FramePoseMatrixCalculator
    {
        private readonly AnimationFile _animationFile;
        private readonly SkeletonFrameNode[] _nodeList;
        private Matrix[] _worldTransform;

        public FramePoseMatrixCalculator(AnimationFile file)
        {
            ValidDateAnimationFIle(file);

            _animationFile = file;
            _nodeList = new SkeletonFrameNode[file.Bones.Length];
            _worldTransform = Enumerable.Repeat(Matrix.Identity, file.Bones.Length).ToArray();

            for (var boneIndex = 0; boneIndex < file.Bones.Length; boneIndex++)
            {
                _nodeList[boneIndex] = new SkeletonFrameNode()
                {
                    Id = file.Bones[boneIndex].Id,
                    ParentId = file.Bones[boneIndex].ParentId
                };
            }
        }

        public List<Matrix> GetInverseBindPoseMatrices()
        {
            RebuildSkeletonGlobalMatrices();

            var output = new List<Matrix>();
            for (var i = 0; i < _worldTransform.Length; i++)
            {
                var invBindPoseMatrix = Matrix.Invert(_worldTransform[i]);
                output.Add(invBindPoseMatrix);
            }

            return output;
        }

        private static void ValidDateAnimationFIle(AnimationFile file)
        {
            if (file.Bones.Length == 0)
                throw new Exception("No bones!");

            if (file.AnimationParts.Count == 0)
                throw new Exception("No anim parts!");

            if (file.AnimationParts[0].DynamicFrames.Count == 0)
                throw new Exception("No anim frames!");

            if (file.AnimationParts[0].DynamicFrames.Count == 0)
                throw new Exception("No anim frames!");

            if (file.AnimationParts[0].DynamicFrames[0].Quaternion.Count != file.Bones.Length)
                throw new Exception("No anim frames!");
        }

        private void RebuildSkeletonGlobalMatrices()
        {
            _worldTransform = new Matrix[_animationFile.Bones.Length];
            for (var boneIndex = 0; boneIndex < _animationFile.Bones.Length; boneIndex++)
            {
                var translationMatrix = Matrix.CreateTranslation(GlobalSceneTransforms.FlipVector(_animationFile.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex].ToVector3()));
                var rotationMatrix = Matrix.CreateFromQuaternion(GlobalSceneTransforms.FlipQuaternion(_animationFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].ToQuaternion()));
                var scaleMatrix = Matrix.CreateScale(1, 1, 1);
                var transform = scaleMatrix * rotationMatrix * translationMatrix;
                _worldTransform[boneIndex] = transform;
            }

            for (var i = 0; i < _worldTransform.Length; i++) 
            {
                var parentIndex = _animationFile.Bones[i].ParentId;

                if (parentIndex == -1)
                    continue;

                _worldTransform[i] = _worldTransform[i] * _worldTransform[parentIndex];
            }
        }
    };
}
