using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Shared.GameFormats.Animation;
using SharpGLTF.Schema2;

namespace MeshImportExport
{
    internal class SkeletonExporter
    {
        public ModelRoot Model { get; private set; }
        public static List<(Node node, Matrix4x4 invMatrix)> CreateSkeletonFromGameSkeleton(AnimationFile file, AnimInvMatrixFile invMatrixFile, ModelRoot model)
        {
            var scene = model.UseScene("default");
            var invMatrixRootBase = invMatrixFile.MatrixList[0];
            var frameBase = file.AnimationParts[0].DynamicFrames[0];

            Dictionary<Node, BoneMetaData> boneMetaData = new();
            var output = new List<(Node node, Matrix4x4 invMatrix)>();
            var rootNode = (scene.CreateNode(file.Bones[0].Name)
                       .WithLocalRotation(new Quaternion(frameBase.Quaternion[0].X, frameBase.Quaternion[0].Y, frameBase.Quaternion[0].Z, frameBase.Quaternion[0].W))
                       .WithLocalTranslation(new Vector3(frameBase.Transforms[0].X, frameBase.Transforms[0].Y, frameBase.Transforms[0].Z)),
                       Matrix4x4.Transpose(new Matrix4x4(invMatrixRootBase.M11, invMatrixRootBase.M12, invMatrixRootBase.M13, invMatrixRootBase.M14,
                                                         invMatrixRootBase.M21, invMatrixRootBase.M22, invMatrixRootBase.M23, invMatrixRootBase.M24,
                                                         invMatrixRootBase.M31, invMatrixRootBase.M32, invMatrixRootBase.M33, invMatrixRootBase.M34,
                                                         invMatrixRootBase.M41, invMatrixRootBase.M42, invMatrixRootBase.M43, invMatrixRootBase.M44)));

            boneMetaData[rootNode.Item1] = new BoneMetaData(file.Bones[0].Name, file.Bones[0].Id);
            output.Add(rootNode);
            var allBonesButFirst = file.Bones.Skip(1);
            foreach (var boneInfo in allBonesButFirst)
            {
                var parent = FindBoneInList(boneMetaData, boneInfo.ParentId, output.Select(x => x.node).ToList());
                if (parent == null)
                {

                }
                else
                {
                    var frame = file.AnimationParts[0].DynamicFrames[0];

                    var invMatrix = invMatrixFile.MatrixList[boneInfo.Id];

                    var newNode = (parent.CreateNode(boneInfo.Name)
                        .WithLocalRotation(new Quaternion(frame.Quaternion[boneInfo.Id].X, frame.Quaternion[boneInfo.Id].Y, frame.Quaternion[boneInfo.Id].Z, frame.Quaternion[boneInfo.Id].W))
                        .WithLocalTranslation(new Vector3(frame.Transforms[boneInfo.Id].X, frame.Transforms[boneInfo.Id].Y, frame.Transforms[boneInfo.Id].Z)),
                        Matrix4x4.Transpose(new Matrix4x4(invMatrix.M11, invMatrix.M12, invMatrix.M13, invMatrix.M14,
                                                          invMatrix.M21, invMatrix.M22, invMatrix.M23, invMatrix.M24,
                                                          invMatrix.M31, invMatrix.M32, invMatrix.M33, invMatrix.M34,
                                                          invMatrix.M41, invMatrix.M42, invMatrix.M43, invMatrix.M44)));

                    output.Add(newNode);
                    boneMetaData[newNode.Item1] = new BoneMetaData(boneInfo.Name, boneInfo.Id);
                }
            }
            return output;
        }

        record BoneMetaData(string Name, int Id);

        static Node? FindBoneInList(Dictionary<Node, BoneMetaData> boneMetaData, int parentId, IEnumerable<Node> boneList)
        {
            foreach (var bone in boneList)
            {
                var metaData = boneMetaData[bone];
                var boneIndex = metaData.Id;
                if (boneIndex == parentId)
                    return bone;
            }

            foreach (var bone in boneList)
            {
                var res = FindBoneInList(boneMetaData, parentId, bone.VisualChildren);
                if (res != null)
                    return res;
            }

            return null;
        }
    }
}
