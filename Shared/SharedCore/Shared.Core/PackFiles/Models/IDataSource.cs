using Shared.ByteParsing;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.PackFiles.Models
{
    public interface IDataSource
    {
        long Size { get; }
        byte[] ReadData();
        byte[] PeekData(int size);
        ByteChunk ReadDataAsChunk();

        CompressionFormat CompressionFormat { get; }
    }


}
