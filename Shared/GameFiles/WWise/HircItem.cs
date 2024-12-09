using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;

namespace Shared.GameFormats.WWise
{
    public abstract class HircItem
    {
        readonly ILogger _logger = Logging.Create<HircItem>();

        public static readonly uint HircHeaderSize = 4; // 2x uint. Type is not included for some reason
        public string OwnerFile { get; set; } = "OwnerFile Not Set";
        public uint ByteIndexInFile { get; set; }
        public bool HasError { get; set; } = true;

        public HircType Type { get; set; }
        public uint Size { get; set; }
        public uint Id { get; set; }
        public uint IndexInFile { get; set; }

        public void Parse(ByteChunk chunk)
        {
            try
            {
                var objectStartIndex = chunk.Index;

                ByteIndexInFile = (uint)objectStartIndex;
                Type = (HircType)chunk.ReadByte();
                Size = chunk.ReadUInt32();
                Id = chunk.ReadUInt32();
                CreateSpecificData(chunk);
                var currentIndex = chunk.Index;
                var computedIndex = (int)(objectStartIndex + 5 + Size);

                chunk.Index = computedIndex;
                HasError = false;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to parse object {Id} in {OwnerFile} at index {IndexInFile}- " + e.Message);
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

        protected abstract void CreateSpecificData(ByteChunk chunk);
        public abstract void UpdateSize();
        public abstract byte[] GetAsByteArray();
    }
}
