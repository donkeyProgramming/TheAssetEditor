using Common;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.FastBin
{
    public class FasBinFile
    { }


    public class FastBinParser
    {
        ILogger _logger = Logging.Create<FastBinParser>();

        public FasBinFile ParseFile(PackFile pf)
        {
            _logger.Here().Information($"Parsing {pf.Name}____________");
            var outputFile = new FasBinFile();

            var chunk = pf.DataSource.ReadDataAsChunk();
            var formatStr = chunk.ReadFixedLength(8);

            if (formatStr != "FASTBIN0")
                throw new NotImplementedException("Unsported fileformat for this parser");

            var rootVersion = chunk.ReadUShort();
            if(rootVersion != 23)
                throw new NotImplementedException("Unsported version for this parser");

            PARSE_BATTLEFIELD_BUILDING_LIST(outputFile, chunk);
            PARSE_BATTLEFIELD_BUILDING_LIST_FAR(outputFile, chunk);
            PARSE_CAPTURE_LOCATION_SET(outputFile, chunk);
            PARSE_EF_LINE_LIST(outputFile, chunk);
            PARSE_GO_OUTLINES(outputFile, chunk);   //5
            PARSE_NON_TERRAIN_OUTLINES(outputFile, chunk);
            PARSE_ZONES_TEMPLATE_LIST(outputFile, chunk);
            PARSE_PREFAB_INSTANCE_LIST(outputFile, chunk);
            PARSE_BMD_OUTLINE_LIST(outputFile, chunk);
            PARSE_TERRAIN_OUTLINES(outputFile, chunk);
            PARSE_LITE_BUILDING_OUTLINES(outputFile, chunk);
            PARSE_CAMERA_ZONES(outputFile, chunk);
            PARSE_CIVILIAN_DEPLOYMENT_LIST(outputFile, chunk);
            PARSE_CIVILIAN_SHELTER_LIST(outputFile, chunk);
            PARSE_PROP_LIST(outputFile, chunk); 

            return null;
        }

        void PARSE_BATTLEFIELD_BUILDING_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_BATTLEFIELD_BUILDING_LIST", chunk, 1, 0);
        }

        void PARSE_BATTLEFIELD_BUILDING_LIST_FAR(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_BATTLEFIELD_BUILDING_LIST_FAR", chunk, 1, 0);
        }


        void PARSE_CAPTURE_LOCATION_SET(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_CAPTURE_LOCATION_SET", chunk, 2, 0);
        }


        void PARSE_EF_LINE_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_EF_LINE_LIST", chunk, 0, 0);
        }


        void PARSE_GO_OUTLINES(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_GO_OUTLINES", chunk, 0, 0);
        }


        void PARSE_NON_TERRAIN_OUTLINES(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_NON_TERRAIN_OUTLINES", chunk, 1, 0);
        }

        void PARSE_ZONES_TEMPLATE_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_ZONES_TEMPLATE_LIST", chunk, 1, 0);
        }

        void PARSE_PREFAB_INSTANCE_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_PREFAB_INSTANCE_LIST", chunk, 1, 0);
        }

        void PARSE_BMD_OUTLINE_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_BMD_OUTLINE_LIST", chunk, 0, 0);
        }

        void PARSE_TERRAIN_OUTLINES(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_TERRAIN_OUTLINES", chunk, 0,1);
        }


        void PARSE_LITE_BUILDING_OUTLINES(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_LITE_BUILDING_OUTLINES", chunk, 0, 0);
        }

        void PARSE_CAMERA_ZONES(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_CAMERA_ZONES", chunk, 0, 0);
        }

        void PARSE_CIVILIAN_DEPLOYMENT_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_CAMERPARSE_CIVILIAN_DEPLOYMENT_LISTA_ZONES", chunk, 0, 0);
        }

        void PARSE_CIVILIAN_SHELTER_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            AssertVersionAndCount("PARSE_CIVILIAN_SHELTER_LIST",chunk, 0, 0);
        }


        void PARSE_PROP_LIST(FasBinFile outputFile, ByteChunk chunk)
        {
            GetVerionAndCount("PARSE_PROP_LIST", chunk, out var version, out var count);

            if (version == 2)
            {
                var keyIndex = chunk.ReadUShort(); // Could this be the key index?

                if (keyIndex != 0)
                    throw new NotImplementedException("Check what this is");

                var keyStr = chunk.ReadString();
                var numChildren = chunk.ReadUInt32();

                for (int popChildIndex = 0; popChildIndex < numChildren; popChildIndex++)
                {
                    var propVersion = chunk.ReadUInt32();
                    if (propVersion != 15)
                        throw new NotImplementedException("Check what this is");
                }
                //170 start of item
                //254 start of bhm_parent
                //84 bytes
                //

                // 273 
            }
            else
            {
                throw new ArgumentException("Unsuported version");
            }
        }


        void GetVerionAndCount(string desc, ByteChunk chunk, out ushort serialiseVersion, out int itemCount)
        {
            var indexAtStart = chunk.Index;

            itemCount = -1;
            serialiseVersion = chunk.ReadUShort();
            if(serialiseVersion != 0)
                itemCount= chunk.ReadUShort();
            var unknownData = chunk.ReadUShort();   // Always 0?

            _logger.Here().Information($"At index {indexAtStart} - Version:{serialiseVersion} NumElements:{itemCount} unk:{unknownData} - {desc}");

        }

        void AssertVersionAndCount(string desc, ByteChunk chunk, ushort expectedSerialiseVersion, uint expectedItemCount)
        {
            GetVerionAndCount(desc, chunk, out var acutalSerialiseVersion, out var actualItemCount);
          
            //if (acutalSerialiseVersion != expectedSerialiseVersion)
            //    throw new ArgumentException("Unexpected version");
            //
            //if (actualItemCount != expectedItemCount)
            //    throw new ArgumentException("Unexpected item count");
        }

    }
}
