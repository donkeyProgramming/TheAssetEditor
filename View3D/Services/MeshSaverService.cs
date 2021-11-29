using Common;
using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using Filetypes.RigidModel.Transforms;
using FileTypes.RigidModel;
using FileTypes.RigidModel.LodHeader;
using FileTypes.RigidModel.MaterialHeaders;
using Microsoft.Xna.Framework;
using Serilog;
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
        static ILogger GetLogger() => Logging.Create<MeshSaverService>();

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

        static RmvLodHeader[] CreateLodHeaders(RmvLodHeader[] baseHeaders, RmvVersionEnum version)
        {
            var numLods = baseHeaders.Count();
            var factory = LodHeaderFactory.Create();
            var output = new RmvLodHeader[numLods];
            for(int i = 0; i < numLods; i++)
                output[i] = factory.CreateFromBase(version, baseHeaders[i], (uint)i);
            return output;
        }

        public static byte[] Save(bool onlySaveVisibleNodes, List<Rmv2ModelNode> modelNodes, GameSkeleton skeleton, RmvVersionEnum version, ModelMaterialEnum modelMaterial)
        {
            var logger = GetLogger();
            logger.Here().Information($"Starting to save model. Nodes = {modelNodes.Count}, Skeleton = {skeleton}, Version = {version}");

            uint lodCount = (uint)modelNodes.First().Model.LodHeaders.Length;

            logger.Here().Information($"Creating header");
            RmvFile outputFile = new RmvFile()
            {
                Header = new RmvFileHeader()
                {
                    _fileType = Encoding.ASCII.GetBytes("RMV2"),
                    SkeletonName = skeleton == null ? "" : skeleton.SkeletonName,
                    Version = version,
                    LodCount = lodCount
                },

                LodHeaders = CreateLodHeaders(modelNodes.First().Model.LodHeaders, version)
            };

            // Create all the meshes
            logger.Here().Information($"Creating meshes");
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
                        logger.Here().Information($"Creating model. Lod: {currentLodIndex}, Model: {meshIndex}");

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

                        logger.Here().Information($"Model. Lod: {currentLodIndex}, Model: {meshIndex} created.");
                        newMeshList[currentLodIndex].Add(newModel);
                    }
                }
            }
            
            // Convert the list to an array
            var newMeshListArray = new RmvModel[lodCount][];
            for (int i = 0; i < lodCount; i++)
                newMeshListArray[i] = newMeshList[i].ToArray();

            // Update data in the header and recalc offset
            logger.Here().Information($"Update offsets");
            outputFile.ModelList = newMeshListArray;
            outputFile.UpdateOffsets();

            // Output the data
            logger.Here().Information($"Generating bytes.");
            var outputBytes = ModelFactory.Create().Save(outputFile);

            logger.Here().Information($"Model saved correctly");
            return outputBytes;
        }
    }
}
