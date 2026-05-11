using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Wav
{
    public abstract class RiffChunk
    {
        public string Tag { get; set; } = string.Empty;
        public uint ByteIndexInFile { get; set; }

        public abstract void ReadData(ByteChunk chunk);

        public abstract byte[] WriteData();

        public void ReadChunk(ByteChunk chunk)
        {
            try
            {
                ReadData(chunk);
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"Failed to parse chunk '{Tag}' at byte index {ByteIndexInFile}: {e.Message}", e);
            }
        }
    }
}
