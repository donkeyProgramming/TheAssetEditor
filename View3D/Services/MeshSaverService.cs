using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using Filetypes.RigidModel.Transforms;
using FileTypes.RigidModel;
using FileTypes.RigidModel.LodHeader;
using FileTypes.RigidModel.MaterialHeaders;
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

        static RmvLodHeader[] CreateLodHeaders(RmvLodHeader baseData, RmvVersionEnum version, uint numLods)
        {
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[numLods];
            for (uint i = 0; i < numLods; i++)
                output[i] = factory.CreateFromBase(version, baseData, i);
            return output;
        }

        public static byte[] Save(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton, RmvVersionEnum version, ModelMaterialEnum modelMaterial)
        {
            uint lodCount = (uint)modelNodes.First().Model.LodHeaders.Length;

            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },

                LodHeaders = CreateLodHeaders(modelNodes.First().Model.LodHeaders.First(), version, lodCount)
            };

            // Create all the meshes
            List<RmvModel>[] newMeshList = new List<RmvModel>[lodCount];
            for (int i = 0; i < lodCount; i++)
                newMeshList[i] = new List<RmvModel>();

            foreach (var modelNode in modelNodes)
            {
                for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
                {
                    List<Rmv2MeshNode> meshes = modelNode.GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                    for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                    {
                        var newModel = new RmvModel()
                        {
                            CommonHeader = meshes[meshIndex].CommonHeader,
                            Material = meshes[meshIndex].Material,
                            Mesh = MeshBuilderService.CreateRmvFileMesh(meshes[meshIndex].Geometry)
                        };

                        var boneNames = new string[0];
                        if (skeleton != null)
                            boneNames = skeleton.BoneNames.ToArray();

                        newModel.Material.UpdateBeforeSave(meshes[meshIndex].Geometry.VertexFormat, version, boneNames);

                        // Update the common header
                        var commonHeader = newModel.CommonHeader;
                        commonHeader.BoundingBox.UpdateBoundingBox(BoundingBox.CreateFromPoints(newModel.Mesh.VertexList.Select(x => x.GetPosistionAsVec3())));
                        commonHeader.ModelTypeFlag = newModel.Material.MaterialId;
                        newModel.CommonHeader = commonHeader;

                        newMeshList[currentLodIndex].Add(newModel);
                    }
                }
            }
            
            // Convert the list to an array
            var newMeshListArray = new RmvModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            // Update data in the header and recalc offset
            outputFile.ModelList = newMeshListArray;
            outputFile.UpdateOffsets();

            // Output the data
            var outputBytes = ModelFactory.Create().Save(outputFile);

            return outputBytes;
        }
    }
}
