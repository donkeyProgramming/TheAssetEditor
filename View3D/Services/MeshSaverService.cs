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




        public static byte[] Save(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton)
        {

            // Create new header
            // Copy load headers
            // Create new model headers 
            // Create mesh data


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
                    List<Rmv2MeshNode> meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                    {
                        meshes[meshIndex].RecomputeBoundingBox();

                        var newMesh  = meshes[meshIndex].CreateRmvSubModel();
                        newMesh.UpdateAttachmentPointList(boneNames);

                        newMeshList[currentLodIndex].Add(newMesh);
                    }
                }
            }

            RmvSubModel[][] newMeshListArray = new RmvSubModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            outputModel.MeshList = newMeshListArray;
            outputModel.UpdateOffsets();

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            outputModel.SaveToByteArray(writer);
            return ms.ToArray();
        }

    }
}
