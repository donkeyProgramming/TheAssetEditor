using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
using SevenZip;
using SevenZip.Compression.LZMA;
using Shared.Core.Settings;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace Shared.Core.PackFiles
{
    public enum CompressionFormat
    {
        // Dummy variant to disable compression.
        None,

        // Legacy format. Supported by all PFH5 games (all Post-WH2 games).

        // Specifically, Total War games use the Non-Streamed LZMA1 format with the following custom header:
        // | Bytes | Type  | Data                                                                                |
        // | ----- | ----- | ----------------------------------------------------------------------------------- |
        // |  4    | [u32] | Uncompressed size (as u32, max at 4GB).                                             |
        // |  1    | [u8]  | LZMA model properties (lc, lp, pb) in encoded form... I think. Usually it's `0x5D`. |
        // |  4    | [u32] | Dictionary size (as u32)... I think. It's usually `[0x00, 0x00, 0x40, 0x00]`.       |

        // For reference, a normal Non-Streamed LZMA1 header (from the original spec) contains:
        // | Bytes | Type          | Data                                                        |
        // | ----- | ------------- | ----------------------------------------------------------- |
        // |  1    | [u8]          | LZMA model properties (lc, lp, pb) in encoded form.         |
        // |  4    | [u32]         | Dictionary size (32-bit unsigned integer, little-endian).   |
        // |  8    | [prim@u64]    | Uncompressed size (64-bit unsigned integer, little-endian). |

        // This means one has to move the uncompressed size to the correct place in order for a compressed file to be readable,
        // and one has to remove the uncompressed size and prepend it to the file in order for the game to read the compressed file.
        Lzma1,

        // New format introduced in WH3 6.2.
        // This is a standard Lz4 implementation, with the following tweaks:
        // | Bytes | Type      | Data                                          |
        // | ----- | --------- | --------------------------------------------- |
        // |  4    | [u32]     | Uncompressed size (as u32, max at 4GB).       |
        // |  *    | &[[`u8`]] | Lz4 data, starting with the Lz4 Magic Number. |
        Lz4,

        // New format introduced in WH3 6.2.

        // This is a standard Zstd implementation, with the following tweaks:
        // | Bytes | Type      | Data                                            |
        // | ----- | --------- | ----------------------------------------------- |
        // |  4    | [u32]     | Uncompressed size (as u32, max at 4GB).         |
        // |  *    | &[[`u8`]] | Zstd data, starting with the Zstd Magic Number. |

        // By default the Zstd compression is done with the checksum and content size flags enabled.
        Zstd
    }

    public static class PackFileCompression
    {
        private const byte LzmaPropertiesIdentifier = 0x5D;
        private const uint Lz4MagicNumber = 0x184D_2204;
        private const uint ZstdMagicNumber = 0xfd2f_b528;

        // CA generally compress file types in specific formats, presumably because they compress better in that format.
        // Sometimes CA compress file types in various formats (though predominantly in one format), presumably by
        // mistake as they use BOB which compresses by folder not file type. We try to replicate that by assigning
        // some file types a specific compression format according to whether they are exclusively compressed
        // in a given  format or predominantly compressed in a given format by CA.
        // Lmza1 is not specified as it's legacy so we only use that for games that support only that format.
        // Zstd is not specified as by default everything not None or Lz4 is Zstd.
        public static List<string> NoneFileTypes { get; } =
        [
            // In CA packs these files are exclusively in this format
            ".bnk",
            ".ca_vp8",
            ".fxc",
            ".hlsl_compiled",
            ".log",
            ".manifest",
            ".wem",
            
            // In CA packs these files are mostly in this format
            ".rigid_model_v2",
            // Action Events don't play if the .dat file their names are stored in is compressed
            ".dat",

            // How RPFM formats these files
            ".rpfm_reserved",

             // .wav files aren't? in CA packs but probably better not to compress them
            ".wav",
        ];

        public static List<string> Lz4FileTypes { get; } =
        [
            // In CA packs these files are exclusively in this format
            ".animpack",
            ".collision",
            ".cs2",
            ".exr",
            ".mvscene",
            ".variantmeshdefinition",
            ".wsmodel",
            ".xt",

            // In CA packs these files are mostly in this format
            ".parsed",
        ];

        public static byte[] Decompress(byte[] data, int outputSize, CompressionFormat compressionFormat)
        {
            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream);

            var uncompressedSize = reader.ReadUInt32();
            if (outputSize > uncompressedSize)
                throw new InvalidDataException($"Output size {outputSize:N0} cannot be greater than the uncompressed size {uncompressedSize:N0}.");

            if (compressionFormat == CompressionFormat.Zstd)
                return DecompressZstd(reader.BaseStream, outputSize);
            if (compressionFormat == CompressionFormat.Lz4)
                return DecompressLz4(reader.BaseStream, outputSize);
            else if (compressionFormat == CompressionFormat.Lzma1)
                return DecompressLzma(reader.BaseStream, outputSize);
            else
                throw new InvalidDataException("Uh oh, the data is either not compressed or has some unknown compression format.");
        }

        private static byte[] DecompressZstd(Stream compressedDataStream, int outputSize)
        {
            var output = new byte[outputSize];
            using var decompressionStream = new DecompressionStream(compressedDataStream);
            ReadExactly(decompressionStream, output, 0, outputSize);
            return output;
        }

        private static byte[] DecompressLz4(Stream compressedDataStream, int outputSize)
        {
            var output = new byte[outputSize];
            using var decompressionStream = new LZ4DecoderStream(compressedDataStream, i => new LZ4ChainDecoder(i.BlockSize, 0));
            ReadExactly(decompressionStream, output, 0, outputSize);
            return output;
        }

        private static byte[] DecompressLzma(Stream stream, int outputSize)
        {
            // Read the property bytes
            var lzmaPropertiesSize = 5;
            var lzmaProperties = new byte[lzmaPropertiesSize];
            ReadExactly(stream, lzmaProperties, 0, lzmaPropertiesSize);

            var remainingInputSize = stream.Length - stream.Position;

            var output = new byte[outputSize];
            using var outputStream = new MemoryStream(output, 0, outputSize, writable: true, publiclyVisible: true);

            var decoder = new Decoder();
            decoder.SetDecoderProperties(lzmaProperties);
            decoder.Code(stream, outputStream, remainingInputSize, outputSize, null);

            if (outputStream.Position != outputSize)
                throw new InvalidDataException($"Expected uncompressed bytes {outputSize:N0} but only received {outputStream.Position:N0} decompressed bytes.");

            return output;
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                var bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                    throw new InvalidDataException($"Requested {count:N0} bytes but only received {totalBytesRead:N0} bytes.");

                totalBytesRead += bytesRead;
            }
        }

        public static byte[] Compress(byte[] data, CompressionFormat compressionFormat)
        {
            if(compressionFormat == CompressionFormat.Zstd)
                return CompressZstd(data);
            else if(compressionFormat == CompressionFormat.Lz4)
                return CompressLz4(data);
            else if (compressionFormat == CompressionFormat.Lzma1)
                return CompressLzma1(data);
            else
                throw new InvalidDataException("Uh oh, the data either cannot be compressed or has some unknown compression format.");
        }

        private static byte[] CompressZstd(byte[] data)
        {
            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes((uint)data.Length));

            using (var compressor = new CompressionStream(stream, 3, leaveOpen: true))
            {
                compressor.SetParameter(ZSTD_cParameter.ZSTD_c_contentSizeFlag, 1);
                compressor.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
                compressor.SetPledgedSrcSize((ulong)data.Length);
                compressor.Write(data, 0, data.Length);
            }

            return stream.ToArray();
        }


        private static byte[] CompressLz4(byte[] data)
        {
            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes((uint)data.Length));

            using (var encoder = LZ4Stream.Encode(stream, leaveOpen: true))
                encoder.Write(data, 0, data.Length);

            return stream.ToArray();
        }

        private static byte[] CompressLzma1(byte[] data)
        {
            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(data.Length), 0, 4);

            var encoder = new Encoder();
            encoder.SetCoderProperties(
                [
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits
                ],
                [
                    0x0040_0000,
                    2,
                    3,
                    0
                ]);

            // Read the property bytes
            encoder.WriteCoderProperties(stream);

            // Write the payload
            using var input = new MemoryStream(data, writable: false);
            encoder.Code(input, stream, input.Length, -1, null);

            return stream.ToArray();
        }

        public static CompressionFormat GetCompressionFormat(byte[] compressionFormatBytes)
        {
            // Lzma1 is identified by the properties
            if (compressionFormatBytes[0] == LzmaPropertiesIdentifier)
                return CompressionFormat.Lzma1;

            // Zstd and Lz4 are identified by their magic numbers
            var magicNumber = BitConverter.ToUInt32(compressionFormatBytes);
            if (magicNumber == ZstdMagicNumber)
                return CompressionFormat.Zstd;
            else if (magicNumber == Lz4MagicNumber)
                return CompressionFormat.Lz4;
            else
                return CompressionFormat.None;
        }

        public static CompressionFormat GetCompressionFormat(GameInformation gameInformation, string firstFilePathPart, string extension)
        {
            var compressionFormats = gameInformation.CompressionFormats;

            // Check if the game supports any compression at all
            if (compressionFormats.All(compressionFormat => compressionFormat == CompressionFormat.None))
                return CompressionFormat.None;

            // We use the root folder for db tables because they don't have an extension
            var isTable = firstFilePathPart == "db" || extension == ".loc";
            var hasExtension = !string.IsNullOrEmpty(extension);

            // Don't compress files that aren't tables and don't have extensions
            if (!isTable && !hasExtension)
                return CompressionFormat.None;

            // Only compress tables in WH3 (and newer games?) as compressed tables are bugged in older games
            if (isTable && compressionFormats.Contains(CompressionFormat.Zstd) && gameInformation.Type == GameTypeEnum.Warhammer3)
                return CompressionFormat.Zstd;
            else if (isTable)
                return CompressionFormat.None;

            // Anything that isn't preferrably None, Lzma1, or Lz4 is set to Zstd unless the game doesn't support that in which case use None
            // Lzma1 is a legacy format so only use it if it's all the game can use even though games with other formats can use it
            if (NoneFileTypes.Contains(extension))
                return CompressionFormat.None;
            else if (compressionFormats.Count == 1 && compressionFormats.Contains(CompressionFormat.Lzma1))
                return CompressionFormat.Lzma1;
            else if (Lz4FileTypes.Contains(extension) && compressionFormats.Contains(CompressionFormat.Lz4))
                return CompressionFormat.Lz4;
            else if (compressionFormats.Contains(CompressionFormat.Zstd))
                return CompressionFormat.Zstd;
            else
                return CompressionFormat.None;
        }
    }
}
