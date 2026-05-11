using Serilog;
using Shared.ByteParsing;
using Shared.Core.ErrorHandling;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public abstract class RiffChunk
    {
        readonly ILogger _logger = Logging.Create<RiffChunk>();

        public const byte PaddingByteValue = 0;

        public string Tag { get; set; } = string.Empty;
        public uint ByteIndexInFile { get; set; }
        public bool HasError { get; set; } = true;

        public abstract void ReadData(ByteChunk chunk);

        public abstract byte[] WriteData();

        public void ReadChunk(ByteChunk chunk)
        {
            try
            {
                ReadData(chunk);
                HasError = false;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to parse chunk '{Tag}' at byte index {ByteIndexInFile} - " + e.Message);
                throw;
            }
        }

        public static void WritePadding(Stream stream) => stream.WriteByte(PaddingByteValue);
    }
}
