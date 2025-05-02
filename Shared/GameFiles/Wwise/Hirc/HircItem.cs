using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public abstract class HircItem
    {
        readonly ILogger _logger = Logging.Create<HircItem>();

        public static readonly uint HircHeaderSize = 4; // 2x uint. Type is not included for some reason
        public string OwnerFilePath { get; set; } = "OwnerFile Not Set";
        public bool IsCaHircItem { get; set; }
        public uint LanguageId { get; set; } 
        public uint ByteIndexInFile { get; set; }
        public uint IndexInFile { get; set; }
        public bool HasError { get; set; } = true;

        // Wwise object properties
        public AkBkHircType HircType { get; set; }
        public uint SectionSize { get; set; }
        public uint Id { get; set; }

        public void Parse(ByteChunk chunk)
        {
            try
            {
                var objectStartIndex = chunk.Index;
                ByteIndexInFile = (uint)objectStartIndex;

                HircType = (AkBkHircType)chunk.ReadByte();
                SectionSize = chunk.ReadUInt32();
                Id = chunk.ReadUInt32();
                ReadData(chunk);

                var currentIndex = chunk.Index;
                var computedIndex = (int)(objectStartIndex + 5 + SectionSize);
                chunk.Index = computedIndex;
                HasError = false;
            }

            catch (Exception e)
            {
                _logger.Here().Error($"Failed to parse object {Id} of type {HircType} in {OwnerFilePath} at index {IndexInFile} - " + e.Message);
                throw;
            }
        }

        protected MemoryStream WriteHeader()
        {
            var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)HircType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(SectionSize, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(Id, out _));
            return memStream;
        }

        protected abstract void ReadData(ByteChunk chunk);
        public abstract byte[] WriteData();
        public abstract void UpdateSectionSize(); 
    }
}
