using System.Diagnostics;
using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Serialization
{
    // CopyAsIs: the bytes stored in the source pack are written unchanged, compressed or not
    record FileCompressionInfo(CompressionFormat IntendedCompressionFormat, bool DecompressBeforeSaving, bool CopyAsIs = false);

    class PackFileWriteInformation(PackFile pf, string fullFileName, long sizePosition, FileCompressionInfo compressionInfo)
    {
        public PackFile PackFile { get; set; } = pf;
        public string FullFileName { get; set; } = fullFileName;
        public long SizePosition { get; set; } = sizePosition;
        public FileCompressionInfo CompressionInfo { get; set; } = compressionInfo;
    }

    // The data source a saved file should read from once the new pack is in place
    record PackFileSourceUpdate(PackFile PackFile, string Path, PackedFileSource NewSource);

    static class PackFileSerializerWriter
    {
        private static readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileSerializerWriter));
        private static readonly IReadOnlyList<(string Path, string Content)> PackFileCorruptionDetectionFiles =
        [
            (@"!!!packfile_corruction_detection\packfile_corruction_detection_1.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 1"),
            (@"packfile_corruction_detection\packfile_corruction_detection_2.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 2"),
            (@"zzzz_packfile_corruction_detection\packfile_corruction_detection_3.txt", "This file is here to validate that the packfile has not been corrupted while saving. This is check 3")
        ];

        // Returns the new data sources rather than applying them: they point into outputFileName,
        // which only holds this layout once the caller has moved the written file into place
        public static IReadOnlyList<PackFileSourceUpdate> SaveToByteArray(string outputFileName, PackFileContainer container, BinaryWriter writer, GameInformation currentGameInformation)
        {
            var stopWatch = Stopwatch.StartNew();

            var ignoredFilePaths = container.PackFileSettings.IgnoredFilesWhenSerializing
                .Where(x => string.IsNullOrWhiteSpace(x) == false)
                .Select(PathNormalization.NormalizeFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var enableCorruptionDetection = container.PackFileSettings.EnablePackFileCorruptionDetection;
            var allFiles = container.GetAllFiles();
            var filesToSerialize = allFiles.Where(x => ignoredFilePaths.Contains(x.Key) == false && (!enableCorruptionDetection || !IsCorruptionDetectionFile(x.Key)));
            var sortedFiles = filesToSerialize.OrderBy(x => x.Key, PackFileSortHelper.PathComparer).ToList();

            if (enableCorruptionDetection)
                AddCorruptionDetectionFiles(sortedFiles);

            var numFiles = sortedFiles.Count;
            var packFileName = container.Name;
            _logger.Here().Information("Saving packfile {PackFileName} v={PackFileVersion} with {NumFiles} files to {OutputFileName}. Current game = {Game}", packFileName, container.Header.Version, numFiles, outputFileName, currentGameInformation.DisplayName);

            if (container.Header.HasEncryptedData || container.Header.HasEncryptedIndex)
                throw new InvalidOperationException("Saving encrypted packs is not supported.");

            var headerSpecificBytes = ComputeFileHeaderSpecificByte(container);
            var fileNamesOffset = ComputeFileNameOffset(headerSpecificBytes, sortedFiles);

            // Update and write header
            container.Header.FileCount = (uint)numFiles;
            WriteHeader(container.Header, (uint)fileNamesOffset, writer);

            // Write the core of the file
            var fileMetaDataTable = BuildMetaDataTable(sortedFiles, container, currentGameInformation);
            SerializeFileTable(fileMetaDataTable, container, writer);
            var sourceUpdates = SerializeFileBlob(outputFileName, fileMetaDataTable, writer);
            VerifyWrittenPack(outputFileName, writer, sourceUpdates);

            if (enableCorruptionDetection)
                ValidateCorruptionDetectionFiles(outputFileName, writer);
            
            stopWatch.Stop();
            _logger.Here().Information("Saving packfile {PackFileName} completed in {ElapsedMilliseconds} ms", packFileName, stopWatch.ElapsedMilliseconds);
            return sourceUpdates;
        }

        // Reads back the index just written and checks it lists exactly the files written, at the offsets
        // and sizes they were written with, all inside the file. Only the index is read, so it is cheap,
        // and unlike the corruption detection files it adds nothing to the pack.
        internal static void VerifyWrittenPack(string outputFileName, BinaryWriter writer, IReadOnlyList<PackFileSourceUpdate> written)
        {
            writer.Flush();
            var stream = writer.BaseStream;
            if (!stream.CanRead || !stream.CanSeek)
                throw new InvalidOperationException("Verifying a saved pack requires a readable, seekable output stream.");

            var originalPosition = stream.Position;
            PackFileContainer readBack;
            try
            {
                stream.Position = 0;
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                readBack = PackFileSerializerLoader.Load(outputFileName, stream.Length, reader, new CaPackDuplicateFileResolver());
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"The saved pack could not be read back, so it was not used: {e.Message}", e);
            }
            finally
            {
                stream.Position = originalPosition;
            }

            if (readBack.GetFileCount() != written.Count)
                throw new InvalidDataException($"The saved pack lists {readBack.GetFileCount()} files but {written.Count} were written, so it was not used.");

            foreach (var file in written)
            {
                if (readBack.FindFile(file.Path)?.DataSource is not PackedFileSource entry)
                    throw new InvalidDataException($"The saved pack is missing '{file.Path}', so it was not used.");

                if (entry.Offset != file.NewSource.Offset || entry.Size != file.NewSource.Size || entry.Offset + entry.Size > stream.Length)
                    throw new InvalidDataException($"The saved pack's entry for '{file.Path}' does not match the data written, so it was not used.");
            }
        }

        private static bool IsCorruptionDetectionFile(string relativePath)
        {
            var normalizedPath = PathNormalization.NormalizeFileName(relativePath);
            var fileName = Path.GetFileName(normalizedPath);
            return PackFileCorruptionDetectionFiles.Any(x =>
                string.Equals(normalizedPath, PathNormalization.NormalizeFileName(x.Path), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, Path.GetFileName(x.Path), StringComparison.OrdinalIgnoreCase));
        }

        private static void AddCorruptionDetectionFiles(List<KeyValuePair<string, PackFile>> sortedFiles)
        {
            foreach (var detectionFile in PackFileCorruptionDetectionFiles)
            {
                var bytes = Encoding.UTF8.GetBytes(detectionFile.Content);
                var packFile = new PackFile(Path.GetFileName(detectionFile.Path), new MemorySource(bytes));
                sortedFiles.Add(new KeyValuePair<string, PackFile>(detectionFile.Path, packFile));
            }

            sortedFiles.Sort((left, right) => PackFileSortHelper.PathComparer.Compare(left.Key, right.Key));
        }

        private static void ValidateCorruptionDetectionFiles(string outputFileName, BinaryWriter writer)
        {
            writer.Flush();
            var stream = writer.BaseStream;
            if (!stream.CanRead || !stream.CanSeek)
                throw new InvalidOperationException("Packfile corruption detection requires a readable, seekable output stream.");

            var originalPosition = stream.Position;
            stream.Position = 0;
            var loadedPack = PackFileSerializerLoader.Load(outputFileName, stream.Length, new BinaryReader(stream, Encoding.UTF8, leaveOpen: true), new CustomPackDuplicateFileResolver());
            stream.Position = originalPosition;

            foreach (var detectionFile in PackFileCorruptionDetectionFiles)
            {
                var packFile = loadedPack.FindFile(detectionFile.Path)
                    ?? throw new InvalidDataException($"Packfile corruption detection failed. Missing validation file '{detectionFile.Path}'.");

                var actualContent = Encoding.UTF8.GetString(packFile.DataSource is PackedFileSource packedFileSource
                    ? packedFileSource.ReadData(stream)
                    : packFile.DataSource.ReadData());
                if (!string.Equals(actualContent, detectionFile.Content, StringComparison.Ordinal))
                    throw new InvalidDataException($"Packfile corruption detection failed. Validation file '{detectionFile.Path}' had unexpected content.");
            }
        }

        static void WriteHeader(PFHeader header, uint fileContentSize, BinaryWriter writer)
        {
            _logger.Here().Information("Starting write Header");

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

            _logger.Here().Information("Finished writing");
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
            _logger.Here().Information("Starting to build FileTable");

            var filesToWrite = new List<PackFileWriteInformation>();
            foreach (var file in sortedFiles)
            {
                var packFile = file.Value;
                var fileCompressionInfo = CanCopyAsIs(packFile.DataSource, container.Header.Version, currentGameInformation)
                    ? new FileCompressionInfo(packFile.DataSource.CompressionFormat, false, CopyAsIs: true)
                    : DetermineFileCompression(container.Header.Version, currentGameInformation, file.Key, packFile.DataSource.CompressionFormat);
                filesToWrite.Add(new PackFileWriteInformation(packFile, file.Key, 0, fileCompressionInfo));
            }

            _logger.Here().Information("Finished building FileTable");

            return filesToWrite;
        }

        // A file that was read from a pack and not edited goes back exactly as it was stored.
        // Re-encoding content nobody touched can only change it; RPFM also leaves most files uncompressed.
        // Only new or edited files follow DetermineFileCompression.
        static bool CanCopyAsIs(IDataSource source, PackFileVersion outputVersion, GameInformation gameInformation)
        {
            if (source is not PackedFileSource { IsEncrypted: false } packed)
                return false;

            if (packed.IsCompressed == false)
                return true;

            // Stored compressed: the output needs PFH5's per-file compression flag and a format the game reads
            return outputVersion == PackFileVersion.PFH5
                && packed.CompressionFormat != CompressionFormat.None
                && gameInformation.CompressionFormats.Contains(packed.CompressionFormat);
        }

        static void SerializeFileTable(List<PackFileWriteInformation> fileMetaData, PackFileContainer container, BinaryWriter writer)
        {
            _logger.Here().Information("Starting SerializeFileTable");

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

            _logger.Here().Information("Finished SerializeFileTable");
        }

        static List<PackFileSourceUpdate> SerializeFileBlob(string outputFileName, List<PackFileWriteInformation> fileMetaDataTabel, BinaryWriter writer)
        {
            _logger.Here().Information("Starting SerializeFileBlob");

            var sourceUpdates = new List<PackFileSourceUpdate>(fileMetaDataTabel.Count);
            var packedFileSourceParent = new PackedFileSourceParent { FilePath = outputFileName };

            foreach (var fileMetaData in fileMetaDataTabel)
            {
                var packFile = fileMetaData.PackFile;
                byte[] data;
                uint uncompressedSize = 0;
                var shouldCompress = fileMetaData.CompressionInfo.IntendedCompressionFormat != CompressionFormat.None;

                if (fileMetaData.CompressionInfo.CopyAsIs)
                {
                    var stored = (PackedFileSource)packFile.DataSource;
                    data = stored.ReadDataWithoutDecompressing();
                    uncompressedSize = stored.UncompressedSize;
                }
                else
                {
                    // Read file data
                    if (fileMetaData.CompressionInfo.DecompressBeforeSaving == false && packFile.DataSource is PackedFileSource packedFileSource)
                        data = packedFileSource.ReadDataWithoutDecompressing();
                    else
                        data = packFile.DataSource.ReadData();
                }

                // Compress if needed
                if (shouldCompress && fileMetaData.CompressionInfo.CopyAsIs == false)
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

                var newSource = new PackedFileSource(
                    packedFileSourceParent,
                    offset,
                    data.Length,
                    false,     // We do not encrypt
                    shouldCompress,
                    fileMetaData.CompressionInfo.IntendedCompressionFormat,
                    uncompressedSize);
                sourceUpdates.Add(new PackFileSourceUpdate(packFile, fileMetaData.FullFileName, newSource));
            }

            _logger.Here().Information("Finished SerializeFileBlob");
            return sourceUpdates;
        }

        
    }
}
