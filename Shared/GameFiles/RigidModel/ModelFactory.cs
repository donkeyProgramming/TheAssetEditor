using System.Runtime.InteropServices;
using Shared.ByteParsing;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel.Vertex.Formats;

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

            // A tree's furthest LOD is a generated billboard laid out material, indices, vertices.
            // Detection is deliberately narrow: the indices must exactly fill the gap before the vertices.
            var indicesFirst = commonHeader.IndexOffset > 0 && commonHeader.IndexOffset < commonHeader.VertexOffset
                && commonHeader.IndexOffset + commonHeader.IndexCount * sizeof(ushort) == commonHeader.VertexOffset;
            var materialEnd = indicesFirst ? commonHeader.IndexOffset : commonHeader.VertexOffset;
            var expectedMaterialSize = modelStartOffset + materialEnd - materialOffset;

            var material = MaterialFactory.Create().LoadMaterial(data, materialOffset, rmvVersionEnum, commonHeader.ModelTypeFlag, expectedMaterialSize);
            var mesh = LoadMesh(rmvVersionEnum, data, commonHeader, material.BinaryVertexFormat, modelStartOffset, indicesFirst);

            var indexEnd = commonHeader.IndexOffset + commonHeader.IndexCount * sizeof(ushort);
            if (indicesFirst == false && commonHeader.MeshSectionSize > indexEnd)
                mesh.SectionTail = data.AsSpan((int)(modelStartOffset + indexEnd), (int)(commonHeader.MeshSectionSize - indexEnd)).ToArray();

            return new RmvModel()
            {
                CommonHeader = commonHeader,
                Material = material,
                Mesh = mesh
            };
        }

        RmvMesh LoadMesh(RmvVersionEnum rmvVersionEnum, byte[] dataArray, RmvCommonHeader commonHeader, VertexFormat binaryVertexFormat, int modelStartOffset, bool indicesFirst)
        {
            var vertexFactory = VertexFactory.Create();
            var indexList = ReadIndices(dataArray, commonHeader, modelStartOffset);

            // Some meshes ship no vertex block: a rope declares no vertices (the game sweeps a tube
            // along the node chain in its material's physics block), and Warhammer 3's decals and
            // static point lights declare a count but leave the geometry for the game to build.
            // The header's count is kept, so the file saves back as it was read.
            if (commonHeader.VertexCount == 0 || commonHeader.IndexOffset == commonHeader.VertexOffset)
                return new RmvMesh() { VertexList = [], IndexList = indexList };

            // An indices-first section's size stops at its vertex block, so the vertices run past the
            // section; such a mesh is always the last in the file, so they run to its end.
            var vertexStart = (int)(commonHeader.VertexOffset + modelStartOffset);
            var vertexBlockSize = indicesFirst ? dataArray.Length - vertexStart : (int)(commonHeader.IndexOffset - commonHeader.VertexOffset);
            if (vertexBlockSize % commonHeader.VertexCount != 0)
                throw new Exception($"Vertex block of {vertexBlockSize} bytes does not divide into {commonHeader.VertexCount} vertices for {commonHeader.ModelTypeFlag}");
            var stride = (uint)(vertexBlockSize / commonHeader.VertexCount);

            // A layout this code does not decode is kept as it was read, so the file still loads and saves
            var layout = vertexFactory.ResolveLayout(binaryVertexFormat, stride, rmvVersionEnum);
            if (layout == null)
            {
                _logger.Here().Warning($"Vertex layout not decoded for {commonHeader.ModelTypeFlag} - {binaryVertexFormat}, stride {stride}. Keeping the vertex block as raw bytes");
                return new RmvMesh()
                {
                    VertexList = [],
                    RawVertexData = dataArray.AsSpan(vertexStart, vertexBlockSize).ToArray(),
                    IndexList = indexList,
                    IndicesFirst = indicesFirst,
                };
            }

            var vertexList = layout.ReadArray(rmvVersionEnum, dataArray, vertexStart, (int)stride, (int)commonHeader.VertexCount);
            if (layout.ForceComputeNormals)
                VertexFactory.ComputeFaceNormals(vertexList, indexList);

            return new RmvMesh()
            {
                VertexList = vertexList,
                IndexList = indexList,
                VertexLayout = vertexFactory.IsKnownVertex(binaryVertexFormat) && vertexFactory.GetVertexSize(binaryVertexFormat, rmvVersionEnum) == stride ? null : layout,
                IndicesFirst = indicesFirst,
            };
        }

        static ushort[] ReadIndices(byte[] dataArray, RmvCommonHeader commonHeader, int modelStartOffset)
        {
            var faceStart = commonHeader.IndexOffset + modelStartOffset;
            var span = dataArray.AsSpan((int)faceStart, (int)(sizeof(ushort) * commonHeader.IndexCount));
            return MemoryMarshal.Cast<byte, ushort>(span).ToArray();
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

        private void ValidateLodHeader(RmvFile file, int lodIndex)
        {
            var expectedMeshCount = file.ModelList[lodIndex].Length;
            if (expectedMeshCount != file.LodHeaders[lodIndex].MeshCount)
                throw new Exception("Unexpected MeshCount");

            // The two byte totals are not always right in CA's own files: bow waves ship them zeroed, and the
            // physics models (cloth sails, ropes) disagree with their geometry. The game walks the file by the
            // mesh sections, so a vanilla file's totals are written back as they were read. RecalculateOffsets
            // sets them correctly for anything built here.
            var expectedIndexSize = file.ModelList[lodIndex].Sum(x => x.Mesh.IndexList.Length) * sizeof(ushort);
            var expectedVertexSize = file.ModelList[lodIndex].Sum(x => x.Mesh.GetVertexBlockSize(x.Material.BinaryVertexFormat, file.Header.Version));
            if (expectedIndexSize != file.LodHeaders[lodIndex].TotalLodIndexSize || expectedVertexSize != file.LodHeaders[lodIndex].TotalLodVertexSize)
                _logger.Here().Warning($"Lod {lodIndex} declares {file.LodHeaders[lodIndex].TotalLodVertexSize} vertex and {file.LodHeaders[lodIndex].TotalLodIndexSize} index bytes, its meshes hold {expectedVertexSize} and {expectedIndexSize}. Keeping the declared values");
        }

        static void WriteVertices(BinaryWriter writer, RmvModel model, RmvVersionEnum rmvVersion)
        {
            if (writer.BaseStream.Position != model.CommonHeader.VertexOffset)
                throw new Exception("Unexpected VertexOffset");

            if (model.Mesh.RawVertexData != null)
                writer.Write(model.Mesh.RawVertexData);

            var vertexFactory = VertexFactory.Create();
            foreach (var vertex in model.Mesh.VertexList)
                writer.Write(model.Mesh.VertexLayout?.Write(rmvVersion, vertex) ?? vertexFactory.Save(rmvVersion, model.Material.BinaryVertexFormat, vertex));
        }

        static void WriteIndices(BinaryWriter writer, RmvModel model)
        {
            if (writer.BaseStream.Position != model.CommonHeader.IndexOffset)
                throw new Exception("Unexpected IndexOffset");

            foreach (var index in model.Mesh.IndexList)
                writer.Write(index);
        }

        byte[] SaveModel(RmvVersionEnum rmvVersion, RmvModel model)
        {
            var vertexFactory = VertexFactory.Create();
            using var modelStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(modelStream);

            if (model.CommonHeader.IndexCount != model.Mesh.IndexList.Length)
                throw new Exception("Unexpected IndexCount");

            // A mesh with no vertex block keeps the count its header declared, see LoadMesh
            if (model.Mesh.VertexList.Length != 0 && model.CommonHeader.VertexCount != model.Mesh.VertexList.Length)
                throw new Exception("Unexpected VertexCount");

            model.UpdateModelTypeFlag(model.Material.MaterialId);
            binaryWriter.Write(ByteHelper.GetBytes(model.CommonHeader));
            binaryWriter.Write(MaterialFactory.Create().Save(model.CommonHeader.ModelTypeFlag, model.Material));

            if (model.Mesh.IndicesFirst)
            {
                WriteIndices(binaryWriter, model);
                WriteVertices(binaryWriter, model, rmvVersion);
            }
            else
            {
                WriteVertices(binaryWriter, model, rmvVersion);
                WriteIndices(binaryWriter, model);
                binaryWriter.Write(model.Mesh.SectionTail);
            }

            // An indices-first section's declared size does not cover its vertices (the birch tree's stops 24 bytes into a
            // 96-byte block), so it is kept as read. Every other section is exactly what was written.
            var modelBytes = modelStream.ToArray();
            if (model.Mesh.IndicesFirst == false && model.CommonHeader.MeshSectionSize != modelBytes.Length)
                throw new Exception("Unexpected MeshSectionSize");

            return modelBytes;
        }
    }
}

