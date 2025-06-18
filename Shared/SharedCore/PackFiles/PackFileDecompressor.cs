using System.IO;
using System.Text;
using K4os.Compression.LZ4.Streams;
using K4os.Compression.LZ4.Encoders;
using SharpDX.DXGI;
using ZstdSharp;
namespace Shared.Core.PackFiles
{
    public static class PackFileDecompressor
    {

        // LZMA Alone doesn't have a defined magic number, but it always starts with one of these, depending on the compression level.
        private readonly static uint[] MAGIC_NUMBERS_LZMA = [
            0x0100_005D,
            0x1000_005D,
            0x0800_005D,
            0x1000_005D,
            0x2000_005D,
            0x4000_005D,
            0x8000_005D,
            0x0000_005D,
            0x0400_005D,
        ];
        private const uint MAGIC_NUMBER_LZ4 = 0x184D_2204;
        private const uint MAGIC_NUMBER_ZSTD = 0xfd2f_b528;

        public static byte[] Decompress(byte[] c_data)
        {
            using (var memStream = new MemoryStream(c_data))
            using (var reader = new BinaryReader(memStream))
            {
                var u_size = reader.ReadInt32();
                var magic_numbers = reader.ReadUInt32();
                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                if (magic_numbers == MAGIC_NUMBER_ZSTD)
                {
                    var buffer = new byte[u_size];
                    var output = new MemoryStream(buffer);
                    using (var decompressionStream = new DecompressionStream(reader.BaseStream))
                    {
                        decompressionStream.CopyTo(output);
                        return output.ToArray();

                    }
                }
                else if (magic_numbers == MAGIC_NUMBER_LZ4)
                {
                    var buffer = new byte[u_size];
                    var output = new MemoryStream(buffer);
                    var decompressor = new LZ4DecoderStream(reader.BaseStream, i => new LZ4ChainDecoder(i.BlockSize, 0));
                    decompressor.CopyTo(output);

                    return output.ToArray();
                }
                else if (MAGIC_NUMBERS_LZMA.Contains(magic_numbers))
                {
                    throw new Exception("LZMA1 detected. Not supported.");
                }
                else
                {
                    return c_data;
                }


            }
        }
    }
}
