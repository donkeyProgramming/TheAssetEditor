﻿using Shared.Core.ByteParsing;
using static Shared.Core.PackFiles.PackFileDecryptor;

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

        protected string _filepath;

        public FileSystemSource(string filepath) : base()
        {
            Size = new FileInfo(filepath).Length;
            this._filepath = filepath;
        }

        public byte[] ReadData()
        {
            return File.ReadAllBytes(_filepath);
        }

        public byte[] ReadData(int size)
        {
            using var reader = new BinaryReader(new FileStream(_filepath, FileMode.Open));
            var output = new byte[size];
            reader.Read(output, 0, size);
            return output;
        }

        public ByteChunk ReadDataAsChunk()
        {
            return new ByteChunk(ReadData());
        }
    }

    public class MemorySource : IDataSource
    {
        public long Size { get; private set; }

        private readonly byte[] _data;

        public MemorySource(byte[] data)
        {
            Size = data.Length;
            this._data = data;
        }

        public byte[] ReadData()
        {
            return _data;
        }

        public byte[] ReadData(int size)
        {
            var output = new byte[size];
            Array.Copy(_data, 0, output, 0, size);
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
        public PackedFileSourceParent Parent { get => _parent; }

        private readonly PackedFileSourceParent _parent;

        public PackedFileSource(PackedFileSourceParent parent, long offset, long length, bool isEncrypted)
        {
            Offset = offset;
            _parent = parent;
            Size = length;
            IsEncrypted = isEncrypted;
        }

        public byte[] ReadData()
        {
            var data = new byte[Size];
            using var stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(Offset, SeekOrigin.Begin);
            stream.ReadExactly(data);
            if (IsEncrypted)
                data = Decrypt(data);
            return data;
        }

        public byte[] ReadData(int size)
        {
            var data = new byte[size];
            using var stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(Offset, SeekOrigin.Begin);
            stream.ReadExactly(data, 0, size);
            if (IsEncrypted)
                data = Decrypt(data);
            return data;
        }

        public byte[] ReadDataForFastSearch(Stream knownStream)
        {
            var data = new byte[Size];
            knownStream.Seek(Offset, SeekOrigin.Begin);
            knownStream.ReadExactly(data, 0, (int)Size);
            if (IsEncrypted)
                data = Decrypt(data);
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
