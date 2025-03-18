using System.Runtime.InteropServices;
using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Vertex;
using SharpDX.MediaFoundation;

namespace Shared.GameFormats.RigidModel
{
    public class ModelFactory
    {
        private readonly ILogger _logger = Logging.Create<ModelFactory>();
        public static ModelFactory Create() => new ModelFactory();

        public ModelFactory()
        {
        }

        public RmvFile Load(byte[] bytes)
        {
            _logger.Here().Information($"Loading RmvFile. Bytes:{bytes.Length}");
            var file = LoadOnlyHeaders(bytes);

            var modelList = new RmvModel[file.Header.LodCount][];
            for (var i = 0; i < file.Header.LodCount; i++)
                modelList[i] = LoadMeshInLod(bytes, file.LodHeaders[i], file.Header.Version);

            file.ModelList = modelList;


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
            for (var meshIndex = 0; meshIndex < header.MeshCount; meshIndex++)
            {
                var offset = (int)header.FirstMeshOffset + sizeOffset;
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

            // Read the indeces
            var faceStart = commonHeader.IndexOffset + modelStartOffset;
            var span = dataArray.AsSpan((int)faceStart, (int)(sizeof(ushort) * commonHeader.IndexCount));
            var ushortSpan = MemoryMarshal.Cast<byte, ushort>(span);
            var indexList = ushortSpan.ToArray();

            var mesh = new RmvMesh()
            {
                VertexList = vertexList,
                IndexList = indexList
            };

            vertexFactory.ReComputeNormals(binaryVertexFormat, ref vertexList, ref indexList);

            return mesh;
        }

        public byte[] Save(RmvFile file)
        {
            _logger.Here().Information("Converting RmvFile to bytes");

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            if (file.LodHeaders.Length != file.Header.LodCount)
                throw new Exception("Unexpected number of Lods");

            _logger.Here().Information("Creating headers");
            writer.Write(ByteHelper.GetBytes(file.Header));
            for (var lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                ValidateLodHeader(file, lodIndex);
                writer.Write(LodHeaderFactory.Create().Save(file.Header.Version, file.LodHeaders[lodIndex]));
            }

            _logger.Here().Information("Creating meshes");
            for (var lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                var models = file.ModelList[lodIndex];
                for (var modelIndex = 0; modelIndex < models.Length; modelIndex++)
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
            using var modelStream = new MemoryStream();
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
    }
}

