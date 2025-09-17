using EasyCompressor;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
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
        // LZMA alone doesn't have a defined magic number, but it always starts with one of these, depending on the compression level
        private static readonly uint[] s_magicNumbersLzma = [
            0x0100_005D,
            0x1000_005D,
            0x0800_005D,
            0x2000_005D,
            0x4000_005D,
            0x8000_005D,
            0x0000_005D,
            0x0400_005D,
        ];
        private static readonly uint s_magicNumberLz4 = 0x184D_2204;
        private static readonly uint s_magicNumberZstd = 0xfd2f_b528;

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

        public static byte[] Decompress(byte[] data)
        {
            var result = Array.Empty<byte>();
            if (data == null || data.Length == 0)
                return result;

            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream);

            // Read the header and get what we need
            var uncompressedSize = reader.ReadUInt32();
            var magicNumber = reader.ReadUInt32();
            var compressionFormat = GetCompressionFormat(magicNumber);
            stream.Seek(-4, SeekOrigin.Current);

            if (compressionFormat == CompressionFormat.Zstd)
                return DecompressZstd(reader, uncompressedSize);
            if (compressionFormat == CompressionFormat.Lz4)
                return DecompressLz4(reader, uncompressedSize);
            else if (compressionFormat == CompressionFormat.Lzma1)
                result = DecompressLzma(data, uncompressedSize);
            else if (compressionFormat == CompressionFormat.None)
                return data;  

            if (result.Length != uncompressedSize)
                throw new InvalidDataException($"Expected {uncompressedSize:N0} bytes after decompression, but got {result.Length:N0}.");

            return result;
        }

        private static byte[] DecompressZstd(BinaryReader reader, uint uncompressedSize)
        {
            var buffer = new byte[uncompressedSize];
            var output = new MemoryStream(buffer);
            using var decompressionStream = new DecompressionStream(reader.BaseStream);
            decompressionStream.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] DecompressLz4(BinaryReader reader, uint uncompressedSize)
        {
            var buffer = new byte[uncompressedSize];
            var output = new MemoryStream(buffer);
            var decompressor = new LZ4DecoderStream(reader.BaseStream, i => new LZ4ChainDecoder(i.BlockSize, 0));
            decompressor.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] DecompressLzma(byte[] data, uint uncompressedSize)
        {
            var uncompressedSizeFieldSize = sizeof(uint);
            var headerDataLength = 5;
            var injectedSizeLength = sizeof(ulong);

            // Compute all the offsets
            var headerStart = uncompressedSizeFieldSize;
            var headerEnd = headerStart + headerDataLength;
            var footerStart = headerEnd;
            var minTotalSize = footerStart;

            // LZMA1 headers have 13 bytes, but we only have 9 due to using a u32 size
            if (data.Length < minTotalSize)
                throw new InvalidDataException("File too small to be valid LZMA.");

            // Unlike other formats, in this one we need to inject the uncompressed size in the file header otherwise it won't be a valid lzma file
            using var primary = new MemoryStream(data.Length + injectedSizeLength);
            primary.Write(data, headerStart, headerDataLength);
            primary.Write(BitConverter.GetBytes((ulong)uncompressedSize), 0, injectedSizeLength);
            primary.Write(data, footerStart, data.Length - footerStart);
            primary.Position = 0;

            try
            {
                return LZMACompressor.Shared.Decompress(primary.ToArray());
            }
            catch
            {
                // Some files may still fail so fall back to a unknown size (u64::MAX) instead
                using var fallback = new MemoryStream(data.Length + injectedSizeLength);
                fallback.Write(data, headerStart, headerDataLength);
                fallback.Write(BitConverter.GetBytes(ulong.MaxValue), 0, injectedSizeLength);
                fallback.Write(data, footerStart, data.Length - footerStart);
                fallback.Position = 0;
                return LZMACompressor.Shared.Decompress(fallback.ToArray());
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
            return data;
        }

        private static byte[] CompressZstd(byte[] data)
        {
            using var stream = new MemoryStream();
            var uncompressedSize = data.Length;
            stream.Write(BitConverter.GetBytes((uint)uncompressedSize));

            using (var compressor = new CompressionStream(stream, 3, leaveOpen: true))
            {
                compressor.SetParameter(ZSTD_cParameter.ZSTD_c_contentSizeFlag, 1);
                compressor.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
                compressor.SetPledgedSrcSize((ulong)uncompressedSize);
                compressor.Write(data, 0, uncompressedSize);
            }

            return stream.ToArray();
        }

        private static byte[] CompressLz4(byte[] data)
        {
            using var stream = new MemoryStream();
            var uncompressedSize = data.Length;
            stream.Write(BitConverter.GetBytes((uint)uncompressedSize));

            using (var encoder = LZ4Stream.Encode(stream, leaveOpen: true))
                encoder.Write(data, 0, uncompressedSize);

            return stream.ToArray();
        }

        private static byte[] CompressLzma1(byte[] data)
        {
            var compressedData = LZMACompressor.Shared.Compress(data);
            var compressedSize = compressedData.Length;
            if (compressedSize < 13)
                throw new InvalidDataException("Data cannot be compressed");

            using var stream = new MemoryStream();
            var uncompressedSize = data.Length;
            stream.Write(BitConverter.GetBytes(uncompressedSize), 0, 4);
            stream.Write(compressedData, 0, 5);
            stream.Write(compressedData, 13, compressedSize - 13);

            return stream.ToArray();
        }

        public static CompressionFormat GetCompressionFormat(uint magicNumber)
        {
            if (magicNumber == s_magicNumberZstd)
                return CompressionFormat.Zstd;
            else if (magicNumber == s_magicNumberLz4)
                return CompressionFormat.Lz4;
            else if (s_magicNumbersLzma.Contains(magicNumber))
                return CompressionFormat.Lzma1;
            else
                return CompressionFormat.None;
        }

        public static CompressionFormat GetCompressionFormat(GameInformation gameInformation, string firstFilePathPart, string extension)
        {
            var compressionFormats = gameInformation.CompressionFormats;

            // Check if the game supports any compression at all
            if (compressionFormats.All(compressionFormat => compressionFormat == CompressionFormat.None))
                return CompressionFormat.None;

            // We use rootFolder for normal db tables because they don't have an extension
            var isTable = firstFilePathPart == "db" || extension == ".loc";
            var hasExtension = !string.IsNullOrEmpty(extension);

            // Don't compress files that aren't tables and don't have extensions
            if (!isTable && !hasExtension)
                return CompressionFormat.None;

            // Only compress tables in WH3 (and newer games?) as compresse tables are bugged in older games
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
