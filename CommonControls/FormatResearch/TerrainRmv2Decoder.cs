// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonControls.FileTypes;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.Services;
using Filetypes.ByteParsing;
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
                var buffer = chunk.Buffer;

                var model = ModelFactory.Create().LoadOnlyHeaders(buffer);
                if (model.LodHeaders.Length == 0)
                    return null;

                var offset = model.LodHeaders.First().FirstMeshOffset;
                var commonHeader = ByteHelper.ByteArrayToStructure<RmvCommonHeader>(buffer, (int)offset);

                if (commonHeader.ModelTypeFlag != ModelMaterialEnum.custom_terrain)
                    return null;    // Skip

                chunk.Index = (int)offset + ByteHelper.GetSize<RmvCommonHeader>();

                var texturePath = chunk.ReadFixedLength(256);

                //chunk.ReadSingle();
                //chunk.ReadSingle();
                //chunk.ReadSingle();
                //chunk.ReadSingle();

                var v0 = chunk.ReadSingle();
                var v1 = chunk.ReadSingle();
                var v2 = chunk.ReadSingle();
                var v3 = chunk.ReadSingle();


                //var g0 = chunk.ReadFloat16();
                //var g1 = chunk.ReadFloat16();
                //var g2 = chunk.ReadFloat16();
                //var g3 = chunk.ReadFloat16();

                var d0 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());
                var d1 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());
                var d2 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());

                var d3 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());
                var d4 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());
                var d5 = VertexLoadHelper.ByteToNormal(chunk.ReadByte());

                var t = chunk.PeakUnknown(36 - (4 * 4));

                var f16 = t.SelectMany(x => x.Data.Where(y => y.Type == DbTypesEnum.Float16)).ToArray();
                var f32 = t.SelectMany(x => x.Data.Where(y => y.Type == DbTypesEnum.Single)).ToArray();

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
