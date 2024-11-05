using Editors.ImportExport.Common;
using GameWorld.Core.Animation;
using Shared.GameFormats.Animation;
using SharpGLTF.Schema2;
using SysNum = System.Numerics;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class GltfAnimationCreator
    {

        private readonly AnimationFile _skeletonAnimFile;
        private readonly List<(Node, SysNum.Matrix4x4)> _skeletonNodes;

        public GltfAnimationCreator(List<(Node, SysNum.Matrix4x4)> gltfSkeleton, AnimationFile skeletonAnimFile)
        {
            _skeletonNodes = gltfSkeleton;
            _skeletonAnimFile = skeletonAnimFile;
        }

        public void CreateFromTWAnim(ModelRoot outputScene, AnimationFile animationToExport, bool doMirror)
        {   
            var gameSkeleton = new GameSkeleton(_skeletonAnimFile, null);
            var animationClip = new AnimationClip(animationToExport, gameSkeleton);

            var secondsPerFrame = animationClip.PlayTimeInSec / animationClip.DynamicFrames.Count;

            for (var boneIndex = 0; boneIndex < animationClip.AnimationBoneCount; boneIndex++)
            {
                var translationKeyFrames = new Dictionary<float, SysNum.Vector3>();
                var rotationKeyFrames = new Dictionary<float, SysNum.Quaternion>();

                // populate the bone track containers with the key frames from the .ANIM animation file
                for (var frameIndex = 0; frameIndex < animationClip.DynamicFrames.Count; frameIndex++)
                {
                    translationKeyFrames.Add(secondsPerFrame * frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipVector(animationClip.DynamicFrames[frameIndex].Position[boneIndex], doMirror)));
                    rotationKeyFrames.Add(secondsPerFrame * frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipQuaternion(animationClip.DynamicFrames[frameIndex].Rotation[boneIndex], doMirror)));
                }

                // find ACTUAL nodes, as opposed to "fake/visual"? nodes
                var boneNode = _skeletonNodes[boneIndex].Item1;
                var logicalIndex = boneNode.LogicalIndex;

                if (logicalIndex >= outputScene.LogicalNodes.Count)
                    throw new Exception($"Fatal Error: Incorrect logical node index. logicalIndex={logicalIndex}, modelRoot.LogicalNodes.Count={outputScene.LogicalNodes.Count}");

                var logicalNode = outputScene.LogicalNodes[logicalIndex];

                logicalNode.
                    WithRotationAnimation("", rotationKeyFrames).
                    WithTranslationAnimation("", translationKeyFrames).
                    WithScaleAnimation("", (0.0f, new SysNum.Vector3(1, 1, 1)));
            }
        }
    }
}
