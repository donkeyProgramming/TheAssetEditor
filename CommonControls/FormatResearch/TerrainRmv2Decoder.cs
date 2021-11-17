using CommonControls.Services;
using Filetypes;
using Filetypes.ByteParsing;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.RigidModel;
using FileTypes.RigidModel.LodHeader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static CommonControls.FormatResearch.DecoderHelper;

namespace CommonControls.FormatResearch
{
    public class TerrainRmv2Decoder
    {
        class RMV2AdditionalInfo
        {
            public ModelMaterialEnum ModelTypeFlag { get; set; }
            public ushort RenderFlag { get; set; }
            public int HeaderSize { get; set; }
            public int VertexSize { get; set; }
        }

        static PackFileService _pfs;

        static List<PackFile> GetFilesTiles(PackFileService pfs) 
        {
            return pfs.FindAllFilesInDirectory(@"terrain\tiles\campaign")
                 .Where(x => Path.GetExtension(x.Name) == ".rigid_model_v2").ToList();
        }

        static List<PackFile> GetFilesMap(PackFileService pfs)
        {
            return pfs.FindAllFilesInDirectory(@"terrain\campaigns\warhammer_map_1_1_wood_elves\global_meshes")
                 .Where(x => Path.GetExtension(x.Name) == ".rigid_model_v2").ToList();
        }

        static List<IByteParser> GetDecoders()
        {
            var parserList = ByteParsers.GetAllParsers().ToList();
            parserList = parserList.Where(x => !(x is OptionalStringAsciiParser || x is OptionalStringParser)).ToList();
            parserList = parserList.Where(x => !(x is StringAsciiParser || x is StringParser)).ToList();
            return parserList;
        }

        static public DecoderHelper CreateWeightedTextureBlend(PackFileService pfs)
        {
            _pfs = pfs;
            var instance = new DecoderHelper();
            instance.Create(GetFilesTiles(_pfs), GetDecoders(), LoadHeader_Weighted_Texture_Blend, ComputeStats);
            return instance;
        }

        static public DecoderHelper CreateTerrainCustom(PackFileService pfs)
        {
            _pfs = pfs;
            var instance = new DecoderHelper();
            instance.Create(GetFilesTiles(_pfs), GetDecoders(), LoadHeader_CustomTerrain, ComputeStats);
            return instance;
        }

        static public DecoderHelper CreateDefaultType(PackFileService pfs)
        {
            _pfs = pfs;
            var instance = new DecoderHelper();
            instance.Create(GetFilesTiles(_pfs), GetDecoders(), LoadHeader_DefaultType, ComputeStats);
            return instance;
        }

        static public DecoderHelper Crate101Type(PackFileService pfs)
        {
            _pfs = pfs;
            var instance = new DecoderHelper();
            instance.Create(GetFilesMap(_pfs), GetDecoders(), LoadHeader_101Type, ComputeStats);
            return instance;
        }

        static DataItem LoadHeader_101Type(PackFile file)
        {
            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();
                var buffer = chunk.Buffer;

                var model = ModelFactory.Create().LoadOnlyHeaders(buffer);
                var offset = model.LodHeaders.First().FirstMeshOffset;
                var commonHeader = ByteHelper.ByteArrayToStructure<RmvCommonHeader>(buffer, (int)offset);

                if (commonHeader.ModelTypeFlag != ModelMaterialEnum.TerrainTiles)
                    return null;    // Skip

                chunk.Index = (int)offset + ByteHelper.GetSize<RmvCommonHeader>();

                var terrainBaseStr = chunk.ReadFixedLength(64);
                //
                var width = chunk.ReadUInt32();
                var hight = chunk.ReadUInt32();
                //
                var unk = chunk.ReadUInt32();
                var unk0 = chunk.ReadUInt32();
                var unk1 = chunk.ReadUInt32();
                var unk2 = chunk.ReadUInt32();

                uint headerSize = commonHeader.VertexOffset;
                var path = _pfs.GetFullPath(file);
                var vertSize = (commonHeader.IndexOffset - commonHeader.VertexOffset) / commonHeader.VertexCount;
                return new DataItem(chunk, path, new RMV2AdditionalInfo() { RenderFlag = commonHeader.RenderFlag, ModelTypeFlag = commonHeader.ModelTypeFlag, HeaderSize = (int)commonHeader.VertexOffset, VertexSize = (int)vertSize })
                {
                    DataMaxSize = headerSize,
                    DataRead = (uint)(chunk.Index - offset)
                };
            }
            catch
            {
                // Failed to get this far
                var path = _pfs.GetFullPath(file);
                return new DataItem(null, path);
            }
        }

        static DataItem LoadHeader_DefaultType(PackFile file)
        {
            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();

                var model = new RmvFile();
                //model.LoadHeaders(chunk.Buffer);

                var offset = model.LodHeaders.First().FirstMeshOffset;
                chunk.Index = (int)offset;

                // Read the beginning of the model header
                var modelTypeFlag = (ModelMaterialEnum)chunk.ReadUShort();
                if (modelTypeFlag != ModelMaterialEnum.default_type)
                    return null;    // Skip

                var renderFlag = chunk.ReadUShort();
                var MeshSectionSize = chunk.ReadUInt32();
                var VertexOffset = chunk.ReadUInt32();
                var VertexCount = chunk.ReadUInt32();
                var IndexOffset = chunk.ReadUInt32();
                var IndexCount = chunk.ReadUInt32();

                var boundingBox = chunk.ReadBytes(24);
                var shaderParams = chunk.ReadBytes(32);
                //var terrainBaseStr = chunk.ReadFixedLength(64);
                //
                //var width = chunk.ReadUInt32();
                //var hight = chunk.ReadUInt32();
                //
                //var unk = chunk.ReadUInt32();
                //var unk0 = chunk.ReadUInt32();
                //var unk1 = chunk.ReadUInt32();
                //var unk2 = chunk.ReadUInt32();

                //chunk.ReadFixedLength(265);
                //chunk.ReadFixedLength(265);
                //chunk.ReadFixedLength(265);

                uint headerSize = VertexOffset;
                var path = _pfs.GetFullPath(file);
                var vertSize = (IndexOffset - VertexOffset) / VertexCount;
                return new DataItem(chunk, path, new RMV2AdditionalInfo() { RenderFlag = renderFlag, ModelTypeFlag = modelTypeFlag, HeaderSize = (int)VertexOffset, VertexSize = (int)vertSize })
                {
                    DataMaxSize = headerSize,
                    DataRead = (uint)(chunk.Index - offset)
                };
            }
            catch
            {
                // Failed to get this far
                var path = _pfs.GetFullPath(file);
                return new DataItem(null, path);
            }
        }

        static DataItem LoadHeader_Weighted_Texture_Blend(PackFile file)
        {
            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();

                var model = new RmvFile();
                //model.LoadHeaders(chunk.Buffer);

                var offset = model.LodHeaders.First().FirstMeshOffset;
                chunk.Index = (int)offset;

                // Read the beginning of the model header
                var modelTypeFlag = (ModelMaterialEnum)chunk.ReadUShort();
                if (modelTypeFlag != ModelMaterialEnum.weighted_texture_blend)
                    return null;    // Skip

                var renderFlag = chunk.ReadUShort();
                var MeshSectionSize = chunk.ReadUInt32();
                var VertexOffset = chunk.ReadUInt32();
                var VertexCount = chunk.ReadUInt32();
                var IndexOffset = chunk.ReadUInt32();
                var IndexCount = chunk.ReadUInt32();

                var boundingBox = chunk.ReadBytes(24);
                var shaderParams = chunk.ReadBytes(32);
                var terrainBaseStr = chunk.ReadFixedLength(64);

                var width = chunk.ReadUInt32();
                var hight = chunk.ReadUInt32();

                var unk = chunk.ReadUInt32();
                var unk0 = chunk.ReadUInt32();
                var unk1 = chunk.ReadUInt32();
                var unk2 = chunk.ReadUInt32();

                uint headerSize = VertexOffset;
                var path = _pfs.GetFullPath(file);
                var vertSize = (IndexOffset - VertexOffset) / VertexCount;
                return new DataItem(chunk, path, new RMV2AdditionalInfo() { RenderFlag = renderFlag, ModelTypeFlag = modelTypeFlag, HeaderSize = (int)VertexOffset, VertexSize = (int)vertSize })
                {
                    DataMaxSize = headerSize,
                    DataRead = (uint)(chunk.Index - offset)
                };
            }
            catch
            {
                // Failed to get this far
                var path = _pfs.GetFullPath(file);
                return new DataItem(null, path);
            }
        }

        static DataItem LoadHeader_CustomTerrain(PackFile file)
        {
            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();

                var model = new RmvFile();
                //model.LoadHeaders(chunk.Buffer);

                var offset = model.LodHeaders.First().FirstMeshOffset;
                chunk.Index = (int)offset;

                // Read the beginning of the model header
                var modelTypeFlag = (ModelMaterialEnum)chunk.ReadUShort();
                if(modelTypeFlag != ModelMaterialEnum.custom_terrain)
                   return null;

                var renderFlag = chunk.ReadUShort();
                var MeshSectionSize = chunk.ReadUInt32();
                var VertexOffset = chunk.ReadUInt32();
                var VertexCount = chunk.ReadUInt32();
                var IndexOffset = chunk.ReadUInt32();
                var IndexCount = chunk.ReadUInt32();

                var boundingBox = chunk.ReadBytes(24);
                var shaderParams = chunk.ReadBytes(32);

                var texturePath = chunk.ReadFixedLength(256);

                uint headerSize = VertexOffset;
                var path = _pfs.GetFullPath(file);
                var vertSize = (IndexOffset - VertexOffset) / VertexCount;
                return new DataItem(chunk, path, new RMV2AdditionalInfo() { RenderFlag = renderFlag, ModelTypeFlag = modelTypeFlag, HeaderSize = (int)VertexOffset, VertexSize = (int)vertSize })
                {
                    DataMaxSize = headerSize,
                    DataRead = (uint)(chunk.Index - offset)
                };
            }
            catch
            {
                // Failed to get this far
                var path = _pfs.GetFullPath(file);
                return new DataItem(null, path);
            }
        }

        static void ComputeStats(List<DataItem> dataItems)
        {
            var failed = dataItems.Where(x => x.FailedOnLoad).Select(x => x.DisplayName).ToList();

            var shaderflags = dataItems
                .Where(x => x.FailedOnLoad == false)
                .Select(x => (x.AdditionalData as RMV2AdditionalInfo).ModelTypeFlag)
                .Distinct()
                .ToList();

            var renderflags = dataItems
                .Where(x => x.FailedOnLoad == false)
                .Select(x => (x.AdditionalData as RMV2AdditionalInfo).RenderFlag)
                .Distinct()
                .ToList();

            var headerSizes = dataItems
                .Where(x => x.FailedOnLoad == false)
                .Select(x => (x.AdditionalData as RMV2AdditionalInfo).HeaderSize)
                .Distinct()
                .ToList();

            var vertSizes = dataItems
              .Where(x => x.FailedOnLoad == false)
              .Select(x => (x.AdditionalData as RMV2AdditionalInfo).VertexSize)
              .Distinct()
              .ToList();

            var headerSizesAndFiles = dataItems
                .Where(x => x.FailedOnLoad == false)
                .GroupBy(x => (x.AdditionalData as RMV2AdditionalInfo).HeaderSize)
                .Select(x => new { HeaderSize = x.Key, Items = dataItems.Where(y => y.FailedOnLoad == false).Where(y => (y.AdditionalData as RMV2AdditionalInfo).HeaderSize == x.Key).Select(x => x.DisplayName).ToList() })
                .ToList();
        }
    }
}
