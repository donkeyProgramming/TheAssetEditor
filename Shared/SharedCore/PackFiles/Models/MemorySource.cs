using Shared.ByteParsing;

namespace Shared.Core.PackFiles.Models
{
    public class MemorySource : IDataSource
    {
        public long Size { get; private set; }

        private readonly byte[] _data;

        public MemorySource(byte[] data)
        {
            Size = data.Length;
            _data = data;
        }

        public byte[] ReadData()
        {
            return _data;
        }

        public byte[] PeekData(int size)
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


}
