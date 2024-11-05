using System.Collections.Generic;
using GameWorld.Core;
using GameWorld.Core.Components;
using System.Windows.Input;
using System.Windows.Navigation;
using Shared.GameFormats.Animation;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using SharpDX.Direct3D9;
using SharpGLTF.Schema2;
using GameWorld.Core.Animation;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Scenes;
using SharpGLTF.Animations;
using SysNum = System.Numerics;
using Editors.ImportExport.Common;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class GltfAnimationCreator
    {
        private readonly AnimationsContainerComponent _animationsContainerComponent = new AnimationsContainerComponent();
        private readonly AnimationPlayer _animationPlayer;
        private readonly AnimationFile _skeletonAnimFile;

        public List<(Node, SysNum.Matrix4x4)> SkeletonNodes { get; set; }

        public GltfAnimationCreator(List<(Node, SysNum.Matrix4x4)> nodeData, AnimationFile skeletonAnimFile)
        {
            SkeletonNodes = nodeData;
            _animationPlayer = _animationsContainerComponent.RegisterAnimationPlayer(new AnimationPlayer(), "MainPlayer");
            _skeletonAnimFile = skeletonAnimFile;
        }

        public void CreateFromTWAnim(AnimationFile animationFile, ModelRoot modelRoot)
        {
            var gameSkeleton = new GameSkeleton(_skeletonAnimFile, _animationPlayer);
            var animationClip = new AnimationClip(animationFile, gameSkeleton);

            var secondsPerFrame = animationClip.PlayTimeInSec / animationClip.DynamicFrames.Count;

            for (var boneIndex = 0; boneIndex < animationClip.AnimationBoneCount; boneIndex++)
            {
                var translationKeyFrames = new Dictionary<float, SysNum.Vector3>();
                var rotationKeyFrames = new Dictionary<float, SysNum.Quaternion>();

                // populate the bone track containers with the key frames from the .ANIM animation file
                for (var frameIndex = 0; frameIndex < animationClip.DynamicFrames.Count; frameIndex++)
                {
                    translationKeyFrames.Add(secondsPerFrame * frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipVector(animationClip.DynamicFrames[frameIndex].Position[boneIndex])));
                    rotationKeyFrames.Add(secondsPerFrame * frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipQuaternion(animationClip.DynamicFrames[frameIndex].Rotation[boneIndex])));
                }

                // find ACTUAL nodes, as opposed to "fake/visual"? nodes
                var boneNode = SkeletonNodes[boneIndex].Item1;
                var logicalIndex = boneNode.LogicalIndex;

                if (logicalIndex >= modelRoot.LogicalNodes.Count)
                    throw new Exception("Fatal Error: Incorrect logical node index");

                var logicalNode = modelRoot.LogicalNodes[logicalIndex];

                logicalNode.
                    WithRotationAnimation("", rotationKeyFrames).
                    WithTranslationAnimation("", translationKeyFrames).
                    WithScaleAnimation("", (0.0f, new SysNum.Vector3(1, 1, 1)));
            }
        }
        static private SysNum.Vector3 ToVector3(Vector3 v) => new SysNum.Vector3(v.X, v.Y, v.Z);
        static private SysNum.Quaternion ToQuaternion(Quaternion q) => new SysNum.Quaternion(q.X, q.Y, q.Z, q.W);
    }
}
