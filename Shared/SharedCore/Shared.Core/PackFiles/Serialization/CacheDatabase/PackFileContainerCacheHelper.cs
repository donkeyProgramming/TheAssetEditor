using System.Security.Cryptography;
using System.Text;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models.Containers;

namespace Shared.Core.PackFiles.Serialization.CacheDatabase
{
    interface IPackFileContainerCacheHelper
    {
        string ComputeFingerprint(List<string> packFileNames);
        string GetCacheFilePath(string gameName, string cacheId);
        CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint);
        CachedPackFileContainer SaveAndLoadCache(string fingerprint, PackFileContainer container, string cacheFilePath);
    }

    class PackFileContainerCacheHelper : IPackFileContainerCacheHelper
    {
        private readonly ILogger _logger = Logging.Create<PackFileContainerCacheHelper>();
        public string GetCacheFilePath(string gameName, string cacheId)
        {
            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(DirectoryHelper.CacheDirectory, $"CachedGameFiles_{safeGameName}_{cacheId}.db");
        }

        public string ComputeFingerprint(List<string> packFileNames)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();

            if (packFileNames.Count == 0)
                throw new Exception("Trying to compute CachecFingerPrint, but no files provied");

            var foundFiles = 0;
            foreach (var packFileName in packFileNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(packFileName))
                {
                    foundFiles++;

                    var info = new FileInfo(packFileName);
                    sb.Append(packFileName);
                    sb.Append('|');
                    sb.Append(info.Length);
                    sb.Append('|');
                    sb.Append(info.LastWriteTimeUtc.Ticks);
                    sb.Append(';');
                }
                else
                {
                    _logger.Here().Warning($"Trying to compute CachecFingerPrint, but file {packFileName} is not found");
                }
            }

            if (foundFiles == 0)
                throw new Exception("Trying to compute CachecFingerPrint, but no files found. This will result in a default ID which can cause problems");

            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash);
        }

        public CachedPackFileContainer SaveAndLoadCache(string fingerprint, PackFileContainer container, string cacheFilePath)
        {
            using (var temp = new CachedPackFileContainer(container.Name, cacheFilePath))
            {
                temp.Save(fingerprint, container);
            }

            var loaded = CachedPackFileContainer.CreateFromFingerPrint(cacheFilePath, fingerprint);
            if (loaded == null)
                throw new Exception($"Failed to load from cache after saving. CacheFile: {cacheFilePath}");

            DeleteStaleCaches(cacheFilePath, fingerprint);
            return loaded;
        }

        // Every game update changes the fingerprint and so the cache file name. Without this, each patch leaves a
        // ~300MB cache behind for Warhammer III. Only files named exactly like this cache with another fingerprint are removed.
        private void DeleteStaleCaches(string cacheFilePath, string fingerprint)
        {
            var cacheDirectory = Path.GetDirectoryName(cacheFilePath);
            var fileName = Path.GetFileName(cacheFilePath);
            var fingerprintSuffix = $"{fingerprint}.db";
            if (cacheDirectory == null || !Directory.Exists(cacheDirectory) || !fileName.EndsWith(fingerprintSuffix, StringComparison.OrdinalIgnoreCase))
                return;

            var prefix = fileName[..^fingerprintSuffix.Length];
            foreach (var candidate in Directory.EnumerateFiles(cacheDirectory, $"{prefix}*.db"))
            {
                var candidateFingerprint = Path.GetFileNameWithoutExtension(candidate)[prefix.Length..];
                if (candidateFingerprint.Equals(fingerprint, StringComparison.OrdinalIgnoreCase) || !IsFingerprint(candidateFingerprint))
                    continue;

                try
                {
                    File.Delete(candidate);
                    _logger.Here().Information($"Removed stale pack file cache '{candidate}'");
                }
                catch (Exception ex)
                {
                    _logger.Here().Warning($"Failed to remove stale pack file cache '{candidate}': {ex.Message}");
                }
            }
        }

        private static bool IsFingerprint(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

        public CachedPackFileContainer? TryLoadFromCache(string cacheFilePath, string fingerprint)
        {
            if (!File.Exists(cacheFilePath))
            {
                _logger.Here().Information($"Cache file does not exist: {cacheFilePath}");
                return null;
            }

            try
            {
                _logger.Here().Information($"Attempting to load cache from: {cacheFilePath} with fingerprint: {fingerprint}");
                var result = CachedPackFileContainer.CreateFromFingerPrint(cacheFilePath, fingerprint);
                if (result == null)
                    _logger.Here().Information($"Cache load returned null (fingerprint/schema mismatch) for: {cacheFilePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Here().Warning($"Failed to load from cache '{cacheFilePath}': {ex.Message}");
                return null;
            }
        }
    }
}
