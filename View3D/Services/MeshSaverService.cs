using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.SceneNodes;

namespace View3D.Services
{
    public class MeshSaverService
    {
        public static List<float> GetDefaultLodReductionValues(int numLods)
        {
            var output = new List<float>();

            for (int lodIndex = 0; numLods < lodIndex; lodIndex++)
                output.Add(GetDefaultLodReductionValue(numLods, lodIndex));

            return output;
        }

        public static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            var deductionRatio = MathHelper.Lerp(0.25f, 0.75f, lerpValue);
            return deductionRatio;
        }

        /*public static byte[] Save(bool onlySaveVisibleNodes, Rmv2ModelNode modelNode, GameSkeleton skeleton)
        {
            RmvRigidModel outputModel = new RmvRigidModel()
            {
                Header = modelNode.Model.Header.Clone(),
                LodHeaders = modelNode.Model.LodHeaders.Select(x => x.Clone()).ToArray()
            };

            List<string> boneNames = new List<string>();
            if (skeleton != null)
                boneNames = skeleton.BoneNames.ToList();

            var lods = modelNode.GetLodNodes();
            var lodCount = lods.Count;

            RmvSubModel[][] newMeshList = new RmvSubModel[lodCount][];
            for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
            {
                List<Rmv2MeshNode> meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                newMeshList[currentLodIndex] = new RmvSubModel[meshes.Count];

                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    meshes[meshIndex].RecomputeBoundingBox();
                    newMeshList[currentLodIndex][meshIndex] = meshes[meshIndex].CreateRmvSubModel();
                    newMeshList[currentLodIndex][meshIndex].UpdateAttachmentPointList(boneNames);
                }
            }

            outputModel.MeshList = newMeshList;
            outputModel.UpdateOffsets();

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            outputModel.SaveToByteArray(writer);
            return ms.ToArray();
        }*/



        static RmvLodHeader[] CreateLodHeaders(RmvLodHeader baseData, RmvVersionEnum version, uint numLods)
        {
            var output = new RmvLodHeader[numLods];
            for (uint i = 0; i < numLods; i++)
            {
                switch (version)
                {
                    case RmvVersionEnum.RMV2_V6:
                        output[i] = Rmv2LodHeader_V6.CreateFromBase(baseData);
                        break;
                    case RmvVersionEnum.RMV2_V7:
                    case RmvVersionEnum.RMV2_V8:
                        output[i] = Rmv2LodHeader_V7_V8.CreateFromBase(baseData, i);
                        break;

                    default:
                        throw new NotImplementedException("Version not supported");
                }
            }

            return output;
        }

        public static byte[] SaveV2(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton, RmvVersionEnum version )
        {
            uint lodCount = (uint)modelNodes.First().Model.LodHeaders.Length;

            RmvRigidModel header = new RmvRigidModel()
            {
                Header = new RmvModelHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },

                LodHeaders = CreateLodHeaders(modelNodes.First().Model.LodHeaders.First(), version, lodCount)
            };

            // We add all the bone names to the mesh, as its just simpler then trying to figure out which bones are actually needed for attachments
            var boneNames = skeleton?.BoneNames.ToList();

            // Create all the meshes
            List<RmvSubModel>[] newMeshList = new List<RmvSubModel>[lodCount];
            for (int i = 0; i < lodCount; i++)
                newMeshList[i] = new List<RmvSubModel>();

            foreach (var modelNode in modelNodes)
            {
                for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
                {
                    List<Rmv2MeshNode> meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                    {
                        var newMesh = meshes[meshIndex].CreateRmvSubModel(version);
                        newMesh.UpdateAttachmentPointList(boneNames);
                        newMesh.UpdateBoundingBox(BoundingBox.CreateFromPoints(meshes[meshIndex].Geometry.GetVertexList()));

                        newMeshList[currentLodIndex].Add(newMesh);
                    }
                }
            }

            // Convert the list to an array
            RmvSubModel[][] newMeshListArray = new RmvSubModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            // Update data in the header and recalc offset
            header.MeshList = newMeshListArray;
            header.UpdateOffsets(version);

            // Output the data
            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            header.SaveToByteArray(writer);

            return ms.ToArray(); ;
        }



        public static byte[] Save(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton)
        {
            RmvRigidModel outputModel = new RmvRigidModel()
            {
                Header = modelNodes.First().Model.Header.Clone(),
                LodHeaders = modelNodes.First().Model.LodHeaders.Select(x => x.Clone()).ToArray()
            };

            List<string> boneNames = new List<string>();
            if (skeleton != null)
                boneNames = skeleton.BoneNames.ToList();

            var lodCount = outputModel.LodHeaders.Count();

            List<RmvSubModel>[] newMeshList = new List<RmvSubModel>[lodCount];
            for(int i = 0; i < lodCount; i++)
                newMeshList[i] = new List<RmvSubModel>();

            foreach (var modelNode in modelNodes)
            {
                for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
                {
                    var meshNodes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (int meshIndex = 0; meshIndex < meshNodes.Count; meshIndex++)
                    {
                        var newMesh  = meshNodes[meshIndex].CreateRmvSubModel(RmvVersionEnum.RMV2_V7);

                        newMesh.UpdateAttachmentPointList(boneNames);
                        newMesh.UpdateBoundingBox(BoundingBox.CreateFromPoints(meshNodes[meshIndex].Geometry.GetVertexList()));
                    }
                }
            }

            RmvSubModel[][] newMeshListArray = new RmvSubModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            outputModel.MeshList = newMeshListArray;
            outputModel.UpdateOffsets(RmvVersionEnum.RMV2_V7);

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            outputModel.SaveToByteArray(writer);



            return ms.ToArray();
        }

    }
}
