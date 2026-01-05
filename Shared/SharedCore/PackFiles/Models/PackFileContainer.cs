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
            if (Header.HasEncryptedData || Header.HasEncryptedIndex)
                throw new InvalidOperationException("Saving encrypted packs is not supported.");

            long headerSpecificBytes = 0;
            if (Header.HasIndexWithTimeStamp)
                headerSpecificBytes += 4;
            if (Header.Version == PackFileVersion.PFH5)
                headerSpecificBytes += 1;

            long fileNamesOffset = 0;

            var sortedFiles = FileList.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            foreach (var file in sortedFiles)
            {
                var fileSize = 4;
                var zeroTerminator = 1;
                var fileNameBytes = Encoding.UTF8.GetByteCount(file.Key);
                fileNamesOffset += fileSize + headerSpecificBytes + fileNameBytes + zeroTerminator;
            }

            Header.FileCount = (uint)FileList.Count;
            PackFileSerializer.WriteHeader(Header, (uint)fileNamesOffset, writer);

            var filesToWrite = new List<PackFileWriteInfo>();

            // Write file metadata
            foreach (var file in sortedFiles)
            {
                var packFile = file.Value;

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
                uint uncompressedFileSize = 0;

                // Determine compression info
                var shouldCompress = file.IntendedCompressionFormat != CompressionFormat.None;
                var isCorrectCompressionFormat = file.CurrentCompressionFormat == file.IntendedCompressionFormat;

                // Read the data
                if (shouldCompress && !isCorrectCompressionFormat)
                {
                    // Decompress the data 
                    var uncompressedData = packFile.DataSource.ReadData();
                    uncompressedFileSize = (uint)uncompressedData.Length;

                    // Compress the data into the right format
                    var compressedData = PackFileCompression.Compress(uncompressedData, file.IntendedCompressionFormat);
                    data = compressedData;

                    // Validate new compression
                    var decompressedData = PackFileCompression.Decompress(compressedData);
                    if (decompressedData.Length != uncompressedData.Length)
                        throw new InvalidDataException($"Decompressed bytes {decompressedData.Length} does not match the expected uncompressed bytes {uncompressedData.Length}.");

                }
                else if (packFile.DataSource is PackedFileSource packedFileSource && isCorrectCompressionFormat)
                {
                    // The data is already in the right format so just get the data as is
                    uncompressedFileSize = packedFileSource.UncompressedSize;
                    data = packedFileSource.ReadDataWithoutDecompressing();
                }
                else
                    data = packFile.DataSource.ReadData();

                var fileSize = (uint)data.Length;

                // Write the data
                var offset = writer.BaseStream.Position;
                writer.Write(data);

                // Patch the file size metadata placeholder 
                var currentPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = file.FileSizeMetadataPosition;
                writer.Write(fileSize);
                writer.BaseStream.Position = currentPosition;

                // We do not encrypt
                var isEncrypted = false;

                // Update DataSource
                packFile.DataSource = new PackedFileSource(
                    packedFileSourceParent,
                    offset,
                    fileSize,
                    isEncrypted,
                    shouldCompress,
                    file.IntendedCompressionFormat,
                    uncompressedFileSize);
            }
        }
    }
}
