using Shared.ByteParsing;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.PackFiles.Models
{

    // This should only be used for unit tests - move to test project later
    public class FileSystemSource : IDataSource
    {
        public long Size { get; private set; } = 0;

        protected string _filepath;

        public FileSystemSource(string filepath)
            : base()
        {
            var size = new FileInfo(filepath).Length;
            if (size > uint.MaxValue)
                throw new InvalidOperationException($"This file's size ({size:N0}) is too large. The maximum file size {uint.MaxValue:N0}.");

            Size = (uint)size;
            this._filepath = filepath;
        }
        

        public byte[] ReadData() => File.ReadAllBytes(_filepath);

        public byte[] PeekData(int size)
        {
            using (var reader = new BinaryReader(new FileStream(_filepath, FileMode.Open)))
            {
                var output = new byte[size];
                reader.Read(output, 0, size);
                return output;
            }
        }

        public ByteChunk ReadDataAsChunk() => new ByteChunk(ReadData());

        public CompressionFormat CompressionFormat { get => CompressionFormat.None; }
    }


}
