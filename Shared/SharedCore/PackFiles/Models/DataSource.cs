using Shared.Core.ByteParsing;

namespace Shared.Core.PackFiles.Models
{
    public enum PackFileDataSourceType
    {
        FileSystem,
        PackFile,
        Memory
    }

    public interface IDataSource
    {
        long Size { get; }
        byte[] ReadData();
        byte[] ReadData(int size);
        ByteChunk ReadDataAsChunk();
    }

    public class FileSystemSource : IDataSource
    {
        public long Size { get; private set; }

        protected string filepath;
        public FileSystemSource(string filepath)
            : base()
        {
            Size = new FileInfo(filepath).Length;
            this.filepath = filepath;
        }

        public byte[] ReadData()
        {
            return File.ReadAllBytes(filepath);
        }

        public byte[] ReadData(int size)
        {
            using (var reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                var output = new byte[size];
                reader.Read(output, 0, size);
                return output;
            }
        }

        public ByteChunk ReadDataAsChunk()
        {
            return new ByteChunk(ReadData());
        }
    }

    public class MemorySource : IDataSource
    {
        public long Size { get; private set; }

        private byte[] data;
        public MemorySource(byte[] data)
        {
            Size = data.Length;
            this.data = data;
        }
        public byte[] ReadData()
        {
            return data;
        }

        public byte[] ReadData(int size)
        {
            var output = new byte[size];
            Array.Copy(data, 0, output, 0, size);
            return output;

        }

        public static MemorySource FromFile(string path)
        {
            return new MemorySource(File.ReadAllBytes(path));
        }
        public ByteChunk ReadDataAsChunk()
        {
            return new ByteChunk(ReadData());
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
        public PackedFileSourceParent Parent { get => _parent; }

        private readonly PackedFileSourceParent _parent;

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
            _parent = parent;
            Size = length;
            IsEncrypted = isEncrypted;
            IsCompressed = isCompressed;
            CompressionFormat = compressionFormat;
            UncompressedSize = uncompressedSize;
        }

        public byte[] ReadData()
        {
            var data = new byte[Size];
            using (Stream stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
            }

            if (IsEncrypted)
                data = PackFileEncryption.Decrypt(data);
            if (IsCompressed)
                data = PackFileCompression.Decompress(data);
            return data;
        }

        public byte[] ReadData(int size)
        {
            var data = new byte[size];
            using (Stream stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
            }

            if (IsEncrypted)
                data = PackFileEncryption.Decrypt(data);
            if (IsCompressed)
                data = PackFileCompression.Decompress(data);
            return data;
        }

        public byte[] ReadData(Stream knownStream)
        {
            var data = new byte[Size];
            knownStream.Seek(Offset, SeekOrigin.Begin);
            knownStream.Read(data, 0, (int)Size);

            if (IsEncrypted)
                data = PackFileEncryption.Decrypt(data);
            if (IsCompressed)
                data = PackFileCompression.Decompress(data);
            return data;
        }

        public byte[] ReadDataWithoutDecompressing()
        {
            var data = new byte[Size];
            using (Stream stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
            }

            if (IsEncrypted)
                data = PackFileEncryption.Decrypt(data);
            return data;
        }

        public ByteChunk ReadDataAsChunk()
        {
            return new ByteChunk(ReadData());
        }
    }

    public class PackedFileSourceParent
    {
        public string FilePath { get; set; }
    }
}
