// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using CommonControls.FileTypes.RigidModel.LodHeader;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel.Vertex;
using Serilog;
using SharedCore;
using SharedCore.ByteParsing;

namespace CommonControls.FileTypes.RigidModel
{
    public class ModelFactory
    {
        ILogger _logger = Logging.Create<ModelFactory>();
        public static ModelFactory Create(bool logLoadedFile = false) => new ModelFactory(logLoadedFile);

        bool _logLoadedFile;
        public ModelFactory(bool logLoadedFile = false)
        {
            _logLoadedFile = logLoadedFile;
        }

        public RmvFile Load(byte[] bytes)
        {
            _logger.Here().Information($"Loading RmvFile. Bytes:{bytes.Length}");
            var file = LoadOnlyHeaders(bytes);

            var modelList = new RmvModel[file.Header.LodCount][];
            for (int i = 0; i < file.Header.LodCount; i++)
                modelList[i] = LoadMeshInLod(bytes, file.LodHeaders[i], file.Header.Version);

            file.ModelList = modelList;
            if (_logLoadedFile)
                DumpToLog(file, bytes.Length);

            return file;
        }

        public RmvFile LoadOnlyHeaders(byte[] bytes)
        {
            var fileHeader = ByteHelper.ByteArrayToStructure<RmvFileHeader>(bytes, 0);
            var lodHeaders = LodHeaderFactory.Create().LoadLodHeaders(bytes, RmvFileHeader.HeaderSize, fileHeader.Version, fileHeader.LodCount, out var bytesReadForLodHeaders);

            return new RmvFile()
            {
                Header = fileHeader,
                LodHeaders = lodHeaders,
            };
        }

        RmvModel[] LoadMeshInLod(byte[] modelByteData, RmvLodHeader header, RmvVersionEnum rmvVersion)
        {
            var modelList = new RmvModel[header.MeshCount];

            var sizeOffset = 0;
            for (int meshIndex = 0; meshIndex < header.MeshCount; meshIndex++)
            {
                int offset = (int)header.FirstMeshOffset + sizeOffset;
                modelList[meshIndex] = LoadModel(modelByteData, offset, rmvVersion);
                sizeOffset += (int)modelList[meshIndex].CommonHeader.MeshSectionSize;
            }

            return modelList;
        }

        RmvModel LoadModel(byte[] data, int modelStartOffset, RmvVersionEnum rmvVersionEnum)
        {
            var commonHeader = ByteHelper.ByteArrayToStructure<RmvCommonHeader>(data, modelStartOffset);
            var materialOffset = modelStartOffset + ByteHelper.GetSize<RmvCommonHeader>();
            var meshStart = commonHeader.VertexOffset + modelStartOffset;
            var expectedMaterialSize = meshStart - materialOffset;

            var material = MaterialFactory.Create().LoadMaterial(data, materialOffset, rmvVersionEnum, commonHeader.ModelTypeFlag, expectedMaterialSize);
            var mesh = LoadMesh(rmvVersionEnum, data, commonHeader, material.BinaryVertexFormat, modelStartOffset);

            return new RmvModel()
            {
                CommonHeader = commonHeader,
                Material = material,
                Mesh = mesh
            };
        }

        RmvMesh LoadMesh(RmvVersionEnum rmvVersionEnum, byte[] dataArray, RmvCommonHeader commonHeader, VertexFormat binaryVertexFormat, int modelStartOffset)
        {
            var vertexFactory = VertexFactory.Create();

            var vertexStart = commonHeader.VertexOffset + modelStartOffset;
            var expectedVertexSize = (commonHeader.IndexOffset - commonHeader.VertexOffset) / commonHeader.VertexCount;
            if (vertexFactory.IsKnownVertex(binaryVertexFormat) == false)
                throw new Exception($"Unknown vertex format for {commonHeader.ModelTypeFlag} - {binaryVertexFormat}. Size:{expectedVertexSize}");

            var vertexSize = vertexFactory.GetVertexSize(binaryVertexFormat, rmvVersionEnum);
            if (expectedVertexSize != vertexSize)
                throw new Exception($"Vertex size does not match for {commonHeader.ModelTypeFlag} - {binaryVertexFormat}. Expected: {expectedVertexSize} Actual: {vertexSize}");

            var vertexList = vertexFactory.CreateVertexFromBytes(rmvVersionEnum, binaryVertexFormat, dataArray, (int)commonHeader.VertexCount, (int)vertexStart, (int)expectedVertexSize);

            var faceStart = commonHeader.IndexOffset + modelStartOffset;
            var IndexList = new ushort[commonHeader.IndexCount];
            for (int i = 0; i < commonHeader.IndexCount; i++)
                IndexList[i] = BitConverter.ToUInt16(dataArray, (int)faceStart + sizeof(ushort) * i);

            var mesh = new RmvMesh()
            {
                VertexList = vertexList,
                IndexList = IndexList
            };

            vertexFactory.ReComputeNormals(binaryVertexFormat, ref vertexList, ref IndexList);

            return mesh;
        }

        public byte[] Save(RmvFile file)
        {
            _logger.Here().Information("Converting RmvFile to bytes");

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            if (file.LodHeaders.Length != file.Header.LodCount)
                throw new Exception("Unexpected number of Lods");

            _logger.Here().Information("Creating headers");
            writer.Write(ByteHelper.GetBytes(file.Header));
            for (int lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                ValidateLodHeader(file, lodIndex);
                writer.Write(LodHeaderFactory.Create().Save(file.Header.Version, file.LodHeaders[lodIndex]));
            }

            _logger.Here().Information("Creating meshes");
            for (int lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                var models = file.ModelList[lodIndex];
                for (int modelIndex = 0; modelIndex < models.Length; modelIndex++)
                {
                    if (modelIndex == 0)    // Is first mesh
                    {
                        var expectedStreamIndex = file.LodHeaders[lodIndex].FirstMeshOffset;
                        if (expectedStreamIndex != writer.BaseStream.Position)
                            throw new Exception("Unexpected FirstMeshOffset");
                    }

                    var modelBytes = SaveModel(file.Header.Version, models[modelIndex]);
                    if (models[modelIndex].CommonHeader.MeshSectionSize != modelBytes.Length)
                        throw new Exception("Unexpected MeshSectionSize");

                    writer.Write(modelBytes);
                }
            }

            // Reload the model to make sure we created something the game can load. Better to get an error here then later.
            var bytes = ms.ToArray();

            _logger.Here().Information("Attempting to reload model");
            var reloadedModel = Load(bytes);
            if (reloadedModel == null)
                throw new Exception("Failed to save model - Could not load the result");

            _logger.Here().Information("Converting RmvFile to bytes - Done");
            return bytes;
        }

        private static void ValidateLodHeader(RmvFile file, int lodIndex)
        {
            var expectedMeshCount = file.ModelList[lodIndex].Length;
            if (expectedMeshCount != file.LodHeaders[lodIndex].MeshCount)
                throw new Exception("Unexpected MeshCount");

            var expectedIndexSize = file.ModelList[lodIndex].Sum(x => x.Mesh.IndexList.Length) * sizeof(ushort);
            if (expectedIndexSize != file.LodHeaders[lodIndex].TotalLodIndexSize)
                throw new Exception("Unexpected TotalLodIndexSize");

            var expectedVertexSize = file.ModelList[lodIndex].Sum(x => x.Mesh.VertexList.Length * VertexFactory.Create().GetVertexSize(x.Material.BinaryVertexFormat, file.Header.Version));
            if (expectedVertexSize != file.LodHeaders[lodIndex].TotalLodVertexSize)
                throw new Exception("Unexpected TotalLodVertexSize");
        }

        byte[] SaveModel(RmvVersionEnum rmvVersion, RmvModel model)
        {
            var vertexFactory = VertexFactory.Create();
            using MemoryStream modelStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(modelStream);

            if (model.CommonHeader.IndexCount != model.Mesh.IndexList.Length)
                throw new Exception("Unexpected IndexCount");

            if (model.CommonHeader.VertexCount != model.Mesh.VertexList.Length)
                throw new Exception("Unexpected IndexCount");

            model.UpdateModelTypeFlag(model.Material.MaterialId);
            binaryWriter.Write(ByteHelper.GetBytes(model.CommonHeader));
            binaryWriter.Write(MaterialFactory.Create().Save(model.CommonHeader.ModelTypeFlag, model.Material));

            var vertStart = binaryWriter.BaseStream.Position;
            if (vertStart != model.CommonHeader.VertexOffset)
                throw new Exception("Unexpected VertexOffset");

            foreach (var vertex in model.Mesh.VertexList)
                binaryWriter.Write(vertexFactory.Save(rmvVersion, model.Material.BinaryVertexFormat, vertex));

            var indexOffset = binaryWriter.BaseStream.Position;
            if (indexOffset != model.CommonHeader.IndexOffset)
                throw new Exception("Unexpected IndexOffset");

            foreach (var index in model.Mesh.IndexList)
                binaryWriter.Write(index);

            var modelBytes = modelStream.ToArray();
            if (model.CommonHeader.MeshSectionSize != modelBytes.Length)
                throw new Exception("Unexpected MeshSectionSize");

            return modelBytes;
        }

        public string DumpToLog(RmvFile rmvFile, int totalSize = -1)
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendLine($"\nModel Info: Size:{totalSize}");

            // File header
            strBuilder.AppendLine("\t Header:");
            strBuilder.AppendLine($"\t\t FileType: {rmvFile.Header.FileType}");

            strBuilder.AppendLine($"\t\t Version: {rmvFile.Header.Version}");
            strBuilder.AppendLine($"\t\t LodCount: {rmvFile.Header.LodCount}");
            strBuilder.AppendLine();

            int lodIndex = 0;
            foreach (var lodHeader in rmvFile.LodHeaders)
            {
                strBuilder.AppendLine($"\t Lod Header: [{lodIndex++}] type: {lodHeader.GetType()} size: {lodHeader.GetHeaderSize()}");
                strBuilder.AppendLine($"\t\t MeshCount: {lodHeader.MeshCount}");
                strBuilder.AppendLine($"\t\t TotalLodVertexSize: {lodHeader.TotalLodVertexSize}");
                strBuilder.AppendLine($"\t\t TotalLodIndexSize: {lodHeader.TotalLodIndexSize}");
                strBuilder.AppendLine($"\t\t FirstMeshOffset: {lodHeader.FirstMeshOffset}");
                strBuilder.AppendLine($"\t\t QualityLvl: {lodHeader.QualityLvl}");
                strBuilder.AppendLine($"\t\t LodCameraDistance: {lodHeader.LodCameraDistance}");
            }

            strBuilder.AppendLine();
            for (int i = 0; i < rmvFile.ModelList.Length; i++)
            {
                for (int j = 0; j < rmvFile.ModelList[i].Length; j++)
                {
                    var mesh = rmvFile.ModelList[i][j];
                    strBuilder.AppendLine($"\t Mesh: [{i}][{j}]");

                    strBuilder.AppendLine($"\t\t Common Header:");
                    strBuilder.AppendLine($"\t\t\t ModelTypeFlag: {mesh.CommonHeader.ModelTypeFlag}");
                    strBuilder.AppendLine($"\t\t\t RenderFlag: {mesh.CommonHeader.RenderFlag}");
                    strBuilder.AppendLine($"\t\t\t MeshSectionSize: {mesh.CommonHeader.MeshSectionSize}");
                    strBuilder.AppendLine($"\t\t\t VertexOffset: {mesh.CommonHeader.VertexOffset}");
                    strBuilder.AppendLine($"\t\t\t VertexCount: {mesh.CommonHeader.VertexCount}");
                    strBuilder.AppendLine($"\t\t\t IndexOffset: {mesh.CommonHeader.IndexOffset}");
                    strBuilder.AppendLine($"\t\t\t IndexCount: {mesh.CommonHeader.IndexCount}");

                    strBuilder.AppendLine($"\t\t\t Shader.Name: {mesh.CommonHeader.ShaderParams.ShaderName}");
                    strBuilder.AppendLine($"\t\t\t Shader.UnknownValues: {string.Join("", mesh.CommonHeader.ShaderParams.UnknownValues.Select(x => x.ToString()))}");
                    strBuilder.AppendLine($"\t\t\t Shader.AllZeroValues: {string.Join("", mesh.CommonHeader.ShaderParams.AllZeroValues.Select(x => x.ToString()))}");

                    strBuilder.AppendLine($"\t\t\t BoundingBox: {mesh.CommonHeader.BoundingBox.MinimumX},{mesh.CommonHeader.BoundingBox.MaximumX} | {mesh.CommonHeader.BoundingBox.MinimumY},{mesh.CommonHeader.BoundingBox.MaximumY} | {mesh.CommonHeader.BoundingBox.MinimumZ},{mesh.CommonHeader.BoundingBox.MaximumZ}");
                    strBuilder.AppendLine($"\t\t\t BoundingBox.Width {mesh.CommonHeader.BoundingBox.Width}");
                    strBuilder.AppendLine($"\t\t\t BoundingBox.Height {mesh.CommonHeader.BoundingBox.Height}");
                    strBuilder.AppendLine($"\t\t\t BoundingBox.Depth {mesh.CommonHeader.BoundingBox.Depth}");

                    var vertexList = mesh.Mesh.VertexList.Select(x => x.GetPosistionAsVec3());
                    var computedBB = Microsoft.Xna.Framework.BoundingBox.CreateFromPoints(vertexList);

                    strBuilder.AppendLine($"\t\t\t Computed BoundingBox: {computedBB.Min.X},{computedBB.Max.X} | {computedBB.Min.Y},{computedBB.Max.Y}  | {computedBB.Min.Z},{computedBB.Max.Z} ");
                    strBuilder.AppendLine($"\t\t\t Computed BoundingBox.Width {Math.Abs(computedBB.Min.X - computedBB.Max.X)}");
                    strBuilder.AppendLine($"\t\t\t Computed BoundingBox.Height {Math.Abs(computedBB.Min.Y - computedBB.Max.Y)}");
                    strBuilder.AppendLine($"\t\t\t Computed BoundingBox.Depth {Math.Abs(computedBB.Min.Z - computedBB.Max.Z)}");

                    strBuilder.AppendLine($"\t\t Material: type: {mesh.Material.GetType()} size: {mesh.Material.ComputeSize()}");
                    strBuilder.AppendLine($"\t\t\t MaterialId: {mesh.Material.MaterialId}");
                    strBuilder.AppendLine($"\t\t\t BinaryVertexFormat: {mesh.Material.BinaryVertexFormat}");
                    strBuilder.AppendLine($"\t\t\t PivotPoint: {mesh.Material.PivotPoint}");
                    strBuilder.AppendLine($"\t\t\t AlphaMode: {mesh.Material.AlphaMode}");
                    strBuilder.AppendLine($"\t\t\t ModelName: {mesh.Material.ModelName}");
                    strBuilder.AppendLine($"\t\t\t TextureDirectory: {mesh.Material.TextureDirectory}");

                    strBuilder.AppendLine($"\t\t\t Textures:");
                    foreach (var texture in mesh.Material.GetAllTextures())
                        strBuilder.AppendLine($"\t\t\t\t Texture: {texture.TexureType} - {texture.Path}");

                    strBuilder.AppendLine($"\t\t Mesh:");
                    strBuilder.AppendLine($"\t\t\t VertexList.Length: {mesh.Mesh.VertexList.Length}");
                    strBuilder.AppendLine($"\t\t\t IndexList.Length: {mesh.Mesh.IndexList.Length}");
                }
            }
            var output = strBuilder.ToString();
            _logger.Here().Information(output);
            return output;
        }
    }
}

