﻿using CommonControls.Common;
using Filetypes.ByteParsing;
using Serilog;
using System;
using System.IO;

namespace Audio.FileFormats.WWise
{
    public abstract class HircItem
    {
        ILogger _logger = Logging.Create<HircItem>();

        public static readonly uint HircHeaderSize = 4; // 2x uint. Type is not included for some reason
        public string OwnerFile { get; set; }
        public uint IndexInFile { get; set; }
        public bool HasError { get; set; } = true;

        public HircType Type { get; set; }
        public uint Size { get; set; }
        public uint Id { get; set; }

        public void Parse(ByteChunk chunk)
        {
            try
            {
                var objectStartIndex = chunk.Index;

                IndexInFile = (uint)objectStartIndex;
                Type = (HircType)chunk.ReadByte();
                Size = chunk.ReadUInt32();
                Id = chunk.ReadUInt32();

                CreateSpesificData(chunk);
                var currentIndex = chunk.Index;
                var computedIndex = (int)(objectStartIndex + 5 + Size);

                chunk.Index = computedIndex;
                HasError = false;
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to parse object - " + e.Message);
                throw;
            }
        }

        protected MemoryStream WriteHeader()
        {
            var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)Type, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(Size, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(Id, out _));

            return memStream;
        }

        protected abstract void CreateSpesificData(ByteChunk chunk);
        public abstract void UpdateSize();
        public abstract byte[] GetAsByteArray();
    }

}