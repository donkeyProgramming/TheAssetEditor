using Filetypes;
using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using Filetypes.RigidModel.Vertex;
using FileTypes.RigidModel.LodHeader;
using FileTypes.RigidModel.MaterialHeaders;
using System;
using System.IO;
using System.Linq;

namespace FileTypes.RigidModel
{
    public class ModelFactory
    {
        public static ModelFactory Create() => new ModelFactory();

        public RmvFile Load(byte[] bytes)
        {
            var file = LoadOnlyHeaders(bytes);

            var modelList = new RmvModel[file.Header.LodCount][];
            for (int i = 0; i < file.Header.LodCount; i++)
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
            var material = MaterialFactory.Create().LoadMaterial(data, materialOffset, rmvVersionEnum, commonHeader.ModelTypeFlag);
            var materialSize = material.ComputeSize();
            
            if (materialOffset + materialSize != commonHeader.VertexOffset + modelStartOffset)
                throw new Exception("Part of material header not read");

            var mesh = LoadMesh(data, commonHeader, material.BinaryVertexFormat, modelStartOffset);

            return new RmvModel()
            { 
                CommonHeader = commonHeader,
                Material = material,
                Mesh = mesh
            };
        }

        RmvMesh LoadMesh(byte[] dataArray, RmvCommonHeader CommonHeader, VertexFormat binaryVertexFormat, int modelStartOffset)
        {
            var vertexFactory = VertexFactory.Create();

            var vertexStart = CommonHeader.VertexOffset + modelStartOffset;
            var expectedVertexSize = (CommonHeader.IndexOffset - CommonHeader.VertexOffset) / CommonHeader.VertexCount;
            var vertexSize = vertexFactory.GetVertexSize(binaryVertexFormat);
            if (expectedVertexSize != vertexSize)
                throw new Exception("Vertex size does not match");

            var VertexList = vertexFactory.CreateVertexFromBytes(binaryVertexFormat, dataArray, (int)CommonHeader.VertexCount, (int)vertexStart, (int)expectedVertexSize);

            var faceStart = CommonHeader.IndexOffset + modelStartOffset;
            var IndexList = new ushort[CommonHeader.IndexCount];
            for (int i = 0; i < CommonHeader.IndexCount; i++)
                IndexList[i] = BitConverter.ToUInt16(dataArray, (int)faceStart + sizeof(ushort) * i);

            var mesh = new RmvMesh()
            {
                VertexList = VertexList,
                IndexList = IndexList
            };

            vertexFactory.ReComputeNormals(binaryVertexFormat, ref VertexList, ref IndexList);

            return mesh;
        }

        public byte[] Save(RmvFile file)
        {
            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            if (file.LodHeaders.Length != file.Header.LodCount)
                throw new Exception("Unexpected number of Lods");

            writer.Write(ByteHelper.GetBytes(file.Header));
            for(int lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                ValidateLodHeader(file, lodIndex);
                writer.Write(LodHeaderFactory.Create().Save(file.Header.Version, file.LodHeaders[lodIndex]));
            }

            for (int lodIndex = 0; lodIndex < file.LodHeaders.Length; lodIndex++)
            {
                var models = file.ModelList[lodIndex];
                for(int modelIndex = 0; modelIndex < models.Length; modelIndex++)
                { 
                    if (modelIndex == 0)    // Is first mesh
                    {
                        var expectedStreamIndex = file.LodHeaders[lodIndex].FirstMeshOffset;
                        if (expectedStreamIndex != writer.BaseStream.Position)
                            throw new Exception("Unexpected FirstMeshOffset");
                    }

                    var modelBytes = SaveModel(models[modelIndex]);
                    if(models[modelIndex].CommonHeader.MeshSectionSize != modelBytes.Length)
                        throw new Exception("Unexpected MeshSectionSize");

                    writer.Write(modelBytes);
                }
            }

            var bytes = ms.ToArray();
            var reloadedModel = Load(bytes);
            if (reloadedModel == null)
                throw new Exception("Failed to save model - Could not load the result");

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

            var expectedVertexSize = file.ModelList[lodIndex].Sum(x => x.Mesh.VertexList.Length * VertexFactory.Create().GetVertexSize(x.Material.BinaryVertexFormat));
            if (expectedVertexSize != file.LodHeaders[lodIndex].TotalLodVertexSize)
                throw new Exception("Unexpected TotalLodVertexSize");
        }

        byte[] SaveModel(RmvModel model)
        {
            var vertexFactory = VertexFactory.Create();
            using MemoryStream modelStream = new MemoryStream();
            using var modelWriter = new BinaryWriter(modelStream);

            if (model.CommonHeader.IndexCount != model.Mesh.IndexList.Length)
                throw new Exception("Unexpected IndexCount");

            if (model.CommonHeader.VertexCount != model.Mesh.VertexList.Length)
                throw new Exception("Unexpected IndexCount");

            modelWriter.Write(ByteHelper.GetBytes(model.CommonHeader));
            modelWriter.Write(MaterialFactory.Create().Save(model.CommonHeader.ModelTypeFlag, model.Material));

            var vertStart = modelWriter.BaseStream.Position;
            if (vertStart != model.CommonHeader.VertexOffset)
                throw new Exception("Unexpected VertexOffset");

            foreach (var vertex in model.Mesh.VertexList)
                modelWriter.Write(vertexFactory.Save(model.Material.BinaryVertexFormat, vertex));

            var indexOffset = modelWriter.BaseStream.Position;
            if (indexOffset != model.CommonHeader.IndexOffset)
                throw new Exception("Unexpected IndexOffset");

            foreach (var index in model.Mesh.IndexList)
                modelWriter.Write(index);

            var modelBytes = modelStream.ToArray();
            if (model.CommonHeader.MeshSectionSize != modelBytes.Length)
                throw new Exception("Unexpected MeshSectionSize");

            return modelBytes;
        }
    }
}

