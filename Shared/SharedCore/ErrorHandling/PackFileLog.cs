using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.ErrorHandling
{
    public class CompressionStats
    {
        public long DiskSize { get; set; }
        public long UncompressedSize { get; set; }

        public CompressionStats(long diskSize = 0, long uncompressedSize = 0)
        {
            DiskSize = diskSize;
            UncompressedSize = uncompressedSize;
        }

        public void Add(CompressionStats stat)
        {
            DiskSize += stat.DiskSize;
            UncompressedSize += stat.UncompressedSize;
        }
    }

    public static class PackFileLog
    {
        private static readonly ILogger s_logger = Logging.CreateStatic(typeof(PackFileLog));

        public static Dictionary<CompressionFormat, CompressionStats> GetCompressionStats(PackFileContainer container)
        {
            var stats = new Dictionary<CompressionFormat, CompressionStats>();

            foreach (var packFile in container.FileList.Values)
            {
                if (packFile.DataSource is PackedFileSource source)
                {
                    var format = source.IsCompressed
                        ? source.CompressionFormat
                        : CompressionFormat.None;

                    if (!stats.TryGetValue(format, out var totals))
                    {
                        totals = new CompressionStats();
                        stats[format] = totals;
                    }

                    totals.DiskSize += source.Size;
                    totals.UncompressedSize += source.IsCompressed ? source.UncompressedSize : 0L;
                }
            }

            return stats;
        }

        public static void LogPackCompression(PackFileContainer container)
        {
            var stats = GetCompressionStats(container);
            var totalFiles = container.FileList.Count;
            var packSizeFmt = FormatSize(container.OriginalLoadByteSize);

            var loadingPart = $"Loading {container.Name}.pack ({totalFiles} files, {packSizeFmt})";

            var fileCounts = new Dictionary<CompressionFormat, int>();
            foreach (var pf in container.FileList.Values)
            {
                if (pf.DataSource is PackedFileSource src)
                {
                    var fmt = src.IsCompressed
                        ? src.CompressionFormat
                        : CompressionFormat.None;

                    if (!fileCounts.TryGetValue(fmt, out var cnt))
                        fileCounts[fmt] = 1;
                    else
                        fileCounts[fmt] = cnt + 1;
                }
            }

            var segments = stats
                .OrderBy(kvp => kvp.Key)
                .Select(kvp =>
                {
                    var fmt = kvp.Key;
                    var count = fileCounts.TryGetValue(fmt, out var c) ? c : 0;
                    var disk = FormatSize(kvp.Value.DiskSize);

                    if (fmt == CompressionFormat.None)
                        return $"{fmt}: {count} files, {disk} (Disk Size)";

                    var unc = FormatSize(kvp.Value.UncompressedSize);
                    return $"{fmt}: {count} files, {disk} (Disk Size), {unc} (Uncompressed Size)";
                })
                .ToList();

            var compressionPart = $"File Compression – {string.Join(" | ", segments)}";
            s_logger.Here().Information($"{loadingPart} | {compressionPart}");
        }

        public static void LogPacksCompression(IDictionary<CompressionFormat, CompressionStats> globalStats)
        {
            var segments = globalStats
                .OrderBy(kvp => kvp.Key)
                .Select(kvp =>
                {
                    var format = kvp.Key;
                    var diskFormatted = FormatSize(kvp.Value.DiskSize);

                    if (format == CompressionFormat.None)
                        return $"{format}: {diskFormatted} (Disk Size)";

                    var uncompressedFormatted = FormatSize(kvp.Value.UncompressedSize);
                    return $"{format}: {diskFormatted} (Disk Size), {uncompressedFormatted} (Uncompressed Size)";
                })
                .ToList();

            var totalDisk = globalStats.Values.Sum(stat => stat.DiskSize);
            var totalUncompressed = globalStats.Values.Sum(stat => stat.UncompressedSize);

            var totalDiskFormatted = FormatSize(totalDisk);
            var totalUncompressedFormatted = FormatSize(totalUncompressed);

            var totalSegment = $"Total: {totalDiskFormatted} (Disk Size), {totalUncompressedFormatted} (Uncompressed Size)";
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
