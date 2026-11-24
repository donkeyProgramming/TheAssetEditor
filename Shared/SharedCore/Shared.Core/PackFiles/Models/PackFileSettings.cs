using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.PackFiles.Models
{
    public class PackFileSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private string? _saveLocationPath;
        private GameTypeEnum? _gameVersion;
        private bool _enablePackFileCorruptionDetection;
        private ObservableCollection<string> _ignoredFilesWhenSerializing = new();

        public PackFileSettings()
        {
            _ignoredFilesWhenSerializing.CollectionChanged += OnIgnoredFilesChanged;
        }

        [JsonIgnore]
        public bool SerializeToDisk { get; set; }

        public string? SaveLocationPath
        {
            get => _saveLocationPath;
            set
            {
                if (_saveLocationPath == value)
                    return;

                _saveLocationPath = value;
            }
        }

        public GameTypeEnum? GameVersion
        {
            get => _gameVersion;
            set
            {
                if (_gameVersion == value)
                    return;

                _gameVersion = value;
            }
        }

        public bool EnablePackFileCorruptionDetection
        {
            get => _enablePackFileCorruptionDetection;
            set
            {
                if (_enablePackFileCorruptionDetection == value)
                    return;

                _enablePackFileCorruptionDetection = value;
            }
        }

        public ObservableCollection<string> IgnoredFilesWhenSerializing
        {
            get => _ignoredFilesWhenSerializing;
            set
            {
                if (ReferenceEquals(_ignoredFilesWhenSerializing, value))
                    return;

                _ignoredFilesWhenSerializing.CollectionChanged -= OnIgnoredFilesChanged;
                _ignoredFilesWhenSerializing = value ?? new ObservableCollection<string>();
                _ignoredFilesWhenSerializing.CollectionChanged += OnIgnoredFilesChanged;
            }
        }

        // Files and folders in a folder project, relative to it, that AE does not load: they are not in the tree, not watched
        // and not packed. Defaults to .git, so a project kept in git does not pack its history.
        public List<string> ExcludedPaths { get => _excludedPaths; set => _excludedPaths = value ?? []; }
        private List<string> _excludedPaths = [".git"];

        public bool IsExcluded(string relativePath)
        {
            var path = PathNormalization.NormalizeFileName(relativePath).Trim('\\');
            foreach (var excluded in ExcludedPaths)
            {
                var entry = PathNormalization.NormalizeFileName(excluded).Trim('\\');
                if (entry.Length != 0 && (path == entry || path.StartsWith(entry + "\\", StringComparison.Ordinal)))
                    return true;
            }
            return false;
        }

        public void Save()
        {
            if (!SerializeToDisk)
                return;

            throw new InvalidOperationException("Serialized pack-file settings require a target path and file-system access.");
        }

        public void Save(string path, IFileSystemAccess fileSystemAccess)
        {
            if (!SerializeToDisk)
                return;

            var settingsToSerialize = new PackFileSettings
            {
                SaveLocationPath = SaveLocationPath,
                EnablePackFileCorruptionDetection = EnablePackFileCorruptionDetection,
                GameVersion = GameVersion,
                IgnoredFilesWhenSerializing = new ObservableCollection<string>(NormalizeIgnoredFiles(IgnoredFilesWhenSerializing)),
                ExcludedPaths = [.. ExcludedPaths]
            };

            var json = JsonSerializer.Serialize(settingsToSerialize, JsonOptions);
            fileSystemAccess.FileWriteAllBytes(path, Encoding.UTF8.GetBytes(json));
        }

        public void ApplySerializedSettings(PackFileSettings settings)
        {
            SaveLocationPath = settings.SaveLocationPath;
            EnablePackFileCorruptionDetection = settings.EnablePackFileCorruptionDetection;
            GameVersion = settings.GameVersion;
            IgnoredFilesWhenSerializing = new ObservableCollection<string>(NormalizeIgnoredFiles(settings.IgnoredFilesWhenSerializing));
            ExcludedPaths = [.. settings.ExcludedPaths ?? []];
        }

        public static PackFileSettings? Load(string path, IFileSystemAccess fileSystemAccess)
        {
            var settingsBytes = fileSystemAccess.FileReadAllBytes(path);
            var json = Encoding.UTF8.GetString(settingsBytes);
            var settings = JsonSerializer.Deserialize<PackFileSettings>(json, JsonOptions);
            if (settings == null || !string.IsNullOrWhiteSpace(settings.SaveLocationPath))
                return settings;

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("OutputPackFilePath", out var legacyOutputPath) && legacyOutputPath.ValueKind == JsonValueKind.String)
                settings.SaveLocationPath = legacyOutputPath.GetString();

            return settings;
        }

        private static List<string> NormalizeIgnoredFiles(IEnumerable<string>? ignoredFiles)
        {
            return (ignoredFiles ?? [])
                .Where(x => string.IsNullOrWhiteSpace(x) == false)
                .Select(PathNormalization.NormalizeFileName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void OnIgnoredFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}