using System.Numerics;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters.GltfSkeleton;
using GameWorld.Core.Animation;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using SharpGLTF.Schema2;
using SysNum = System.Numerics;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class ProcessedGltfSkeleton
    {
        public required List<(Node, Matrix4x4)> Data { get; set; }
    }

    public class GltfAnimationCreator
    {
        private readonly IPackFileService _packFileService;

        public GltfAnimationCreator(IPackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public ProcessedGltfSkeleton? CreateAnimationAndSkeleton(string? skeletonNameFromRmv2, ModelRoot outputScene, RmvToGltfExporterSettings settings)
        {
            if(string.IsNullOrWhiteSpace(skeletonNameFromRmv2))
                return null;

            var skeletonAnimFile = FetchAnimSkeleton(skeletonNameFromRmv2);
            var gltfSkeleton = CreateSkeleton(outputScene, skeletonAnimFile, settings.MirrorMesh);

            if (settings.ExportAnimations == false)
                return gltfSkeleton;

            foreach (var animation in settings.InputAnimationFiles)
            {
                var animationToExport = AnimationFile.Create(animation);
                CreateFromTWAnim(gltfSkeleton, skeletonAnimFile, animationToExport, outputScene, settings);
            }

            return gltfSkeleton;
        }

        ProcessedGltfSkeleton CreateSkeleton(ModelRoot outputScene, AnimationFile animSkeletonFil, bool doMirror)
        {
            if (animSkeletonFil.AnimationParts.Count == 0)
                throw new Exception("No AnimationParts found in AnimationFile");

            if (animSkeletonFil.AnimationParts[0].DynamicFrames.Count == 0)
                throw new Exception("No DynamicFrames found in AnimationPart");

            var frame = animSkeletonFil.AnimationParts[0].DynamicFrames[0];

            var framePoseMatrixCalculator = new FramePoseMatrixCalculator(animSkeletonFil);
            var invMatrices = framePoseMatrixCalculator.GetInverseBindPoseMatrices(doMirror);

            var outputGltfBindings = new List<(Node node, Matrix4x4 invMatrix)>();

            var scene = outputScene.UseScene("default");

            scene.CreateNode($"//skeleton//{animSkeletonFil.Header.SkeletonName.ToLower()}");

            var parentIdToGltfNode = new Dictionary<int, Node>();
            parentIdToGltfNode[-1] = scene.CreateNode(""); // bones with no parent will be children of the scene

            for (var boneIndex = 0; boneIndex < animSkeletonFil.Bones.Length; boneIndex++)
            {
                var parentNode = parentIdToGltfNode[animSkeletonFil.Bones[boneIndex].ParentId];
                if (parentNode == null)
                    throw new Exception($"Parent Node not found for boneIndex={boneIndex}");

                parentIdToGltfNode[boneIndex] = parentNode.CreateNode(animSkeletonFil.Bones[boneIndex].Name);

                parentIdToGltfNode[boneIndex].
                    WithLocalTranslation(VecConv.GetSys(GlobalSceneTransforms.FlipVector(frame.Transforms[boneIndex].ToVector3(), doMirror))).
                    WithLocalRotation(VecConv.GetSys(GlobalSceneTransforms.FlipQuaternion(frame.Quaternion[boneIndex].ToQuaternion(), doMirror))).
                    WithLocalScale(new System.Numerics.Vector3(1, 1, 1));

                var invBindPoseMatrix4x4 = VecConv.GetSys(invMatrices[boneIndex]);

                outputGltfBindings.Add((parentIdToGltfNode[boneIndex], invBindPoseMatrix4x4));
            }

            return new ProcessedGltfSkeleton() { Data = outputGltfBindings };
        }

        AnimationFile FetchAnimSkeleton(string skeletonNameFromRmv2)
        {
            var skeletonName = skeletonNameFromRmv2 + ".anim";
            
            var skeletonSearchList = PackFileServiceUtility.SearchForFile(_packFileService, skeletonName);
            var skeletonPath = _packFileService.GetFullPath(_packFileService.FindFile(skeletonSearchList[0]));
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);

            var animSkeletonFile = AnimationFile.Create(skeletonPackFile);
            return animSkeletonFile;
        }


        void CreateFromTWAnim(ProcessedGltfSkeleton gltfSkeleton, AnimationFile skeletonAnimFile, AnimationFile animationToExport, ModelRoot outputScene, RmvToGltfExporterSettings settings)
        {
            var doMirror = settings.MirrorMesh;
            var gameSkeleton = new GameSkeleton(skeletonAnimFile, null);
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
                var boneNode = gltfSkeleton.Data[boneIndex].Item1;
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
