using Shared.ByteParsing;
using Shared.Core.PackFiles.ErrorHandling;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.PackFiles.Models.FileSources
{
    public class PackedFileSourceParent
    {
        public required string FilePath { get; set; }

        // The pack as AE last loaded or saved it. Null for packs that are never checked (CA packs).
        public PackFileStamp? ExpectedStamp { get; set; }

        public void EnsureUnchanged()
        {
            if (ExpectedStamp != null && ExpectedStamp != PackFileStamp.Read(FilePath))
                throw new PackFileChangedOnDiskException(FilePath);
        }
    }

    public record PackFileStamp(long Length, DateTime LastWriteTimeUtc)
    {
        public static PackFileStamp? Read(string path)
        {
            var info = new FileInfo(path);
            return info.Exists ? new PackFileStamp(info.Length, info.LastWriteTimeUtc) : null;
        }
    }

    public record PackedFileSource : IDataSource
    {
        public long Offset { get; private set; }
        public long Size { get; private set; }
        public bool IsEncrypted { get; private set; }
        public bool IsCompressed { get; set; }
        public CompressionFormat CompressionFormat { get; set; }
        public uint UncompressedSize { get; set; }
        public PackedFileSourceParent Parent { get; set; }

        public PackedFileSource(
            PackedFileSourceParent parent,
            long offset,
            long length,
            bool isEncrypted,
            bool isCompressed,
            CompressionFormat compressionFormat,
            uint uncompressedSize)
        {
            Offset = offset;
            Parent = parent;
            Size = length;
            IsEncrypted = isEncrypted;
            IsCompressed = isCompressed;
            CompressionFormat = compressionFormat;
            UncompressedSize = uncompressedSize;
        }

        public byte[] ReadData()
        {
            Parent.EnsureUnchanged();
            using var stream = File.Open(Parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return ReadData(stream);
        }

        public byte[] ReadData(Stream knownStream)
        {
            var data = new byte[Size];
            knownStream.Seek(Offset, SeekOrigin.Begin);
            knownStream.ReadExactly(data, 0, (int)Size);

            if (IsEncrypted)
                data = FileEncryption.Decrypt(data);

            if (IsCompressed)
            {
                data = FileCompression.Decompress(data, (int)UncompressedSize, CompressionFormat);
                if (data.Length != UncompressedSize)
                    throw new InvalidDataException($"Decompressed bytes {data.Length:N0} does not match the expected uncompressed bytes {UncompressedSize:N0}.");
            }

            return data;
        }

        public byte[] PeekData(int size)
        {
            byte[] data;

            Parent.EnsureUnchanged();
            using (var stream = File.Open(Parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);

                if (!IsEncrypted && !IsCompressed)
                {
                    data = new byte[size];
                    stream.ReadExactly(data);
                }
                else
                {
                    data = new byte[Size];
                    stream.ReadExactly(data);

                    if (IsEncrypted)
                        data = FileEncryption.Decrypt(data);

                    if (IsCompressed)
                    {
                        data = FileCompression.Decompress(data, size, CompressionFormat);
                        if (data.Length != size)
                            throw new InvalidDataException($"Decompressed bytes {data.Length:N0} does not match the expected uncompressed bytes {size:N0}.");
                    }
                }
            }           

            return data;
        }

        public byte[] ReadDataWithoutDecompressing()
        {
            var data = new byte[Size];

            Parent.EnsureUnchanged();
            using (var stream = File.Open(Parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.ReadExactly(data);
            }

            if (IsEncrypted)
                data = FileEncryption.Decrypt(data);

            return data;
        }

        public ByteChunk ReadDataAsChunk() => new ByteChunk(ReadData());
    }
}
