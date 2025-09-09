using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Data
{
    public class DataChunk
    {
        public static ByteChunk ReadData(string fileName, ByteChunk chunk)
        {
            var tag = Encoding.UTF8.GetString(chunk.ReadBytes(4));
            var chunkSize = chunk.ReadUInt32();
            var buffer = chunk.CreateSub((int)chunkSize);
            return buffer;
        }
    }
}
