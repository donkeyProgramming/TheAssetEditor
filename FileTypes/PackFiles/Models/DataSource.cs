using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileTypes.PackFiles.Models
{


    public enum PackFileDataSourceType
    { 
        FileSystem,
        PackFile,
        Memory
    }
    public interface IDataSource
    {
        long Size{get;}
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
            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                byte[] output = new byte[size];
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
            byte[] output = new byte[size];
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

    public class PackedFileSource : IDataSource
    {
        public long Size { get; private set; }
       
        public long Offset
        {
            get;
            private set;
        }

        PackedFileSourceParent _parent;

        public PackedFileSourceParent Parent { get => _parent; }
        public PackedFileSource(PackedFileSourceParent parent, long offset, long length)
        {
            Offset = offset;
            _parent = parent;
            Size = length;
        }
        public byte[] ReadData()
        {
            byte[] data = new byte[Size];
            using (Stream stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
            }
            return data;
        }

        public byte[] ReadData(int size)
        {
            byte[] data = new byte[size];
            using (Stream stream = File.Open(_parent.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, size);
            }
            return data;
        }

        public byte[] ReadDataForFastSearch(Stream knownStream)
        {
            byte[] data = new byte[Size];
            knownStream.Seek(Offset, SeekOrigin.Begin);
            knownStream.Read(data, 0, (int)Size);
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
