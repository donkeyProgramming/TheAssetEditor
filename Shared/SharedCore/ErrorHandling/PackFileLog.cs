using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.ErrorHandling
{
    public class CompressionInformation(long diskSize = 0, long uncompressedSize = 0)
    {
        public long DiskSize { get; set; } = diskSize;
        public long UncompressedSize { get; set; } = uncompressedSize;

        public void Add(CompressionInformation compressionInformation)
        {
            DiskSize += compressionInformation.DiskSize;
            UncompressedSize += compressionInformation.UncompressedSize;
        }
    }

    public static class PackFileLog
    {
        private static readonly ILogger s_logger = Logging.CreateStatic(typeof(PackFileLog));

        public static Dictionary<CompressionFormat, CompressionInformation> GetCompressionInformation(PackFileContainer container)
        {
            var compressionInformation = new Dictionary<CompressionFormat, CompressionInformation>();

            foreach (var packFile in container.FileList.Values)
            {
                if (packFile.DataSource is PackedFileSource source)
                {
                    var compressionFormat = source.IsCompressed ? source.CompressionFormat : CompressionFormat.None;
                    if (!compressionInformation.TryGetValue(compressionFormat, out var totals))
                    {
                        totals = new CompressionInformation();
                        compressionInformation[compressionFormat] = totals;
                    }

                    totals.DiskSize += source.Size;
                    totals.UncompressedSize += source.IsCompressed ? source.UncompressedSize : 0L;
                }
            }

            return compressionInformation;
        }

        public static void LogPackCompression(PackFileContainer container)
        {
            var compressionInformation = GetCompressionInformation(container);
            var totalFiles = container.FileList.Count;
            var packSize = FormatSize(container.OriginalLoadByteSize);

            var loadingPart = $"Loading {container.Name}.pack ({totalFiles} files, {packSize})";

            var fileCountsByCompressionFormat = new Dictionary<CompressionFormat, int>();
            var fileTypeCountsByCompressionFormat = new Dictionary<CompressionFormat, Dictionary<string, int>>();

            foreach (var packFile in container.FileList.Values)
            {
                if (packFile.DataSource is not PackedFileSource packedFileSource)
                    continue;

                var compressionFormat = packedFileSource.IsCompressed ? packedFileSource.CompressionFormat : CompressionFormat.None;

                if (!fileCountsByCompressionFormat.TryGetValue(compressionFormat, out var fileCount))
                    fileCountsByCompressionFormat[compressionFormat] = 1;
                else
                    fileCountsByCompressionFormat[compressionFormat] = fileCount + 1;

                var fileType = string.IsNullOrWhiteSpace(packFile.Extension) ? "no_extension" : packFile.Extension;

                if (!fileTypeCountsByCompressionFormat.TryGetValue(compressionFormat, out var fileTypeCounts))
                {
                    fileTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    fileTypeCountsByCompressionFormat[compressionFormat] = fileTypeCounts;
                }

                if (!fileTypeCounts.TryGetValue(fileType, out var fileTypeCount))
                    fileTypeCounts[fileType] = 1;
                else
                    fileTypeCounts[fileType] = fileTypeCount + 1;
            }

            var segments = new List<string>();

            foreach (var compressionEntry in compressionInformation.OrderBy(compressionEntry => compressionEntry.Key))
            {
                var compressionFormat = compressionEntry.Key;
                var count = fileCountsByCompressionFormat.TryGetValue(compressionFormat, out var fileCount)? fileCount : 0;
                var diskSize = FormatSize(compressionEntry.Value.DiskSize);

                var fileSizes = compressionFormat == CompressionFormat.None
                    ? $"Disk Size: {diskSize}"
                    : $"Disk Size: {diskSize}, Uncompressed Size: {FormatSize(compressionEntry.Value.UncompressedSize)}";

                var fileTypes = string.Empty;
                if (fileTypeCountsByCompressionFormat.TryGetValue(compressionFormat, out var fileTypeCounts) && fileTypeCounts.Count > 0)
                {
                    var fileTypeSegments = new List<string>();

                    foreach (var fileTypeEntry in fileTypeCounts.OrderBy(fileTypeEntry => fileTypeEntry.Key, StringComparer.OrdinalIgnoreCase))
                        fileTypeSegments.Add($"{fileTypeEntry.Key} ({fileTypeEntry.Value})");

                    fileTypes = $": {string.Join(", ", fileTypeSegments)}";
                }

                segments.Add($"{compressionFormat} ({count} files, {fileSizes}){fileTypes}");
            }

            s_logger.Here().Information($"{loadingPart} | {string.Join(" | ", segments)}");
        }

        public static void LogPacksCompression(IDictionary<CompressionFormat, CompressionInformation> allCompressionInformation)
        {
            var segments = new List<string>();

            foreach (var compressionEntry in allCompressionInformation.OrderBy(compressionEntry => compressionEntry.Key))
            {
                var compressionFormat = compressionEntry.Key;
                var diskSize = FormatSize(compressionEntry.Value.DiskSize);

                if (compressionFormat == CompressionFormat.None)
                {
                    segments.Add($"{compressionFormat}: {diskSize} (Disk Size)");
                    continue;
                }

                var uncompressedSize = FormatSize(compressionEntry.Value.UncompressedSize);
                segments.Add($"{compressionFormat}: {diskSize} (Disk Size), {uncompressedSize} (Uncompressed Size)");
            }

            var totalDiskSize = FormatSize(allCompressionInformation.Values.Sum(compressionInformation => compressionInformation.DiskSize));
            var totalUncompressedSize = FormatSize(allCompressionInformation.Values.Sum(compressionInformation => compressionInformation.UncompressedSize));

            var totalSegment = $"Total: {totalDiskSize} (Disk Size), {totalUncompressedSize} (Uncompressed Size)";
            var summary = string.Join(" | ", segments.Append(totalSegment));

            s_logger.Here().Information($"Size of compressed files in all packs by format - {summary}");
        }

        private static string FormatSize(long bytes)
        {
            var kb = 1024.0;
            var mb = kb * 1024.0;
            var gb = mb * 1024.0;

            if (bytes >= gb)
                return $"{bytes / gb:F2} GB";
            if (bytes >= mb)
                return $"{bytes / mb:F2} MB";
            return $"{bytes / kb:F2} KB";
        }
    }
}
