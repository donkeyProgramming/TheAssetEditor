using System.Security.Cryptography;
using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Shared.Core.PackFiles.Serialization
{
    internal record CachedFileEntry(
        string RelativePath,
        string FileName,
        string SourcePackFilePath,
        long Offset,
        long Size,
        bool IsEncrypted,
        bool IsCompressed,
        CompressionFormat CompressionFormat,
        uint UncompressedSize);

    internal class CachedContainerData
    {
        public string Fingerprint { get; set; } = "";
        public string ContainerName { get; set; } = "";
        public string SystemFilePath { get; set; } = "";
        public List<string> SourcePackFilePaths { get; set; } = [];
        public List<CachedFileEntry> Files { get; set; } = [];
    }

    internal static class PackFileContainerCacheHelper
    {
        private static readonly byte[] s_magic = "AEPC"u8.ToArray();
        private const int CacheVersion = 1;

        public static string GetCacheFilePath(string gameDataFolder, string gameName)
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AssetEditor",
                "Cache");

            Directory.CreateDirectory(cacheDir);

            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(cacheDir, $"ca_pack_cache_{safeGameName}.bin");
        }

        public static string ComputeFingerprint(string gameDataFolder, List<string> packFileNames)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();

            foreach (var packFileName in packFileNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var fullPath = Path.Combine(gameDataFolder, packFileName);
                if (File.Exists(fullPath))
                {
                    var info = new FileInfo(fullPath);
                    sb.Append(packFileName);
                    sb.Append('|');
                    sb.Append(info.Length);
                    sb.Append('|');
                    sb.Append(info.LastWriteTimeUtc.Ticks);
                    sb.Append(';');
                }
            }

            var manifestPath = Path.Combine(gameDataFolder, "manifest.txt");
            if (File.Exists(manifestPath))
            {
                var info = new FileInfo(manifestPath);
                sb.Append("manifest.txt|");
                sb.Append(info.Length);
                sb.Append('|');
                sb.Append(info.LastWriteTimeUtc.Ticks);
            }

            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash);
        }

        public static void SaveCache(CachedContainerData data, string cacheFilePath)
        {
            using var stream = File.Create(cacheFilePath);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write(s_magic);
            writer.Write(CacheVersion);

            writer.Write(data.Fingerprint);
            writer.Write(data.ContainerName);
            writer.Write(data.SystemFilePath);

            writer.Write(data.SourcePackFilePaths.Count);
            foreach (var path in data.SourcePackFilePaths)
                writer.Write(path);

            writer.Write(data.Files.Count);
            foreach (var entry in data.Files)
            {
                writer.Write(entry.RelativePath);
                writer.Write(entry.FileName);
                writer.Write(entry.SourcePackFilePath);
                writer.Write(entry.Offset);
                writer.Write(entry.Size);
                writer.Write(entry.IsEncrypted);
                writer.Write(entry.IsCompressed);
                writer.Write((int)entry.CompressionFormat);
                writer.Write(entry.UncompressedSize);
            }
        }

        public static CachedContainerData? LoadCache(string cacheFilePath)
        {
            if (!File.Exists(cacheFilePath))
                return null;

            using var stream = File.OpenRead(cacheFilePath);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var magic = reader.ReadBytes(s_magic.Length);
            if (!magic.AsSpan().SequenceEqual(s_magic))
                return null;

            var version = reader.ReadInt32();
            if (version != CacheVersion)
                return null;

            var data = new CachedContainerData
            {
                Fingerprint = reader.ReadString(),
                ContainerName = reader.ReadString(),
                SystemFilePath = reader.ReadString(),
            };

            var sourcePathCount = reader.ReadInt32();
            for (var i = 0; i < sourcePathCount; i++)
                data.SourcePackFilePaths.Add(reader.ReadString());

            var fileCount = reader.ReadInt32();
            for (var i = 0; i < fileCount; i++)
            {
                data.Files.Add(new CachedFileEntry(
                    RelativePath: reader.ReadString(),
                    FileName: reader.ReadString(),
                    SourcePackFilePath: reader.ReadString(),
                    Offset: reader.ReadInt64(),
                    Size: reader.ReadInt64(),
                    IsEncrypted: reader.ReadBoolean(),
                    IsCompressed: reader.ReadBoolean(),
                    (CompressionFormat)reader.ReadInt32(),
                    UncompressedSize: reader.ReadUInt32()));
            }

            return data;
        }

        public static CachedContainerData BuildCacheData(string fingerprint, PackFileContainer container)
        {
            var data = new CachedContainerData
            {
                Fingerprint = fingerprint,
                ContainerName = container.Name,
                SystemFilePath = container.SystemFilePath,
                SourcePackFilePaths = container.SourcePackFilePaths.ToList(),
            };

            foreach (var (relativePath, packFile) in container.FileList)
            {
                if (packFile.DataSource is PackedFileSource source)
                {
                    data.Files.Add(new CachedFileEntry(
                        relativePath,
                        packFile.Name,
                        source.Parent.FilePath,
                        source.Offset,
                        source.Size,
                        source.IsEncrypted,
                        source.IsCompressed,
                        source.CompressionFormat,
                        source.UncompressedSize));
                }
            }

            return data;
        }

        public static CachedPackFileContainer RestoreFromCache(CachedContainerData data)
        {
            var container = new CachedPackFileContainer(data.ContainerName)
            {
                SystemFilePath = data.SystemFilePath,
            };

            foreach (var sourcePath in data.SourcePackFilePaths)
                container.SourcePackFilePaths.Add(sourcePath);

            var parentCache = new Dictionary<string, PackedFileSourceParent>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in data.Files)
            {
                if (!parentCache.TryGetValue(entry.SourcePackFilePath, out var parent))
                {
                    parent = new PackedFileSourceParent { FilePath = entry.SourcePackFilePath };
                    parentCache[entry.SourcePackFilePath] = parent;
                }

                var source = new PackedFileSource(
                    parent,
                    entry.Offset,
                    entry.Size,
                    entry.IsEncrypted,
                    entry.IsCompressed,
                    entry.CompressionFormat,
                    entry.UncompressedSize);

                container.FileList[entry.RelativePath] = new PackFile(entry.FileName, source);
            }

            return container;
        }
    }
}
