using System.Numerics;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters.GltfSkeleton;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
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

    public class GltfSkeletonBuilder
    {
        private readonly IPackFileService _packFileService;        

        public GltfSkeletonBuilder(IPackFileService packFileService)
        {
            _packFileService = packFileService;            
        }

        public ProcessedGltfSkeleton CreateSkeleton(AnimationFile skeletonAnimFile, ModelRoot outputScene, RmvToGltfExporterSettings settings)
        {           
            var gltfSkeleton = CreateSkeleton(outputScene, skeletonAnimFile, settings.MirrorMesh);
                        
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


    }
}
