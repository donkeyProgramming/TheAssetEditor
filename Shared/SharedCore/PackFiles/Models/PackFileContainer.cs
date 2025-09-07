using System.Text;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public record PackFileWriteInfo(
        PackFile PackFile,
        long FileSizeMetadataPosition,
        CompressionFormat CurrentCompressionFormat,
        CompressionFormat IntendedCompressionFormat);

    public class PackFileContainer
    {
        public string Name { get; set; }
        public PFHeader Header { get; set; }
        public bool IsCaPackFile { get; set; } = false;
        public string SystemFilePath { get; set; }
        public long OriginalLoadByteSize { get; set; } = -1;

        public Dictionary<string, PackFile> FileList { get; set; } = [];

        public PackFileContainer(string name)
        {
            Name = name;
        }

        public void MergePackFileContainer(PackFileContainer other)
        {
            foreach (var item in other.FileList)
                FileList[item.Key] = item.Value;
            return;
        }

        public void SaveToByteArray(BinaryWriter writer, GameInformation gameInformation)
        {
            long fileNamesOffset = 0;
            var sortedFiles = FileList.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            foreach (var file in sortedFiles)
            {
                if (Header.Version == PackFileVersion.PFH5)
                    fileNamesOffset += 1;
                if (Header.HasIndexWithTimeStamp)
                    fileNamesOffset += 4;
                fileNamesOffset += 4 + file.Key.Length + 1;    // Size + filename with zero terminator
            }

            long headerSpesificBytes = 0;
            if (Header.HasIndexWithTimeStamp)
                headerSpesificBytes += 4;
            if (Header.Version == PackFileVersion.PFH5)
                headerSpesificBytes += 1;

            long fileNamesOffset2 = 0;
            foreach (var file in sortedFiles)
            {
                var fileSize = 4;
                var strLength = file.Key.Length + 1;
                fileNamesOffset2 += fileSize + headerSpesificBytes + strLength;
            }

            Header.FileCount = (uint)FileList.Count;
            PackFileSerializer.WriteHeader(Header, (uint)fileNamesOffset, writer);

            var filesToWrite = new List<PackFileWriteInfo>();

            // Write file metadata
            foreach (var file in sortedFiles)
            {
                var packFile = file.Value;
                var fileSize = (int)packFile.DataSource.Size;

                // Determine compression info
                var currentCompressionFormat = CompressionFormat.None;
                if (packFile.DataSource is PackedFileSource packedFileSource)
                    currentCompressionFormat = packedFileSource.CompressionFormat;
                var firstFilePathPart = file.Key.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).First();
                var intendedCompressionFormat = PackFileCompression.GetCompressionFormat(gameInformation, firstFilePathPart, packFile.Extension);
                var shouldCompress = intendedCompressionFormat != CompressionFormat.None;

                // File size placeholder (rewritten later)
                var fileSizePosition = writer.BaseStream.Position;
                writer.Write(0);

                // Timestamp
                if (Header.HasIndexWithTimeStamp)
                    writer.Write(0);

                // Compression
                if (Header.Version == PackFileVersion.PFH5)
                    writer.Write(shouldCompress);

                // Filename
                var fileNameBytes = Encoding.UTF8.GetBytes(file.Key);
                writer.Write(fileNameBytes);

                // Zero terminator
                writer.Write((byte)0);

                filesToWrite.Add(new PackFileWriteInfo(
                    packFile,
                    fileSizePosition,
                    currentCompressionFormat,
                    intendedCompressionFormat));
            }

            var packedFileSourceParent = new PackedFileSourceParent { FilePath = SystemFilePath };

            // Write the files
            foreach (var file in filesToWrite)
            {
                var packFile = file.PackFile;
                byte[] data;
                var fileSize = 0;
                uint uncompressedFileSize = 0;

                // Read the data
                var shouldCompress = file.IntendedCompressionFormat != CompressionFormat.None;
                var isCorrectCompressionFormat = file.CurrentCompressionFormat == file.IntendedCompressionFormat;
                if (shouldCompress && !isCorrectCompressionFormat)
                {
                    // Decompress the data 
                    var uncompressedData = packFile.DataSource.ReadData();
                    uncompressedFileSize = (uint)uncompressedData.Length;

                    // Compress the data into the right format
                    var compressedData = PackFileCompression.Compress(uncompressedData, file.IntendedCompressionFormat);
                    data = compressedData;
                    fileSize = compressedData.Length;
                }
                else if (packFile.DataSource is PackedFileSource packedFileSource && isCorrectCompressionFormat)
                {
                    // The data is already in the right format so just get the compressed data
                    uncompressedFileSize = packedFileSource.UncompressedSize;
                    var compressedData = packedFileSource.ReadDataWithoutDecompressing();
                    data = compressedData;
                    fileSize = data.Length;
                }
                else
                {
                    data = packFile.DataSource.ReadData();
                    fileSize = data.Length;
                }

                // Write the data
                var offset = writer.BaseStream.Position;
                writer.Write(data);

                // Patch the file size metadata placeholder 
                var currentPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = file.FileSizeMetadataPosition;
                writer.Write(fileSize);
                writer.BaseStream.Position = currentPosition;

                // Update DataSource
                packFile.DataSource = new PackedFileSource(
                    packedFileSourceParent,
                    offset,
                    fileSize,
                    Header.HasEncryptedData,
                    shouldCompress,
                    file.IntendedCompressionFormat,
                    uncompressedFileSize);
            }
        }
    }
}
