using Editors.ImportExport.Common;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using SharpGLTF.Animations;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
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
    public class GltfAnimationTrackSampler
    {
        static public Vector3 SampleTranslation(ModelRoot model, string boneName, float time)
        {
            // Access the first animation
            var animation = model.LogicalAnimations[0];

            // Access the first node
            var node = model.LogicalNodes.FirstOrDefault(n => n.Name.ToLower() == boneName.ToLower());

            if (node == null)
            {
                throw new Exception("Error, Unexpected, Node not found");
            }

            // Get the translation sampler for the node
            var translationSampler = animation.FindTranslationChannel(node).GetTranslationSampler().CreateCurveSampler();            

            var translationVector = translationSampler.GetPoint(time);

            return GlobalSceneTransforms.FlipVector(translationVector, true);
        }

        static public Quaternion SampleQuaternion(ModelRoot model, string boneName, float time)
        {
            // Access the first animation
            var animation = model.LogicalAnimations[0];

            // Access the first nodef
            var node = model.LogicalNodes.FirstOrDefault(n => n.Name.ToLower() == boneName.ToLower());

            if (node == null)
            {
                throw new Exception("Error, Unexpected, Node not found");
            }

            // Get the translation sampler for the node 
            var quaternionSampler = animation.FindRotationChannel(node).GetRotationSampler().CreateCurveSampler();

            var outQuaternion = quaternionSampler.GetPoint(time);

            return GlobalSceneTransforms.FlipQuaternion(outQuaternion, true);
        }

    }
    internal class FramePoseMatrixCalculator
    {
        private readonly AnimationFile _animationFile;
        private readonly SkeletonFrameNode[] _nodeList;
        private Matrix[] _worldTransform;

        public FramePoseMatrixCalculator(AnimationFile file)
        {
            ValidDateSkeletonFile(file);

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

        public List<Matrix> GetInverseBindPoseMatrices(bool doMirror)
        {
            RebuildSkeletonGlobalMatrices(doMirror);

            var output = new List<Matrix>();
            for (var i = 0; i < _worldTransform.Length; i++)
            {
                var invBindPoseMatrix = Matrix.Invert(_worldTransform[i]);
                output.Add(invBindPoseMatrix);
            }

            return output;
        }

        /// <summary>
        /// Checks thhat the input file is a valid skeleton file
        /// </summary>            
        private static void ValidDateSkeletonFile(AnimationFile file)
        {
            if (file.Bones.Length == 0)
                throw new Exception("No bones!");

            if (file.AnimationParts.Count == 0)
                throw new Exception($"No anim parts! Bone Count: {file.Bones.Length}");

            if (file.AnimationParts[0].DynamicFrames.Count == 0)
                throw new Exception($"No anim frames, in part 0!  Bone Count: {file.Bones.Length}, Anim Parts Count: {file.AnimationParts.Count}");

            if (file.AnimationParts[0].DynamicFrames[0].Quaternion.Count != file.Bones.Length)
                throw new Exception($"Not a valid skeleton file, doesn't contain quaternion values for all bones! Quat count frame 0: {file.AnimationParts[0].DynamicFrames[0].Quaternion.Count}, Bone count {file.Bones.Length}");

            if (file.AnimationParts[0].DynamicFrames[0].Transforms.Count != file.Bones.Length)
                throw new Exception($"Not a valid skeleton file, doesn't contain translation values for all bones! Trans count frame 0: {file.AnimationParts[0].DynamicFrames[0].Transforms.Count}, Bone count {file.Bones.Length}");
        }

        private void RebuildSkeletonGlobalMatrices(bool doMirror)
        {
            _worldTransform = new Matrix[_animationFile.Bones.Length];
            for (var boneIndex = 0; boneIndex < _animationFile.Bones.Length; boneIndex++)
            {
                var translationMatrix = Matrix.CreateTranslation(GlobalSceneTransforms.FlipVector(_animationFile.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex].ToVector3(), doMirror));
                var rotationMatrix = Matrix.CreateFromQuaternion(GlobalSceneTransforms.FlipQuaternion(_animationFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].ToQuaternion(), doMirror));
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
