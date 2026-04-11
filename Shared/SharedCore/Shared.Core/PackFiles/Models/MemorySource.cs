using Shared.ByteParsing;
using Shared.Core.PackFiles.Utility;

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

        public ByteChunk ReadDataAsChunk() => new ByteChunk(ReadData());

        public CompressionFormat CompressionFormat { get => CompressionFormat.None; }

        public static MemorySource FromFile(string path) => new MemorySource(File.ReadAllBytes(path));
    }


}
