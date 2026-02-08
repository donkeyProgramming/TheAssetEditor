using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Serialization
{
    record FileCompressionInfo(CompressionFormat IntendedCompressionFormat, bool DecompressBeforeSaving);

    class PackFileWriteInformation(PackFile pf, string fullFileName, long sizePosition, FileCompressionInfo compressionInfo)
    {
        public PackFile PackFile { get; set; } = pf;
        public string FullFileName { get; set; } = fullFileName;
        public long SizePosition { get; set; } = sizePosition;
        public FileCompressionInfo CompressionInfo { get; set; } = compressionInfo;
    }

    static class PackFileSerializerWriter
    {
        public static void SaveToByteArray(string outputFileName, PackFileContainer container, BinaryWriter writer, GameInformation currentGameInformation)
        {
            if (container.Header.HasEncryptedData || container.Header.HasEncryptedIndex)
                throw new InvalidOperationException("Saving encrypted packs is not supported.");

            var sortedFiles = container.FileList.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            var headerSpecificBytes = ComputeFileHeaderSpecificByte(container);
            var fileNamesOffset = ComputeFileNameOffset(headerSpecificBytes, sortedFiles);

            // Update and write header
            container.Header.FileCount = (uint)container.FileList.Count;
            WriteHeader(container.Header, (uint)fileNamesOffset, writer);

            // Write the core of the file
            var fileMetaDataTable = BuildMetaDataTable(sortedFiles, container, currentGameInformation);
            SerializeFileTable(fileMetaDataTable, container, writer);
            SerializeFileBlob(outputFileName, fileMetaDataTable, container, writer);    
        }

        public static void WriteHeader(PFHeader header, uint fileContentSize, BinaryWriter writer)
        {
            var packFileTypeStr = PackFileVersionConverter.ToString(header.Version);        // 4
            foreach (var c in packFileTypeStr)
                writer.Write(c);

            writer.Write(header.ByteMask);                                                  // 8
            writer.Write(header.DependantFiles.Count);                                      // 12

            var pack_file_index_size = 0;
            foreach (var file in header.DependantFiles)
                pack_file_index_size += file.Length + 1;

            writer.Write(pack_file_index_size);                                             // 16
            writer.Write(header.FileCount);                                                 // 20
            writer.Write(fileContentSize);                                                  // 24

            switch (header.Version)
            {
                case PackFileVersion.PFH0:
                    break;// Nothing needed to do
                case PackFileVersion.PFH2:
                case PackFileVersion.PFH3: // 64 bit timestamp
                    writer.Write(0);
                    writer.Write(0);
                    break;
                case PackFileVersion.PFH4:
                case PackFileVersion.PFH5:
                    if (header.HasExtendedHeader)
                        throw new Exception("Not supported packfile type");

                    writer.Write(PFHeader.DefaultTimeStamp);
                    break;

                default:
                    throw new Exception("Not supported packfile type");
            }

            foreach (var file in header.DependantFiles)
            {
                var fileNameBytes = Encoding.UTF8.GetBytes(file);
                writer.Write(fileNameBytes);
                writer.Write((byte)0);
            }
        }

        static long ComputeFileHeaderSpecificByte(PackFileContainer container)
        {
            long headerSpecificBytes = 0;
            if (container.Header.HasIndexWithTimeStamp)
                headerSpecificBytes += 4;
            if (container.Header.Version == PackFileVersion.PFH5)
                headerSpecificBytes += 1;

            return headerSpecificBytes;
        }

        static long ComputeFileNameOffset(long headerSpecificBytePerFile, IList<KeyValuePair<string, PackFile>> sortedFileList)
        {
            long fileNamesOffset = 0;
            foreach (var file in sortedFileList)
            {
                var fileSize = 4;
                var zeroTerminator = 1;
                var fileNameBytes = Encoding.UTF8.GetByteCount(file.Key);
                fileNamesOffset += fileSize + headerSpecificBytePerFile + fileNameBytes + zeroTerminator;
            }

            return fileNamesOffset;
        }
         
        public static FileCompressionInfo DetermineFileCompression(PackFileVersion outputPackFileVersion, GameInformation currentGameInformation, string fullFileName, CompressionFormat currentFileCompressionFormat)
        {
            var doesGameSupportCompression = FileCompression.DoesGameSupportCompression(currentGameInformation);
            var compressIfPossible = doesGameSupportCompression && outputPackFileVersion == PackFileVersion.PFH5;

            var targetFileCompressionFormat = FileCompression.GetCompressionFormat(currentGameInformation, fullFileName);
            var isFileCompressed = currentFileCompressionFormat != CompressionFormat.None;

            if (isFileCompressed == false)
            {
                if (compressIfPossible)
                {
                    // Case 1 - Not a compressed file, going to a packfile/game with compression
                    return new FileCompressionInfo(targetFileCompressionFormat, true);
                }
                else
                {
                    // Case 2 - Not a compressed file, going to a packfile/game without compression
                    return new FileCompressionInfo(CompressionFormat.None, false);
                }
            }
            else
            {
                if (compressIfPossible)
                {
                    // Case 3 - A compressed file, going to a packfile/game with compression. Same target and source format
                    if (currentFileCompressionFormat == targetFileCompressionFormat)
                        return new FileCompressionInfo(targetFileCompressionFormat, false);

                    // Case 4 - A compressed file, going to a packfile/game with compression. Different target and source format
                    return new FileCompressionInfo(targetFileCompressionFormat, true);
                }
                else
                {
                    // Case 5 - A compressed file, going to a packfile/game without compression
                    return new FileCompressionInfo(CompressionFormat.None, true);
                }
            }
        }

        public static List<PackFileWriteInformation> BuildMetaDataTable(IList<KeyValuePair<string, PackFile>> sortedFiles, PackFileContainer container, GameInformation currentGameInformation)
        {
            var filesToWrite = new List<PackFileWriteInformation>();
            foreach (var file in sortedFiles)
            {
                var packFile = file.Value;
                var fileCompressionInfo = DetermineFileCompression(container.Header.Version, currentGameInformation, file.Key, packFile.DataSource.CompressionFormat);
                filesToWrite.Add(new PackFileWriteInformation(packFile, file.Key, 0, fileCompressionInfo));
            }

            return filesToWrite;
        }

        public static void SerializeFileTable(List<PackFileWriteInformation> fileMetaData, PackFileContainer container, BinaryWriter writer)
        {
            // Write file table
            // FileStartPosition
            // TimeStamp
            // CompressionFlag
            // FileName
            // ZeroTerminator for FileName
            foreach (var file in fileMetaData)
            {
                // File size placeholder (rewritten later)
                var sizePosition = writer.BaseStream.Position;
                writer.Write(0);
                file.SizePosition = sizePosition;   

                // Timestamp
                if (container.Header.HasIndexWithTimeStamp)
                    writer.Write(0);

                // Even if we do not compress - we alsways need to write the flag for PFH5
                if (container.Header.Version == PackFileVersion.PFH5)
                {
                    var shouldCompress = file.CompressionInfo.IntendedCompressionFormat != CompressionFormat.None;
                    writer.Write(shouldCompress);
                }

                // Filename
                var fileNameBytes = Encoding.UTF8.GetBytes(file.FullFileName);
                writer.Write(fileNameBytes);
                writer.Write((byte)0); // Zero terminator
            }
        }

        public static void SerializeFileBlob(string outputFileName, List<PackFileWriteInformation> fileMetaDataTabel, PackFileContainer container, BinaryWriter writer)
        {
            foreach (var fileMetaData in fileMetaDataTabel)
            {
                var packFile = fileMetaData.PackFile;
                byte[] data;
                uint uncompressedSize = 0;

                // Read file data
                if (fileMetaData.CompressionInfo.DecompressBeforeSaving == false && packFile.DataSource is PackedFileSource packedFileSource)
                    data = packedFileSource.ReadDataWithoutDecompressing();
                else
                    data = packFile.DataSource.ReadData();

                // Compress if needed
                var shouldCompress = fileMetaData.CompressionInfo.IntendedCompressionFormat != CompressionFormat.None;
                if (shouldCompress)
                {
                    var uncompressedData = packFile.DataSource.ReadData();
                    uncompressedSize = (uint)uncompressedData.Length;

                    // Compress the data into the right format
                    var compressedData = FileCompression.Compress(uncompressedData, fileMetaData.CompressionInfo.IntendedCompressionFormat);
                    data = compressedData;

                    // Validate new compression
                    var decompressedData = FileCompression.Decompress(compressedData, uncompressedData.Length, fileMetaData.CompressionInfo.IntendedCompressionFormat);
                    if (decompressedData.Length != uncompressedData.Length)
                        throw new InvalidDataException($"Decompressed bytes {decompressedData.Length:N0} does not match the expected uncompressed bytes {uncompressedData.Length:N0}.");
                }

                // Write the data
                var offset = writer.BaseStream.Position;
                writer.Write(data);

                // Patch the size from the position stored earlier
                var currentPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = fileMetaData.SizePosition;
                writer.Write(data.Length);
                writer.BaseStream.Position = currentPosition;

                // Update DataSource
                var packedFileSourceParent = new PackedFileSourceParent { FilePath = outputFileName };
                packFile.DataSource = new PackedFileSource(
                    packedFileSourceParent,
                    offset,
                    data.Length,
                    false,     // We do not encrypt
                    shouldCompress,
                    fileMetaData.CompressionInfo.IntendedCompressionFormat,
                    uncompressedSize);
            }
        }

        
    }
}
