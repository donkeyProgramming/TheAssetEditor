using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Serialization
{
    public record PackFileWriteInformation(
    PackFile PackFile,
    long SizePosition,
    CompressionFormat CurrentCompressionFormat,
    CompressionFormat IntendedCompressionFormat);

    public static class PackFileSerializerWriter
    {
        public static void SaveToByteArray(PackFileContainer container, BinaryWriter writer, GameInformation gameInformation)
        {
            if (container.Header.HasEncryptedData || container.Header.HasEncryptedIndex)
                throw new InvalidOperationException("Saving encrypted packs is not supported.");

            long headerSpecificBytes = 0;
            if (container.Header.HasIndexWithTimeStamp)
                headerSpecificBytes += 4;
            if (container.Header.Version == PackFileVersion.PFH5)
                headerSpecificBytes += 1;

            long fileNamesOffset = 0;

            var sortedFiles = container.FileList.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            foreach (var file in sortedFiles)
            {
                var fileSize = 4;
                var zeroTerminator = 1;
                var fileNameBytes = Encoding.UTF8.GetByteCount(file.Key);
                fileNamesOffset += fileSize + headerSpecificBytes + fileNameBytes + zeroTerminator;
            }

            container.Header.FileCount = (uint)container.FileList.Count;
            PackFileSerializerLoader.WriteHeader(container.Header, (uint)fileNamesOffset, writer);

            var filesToWrite = new List<PackFileWriteInformation>();

            // Write file metadata
            foreach (var file in sortedFiles)
            {
                var packFile = file.Value;

                // Determine compression info
                var currentCompressionFormat = CompressionFormat.None;
                if (packFile.DataSource is PackedFileSource packedFileSource)
                    currentCompressionFormat = packedFileSource.CompressionFormat;
                var firstFilePathPart = file.Key.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).First();
                var intendedCompressionFormat = FileCompression.GetCompressionFormat(gameInformation, firstFilePathPart, packFile.Extension);
                var shouldCompress = intendedCompressionFormat != CompressionFormat.None;

                // File size placeholder (rewritten later)
                var sizePosition = writer.BaseStream.Position;
                writer.Write(0);

                // Timestamp
                if (container.Header.HasIndexWithTimeStamp)
                    writer.Write(0);

                // Compression
                if (container.Header.Version == PackFileVersion.PFH5)
                    writer.Write(shouldCompress);

                // Filename
                var fileNameBytes = Encoding.UTF8.GetBytes(file.Key);
                writer.Write(fileNameBytes);

                // Zero terminator
                writer.Write((byte)0);

                filesToWrite.Add(new PackFileWriteInformation(packFile, sizePosition, currentCompressionFormat, intendedCompressionFormat));
            }

            var packedFileSourceParent = new PackedFileSourceParent { FilePath = container.SystemFilePath };

            // Write the files
            foreach (var file in filesToWrite)
            {
                var packFile = file.PackFile;
                byte[] data;
                uint uncompressedSize = 0;

                // Determine compression info
                var shouldCompress = file.IntendedCompressionFormat != CompressionFormat.None;
                var isCorrectCompressionFormat = file.CurrentCompressionFormat == file.IntendedCompressionFormat;

                // Read the data
                if (shouldCompress && !isCorrectCompressionFormat)
                {
                    // Decompress the data 
                    var uncompressedData = packFile.DataSource.ReadData();
                    uncompressedSize = (uint)uncompressedData.Length;

                    // Compress the data into the right format
                    var compressedData = FileCompression.Compress(uncompressedData, file.IntendedCompressionFormat);
                    data = compressedData;

                    // Validate new compression
                    var decompressedData = FileCompression.Decompress(compressedData, uncompressedData.Length, file.IntendedCompressionFormat);
                    if (decompressedData.Length != uncompressedData.Length)
                        throw new InvalidDataException($"Decompressed bytes {decompressedData.Length:N0} does not match the expected uncompressed bytes {uncompressedData.Length:N0}.");
                }
                else if (packFile.DataSource is PackedFileSource packedFileSource && isCorrectCompressionFormat)
                {
                    // The data is already in the right format so just get the data as is
                    uncompressedSize = packedFileSource.UncompressedSize;
                    data = packedFileSource.ReadDataWithoutDecompressing();
                }
                else
                    data = packFile.DataSource.ReadData();

                // Write the data
                var offset = writer.BaseStream.Position;
                writer.Write(data);

                // Patch the size from the position stored earlier
                var currentPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = file.SizePosition;
                writer.Write(data.Length);
                writer.BaseStream.Position = currentPosition;

                // We do not encrypt
                var isEncrypted = false;

                // Update DataSource
                packFile.DataSource = new PackedFileSource(
                    packedFileSourceParent,
                    offset,
                    data.Length,
                    isEncrypted,
                    shouldCompress,
                    file.IntendedCompressionFormat,
                    uncompressedSize);
            }
        }
    }
}
