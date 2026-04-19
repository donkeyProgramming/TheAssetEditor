using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
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
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = false,
        };

        public static string GetCacheFilePath(string gameDataFolder, string gameName)
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AssetEditor",
                "Cache");

            Directory.CreateDirectory(cacheDir);

            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(cacheDir, $"ca_pack_cache_{safeGameName}.json");
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
            var json = JsonSerializer.Serialize(data, s_jsonOptions);
            File.WriteAllText(cacheFilePath, json);
        }

        public static CachedContainerData? LoadCache(string cacheFilePath)
        {
            if (!File.Exists(cacheFilePath))
                return null;

            var json = File.ReadAllText(cacheFilePath);
            return JsonSerializer.Deserialize<CachedContainerData>(json, s_jsonOptions);
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
