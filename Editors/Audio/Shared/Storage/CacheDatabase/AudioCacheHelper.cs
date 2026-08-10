using System.IO;
using System.Security.Cryptography;
using System.Text;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Settings;

namespace Editors.Audio.Shared.Storage.CacheDatabase
{
    internal interface IAudioCacheHelper
    {
        string ComputeFingerprint(List<IPackFileContainer> containers, string cacheKind);
        string GetCacheFilePath(string label, string fingerprint);
        AudioCache TryLoadFromCache(string cacheFilePath, string fingerprint);
        AudioCache SaveAndLoadCache(AudioCacheSource source);
    }

    internal sealed record AudioCacheSource(string CacheFilePath, string Fingerprint, bool IsGameFiles, List<IPackFileContainer> BnkContainers, List<IPackFileContainer> DatContainers);

    internal class AudioCacheHelper(ApplicationSettingsService applicationSettingsService, DatLoader datLoader, BnkLoader bnkLoader) : IAudioCacheHelper
    {
        private readonly ApplicationSettingsService _applicationSettingsService = applicationSettingsService;
        private readonly DatLoader _datLoader = datLoader;
        private readonly BnkLoader _bnkLoader = bnkLoader;
        private readonly ILogger _logger = Logging.Create<AudioCacheHelper>();

        public string GetCacheFilePath(string label, string fingerprint)
        {
            var safeLabel = string.Join("_", label.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(DirectoryHelper.CacheDirectory, $"CachedAudioData_{safeLabel}_{fingerprint}.db");
        }

        public string ComputeFingerprint(List<IPackFileContainer> containers, string cacheKind)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var fingerprintedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Append(hash, $"game:{_applicationSettingsService.CurrentSettings.CurrentGame};kind:{cacheKind};");

            for (var containerIndex = 0; containerIndex < containers.Count; containerIndex++)
            {
                var container = containers[containerIndex];
                var relevantFiles = container.SearchFiles(null, [".bnk", ".dat", ".wwiseids"]);
                foreach (var (path, file) in relevantFiles.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
                {
                    Append(hash, $"{containerIndex}|{container.IsCaPackFile}|{path}|");
                    AppendDataSourceFingerprint(hash, container, path, file.DataSource, fingerprintedFiles);
                }
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }

        public AudioCache SaveAndLoadCache(AudioCacheSource source)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(source.CacheFilePath)!);

            _logger.Here().Information($"Building processed DAT data for the {(source.IsGameFiles ? "game files" : "project files")} audio cache");
            var datData = _datLoader.LoadDatData(source.DatContainers);

            using (var repository = new AudioCache(source.CacheFilePath))
            {
                repository.Save(source.Fingerprint, source.IsGameFiles, source.BnkContainers, _bnkLoader, datData);
            }

            var loaded = AudioCache.CreateFromFingerPrint(source.CacheFilePath, source.Fingerprint);
            if (loaded == null)
                throw new InvalidDataException($"Failed to load audio repository after saving. CacheFile: {source.CacheFilePath}");

            DeleteStaleCaches(source);
            return loaded;
        }

        public AudioCache TryLoadFromCache(string cacheFilePath, string fingerprint)
        {
            if (!File.Exists(cacheFilePath))
            {
                _logger.Here().Information($"Audio cache file does not exist: {cacheFilePath}");
                return null;
            }

            try
            {
                _logger.Here().Information($"Attempting to load audio cache from: {cacheFilePath} with fingerprint: {fingerprint}");
                var result = AudioCache.CreateFromFingerPrint(cacheFilePath, fingerprint);
                if (result == null)
                {
                    _logger.Here().Information($"Audio cache load returned null (fingerprint/schema mismatch) for: {cacheFilePath}");
                }
                return result;
            }
            catch (Exception exception)
            {
                _logger.Here().Warning($"Failed to load audio cache '{cacheFilePath}': {exception.Message}");
                return null;
            }
        }

        private void AppendDataSourceFingerprint(IncrementalHash hash, IPackFileContainer container, string relativePath, IDataSource dataSource, HashSet<string> fingerprintedFiles)
        {
            if (dataSource is PackedFileSource packedSource)
            {
                AppendFileFingerprint(hash, packedSource.Parent.FilePath, fingerprintedFiles);
                Append(hash, $"packed:{packedSource.Offset}|{packedSource.Size}|{packedSource.IsEncrypted}|{packedSource.IsCompressed}|{packedSource.CompressionFormat}|{packedSource.UncompressedSize};");
            }
            else if (dataSource is FileSystemSource && container.ContainerType == PackFileContainerType.SystemFolder && !string.IsNullOrWhiteSpace(container.SystemFilePath))
                AppendFileFingerprint(hash, Path.Combine(container.SystemFilePath, relativePath), fingerprintedFiles);
            else
            {
                Append(hash, $"memory:{dataSource.Size}|");
                hash.AppendData(SHA256.HashData(dataSource.ReadData()));
            }
        }

        private void AppendFileFingerprint(IncrementalHash hash, string path, HashSet<string> fingerprintedFiles)
        {
            var fullPath = Path.GetFullPath(path);
            if (!fingerprintedFiles.Add(fullPath))
                return;

            if (!File.Exists(fullPath))
            {
                _logger.Here().Warning($"Audio cache fingerprint could not find file '{fullPath}'");
                return;
            }

            var fileInfo = new FileInfo(fullPath);
            Append(hash, $"{fullPath}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks};");
        }

        private void DeleteStaleCaches(AudioCacheSource source)
        {
            var cacheDirectory = Path.GetDirectoryName(source.CacheFilePath);
            if (cacheDirectory == null || !Directory.Exists(cacheDirectory))
                return;

            var fileName = Path.GetFileName(source.CacheFilePath);
            var fingerprintSuffix = $"_{source.Fingerprint}.db";
            if (!fileName.EndsWith(fingerprintSuffix, StringComparison.OrdinalIgnoreCase))
                return;

            var labelPrefix = fileName[..^fingerprintSuffix.Length];
            var staleCacheFiles = Directory.EnumerateFiles(cacheDirectory, $"{labelPrefix}_*.db");

            foreach (var cacheFile in staleCacheFiles)
            {
                if (cacheFile.Equals(source.CacheFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    File.Delete(cacheFile);
                }
                catch (Exception exception)
                {
                    _logger.Here().Warning($"Failed to remove stale audio cache '{cacheFile}': {exception.Message}");
                }
            }
        }

        private static void Append(IncrementalHash hash, string value)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(value));
        }
    }
}
