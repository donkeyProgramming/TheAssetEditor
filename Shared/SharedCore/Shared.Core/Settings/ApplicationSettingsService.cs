using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.Settings
{
    public enum CameraControlMode
    {
        BlenderStyle,
        AssetEditorStyle,
    }

    public record RecentPackFileInfo(string Path, PackFileContainerType ContainerType, bool IsReadOnly);

    public class ApplicationSettings
    {
        public record GamePathPair(GameTypeEnum Game, string Path);

        public ObservableCollection<RecentPackFileInfo> RecentPackFiles { get; set; } = [];
        public ThemeType Theme { get; set; } = ThemeType.DarkTheme;
        public BackgroundColour RenderEngineBackgroundColour { get; set; } = BackgroundColour.DarkGrey;
        public bool StartMaximised { get; set; } = false;
        public List<GamePathPair> GameDirectories { get; set; } = [];
        public GameTypeEnum CurrentGame { get; set; } = GameTypeEnum.Warhammer3;
        public bool LoadCaPacksByDefault { get; set; } = true;
        public bool ShowCAWemFiles { get; set; } = false;
        public bool IsFirstTimeStartingApplication { get; set; } = true;
        public bool IsDeveloperRun { get; set; } = false;
        public bool OnlyLoadLod0ForReferenceMeshes { get; set; } = true;
        public int VisualEditorsGridSize { get; set; } = 10;
        public Vector3 VertexSelectionColour { get; set; } = new Vector3(1.0f, 0.47f, 0.0f);
        public string SelectedLangauge { get; set; } = "en";
        public CameraControlMode CameraControlMode { get; set; } = CameraControlMode.AssetEditorStyle;
    }

    public class ApplicationSettingsService
    {
        private readonly ILogger _logger = Logging.Create<ApplicationSettingsService>();

        public bool AllowSettingsUpdate { get; set; } = false;

        public static string SettingsFile
        {
            get
            {
                return Path.Combine(DirectoryHelper.ApplicationDirectory, "ApplicationSettings.json");
            }
        }

        public ApplicationSettings CurrentSettings { get; private set; }

        public ApplicationSettingsService(GameTypeEnum currentGame = GameTypeEnum.Unknown)
        {
            CurrentSettings = new ApplicationSettings() { CurrentGame = currentGame };
        }

        public string? GetGamePathForCurrentGame()
        {
            var game = CurrentSettings.CurrentGame;
            if (game == GameTypeEnum.Unknown)
                return null;
            return GetGamePathForGame(game);
        }

        public void AddRecentlyOpenedPackFile(string path, PackFileContainerType containerType, bool isReadOnly)
        {
            var recentPackFiles = CurrentSettings.RecentPackFiles;
            var newEntry = new RecentPackFileInfo(path, containerType, isReadOnly);

            if (recentPackFiles.Any() && recentPackFiles.Last() == newEntry)
                return;

            var existing = recentPackFiles.FirstOrDefault(x => x.Path == path);
            if (existing != null)
                recentPackFiles.Remove(existing);

            recentPackFiles.Add(newEntry);

            if (recentPackFiles.Count > 15)
                recentPackFiles.RemoveAt(0);
        }

        public string? GetGamePathForGame(GameTypeEnum game)
        {
            var gameDirInfo = CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game);
            return gameDirInfo?.Path;
        }

        public void Save()
        {
            if (AllowSettingsUpdate == false)
                return;
            _logger.Here().Information($"Saving settings file {SettingsFile}");

            var jsonStr = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(SettingsFile, jsonStr);
        }

        public void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    var content = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<ApplicationSettings>(content);
                    if (settings == null)
                        _logger.Here().Information($"Failed to load settings - json parsing error.");
                    else
                        CurrentSettings = settings;

                    _logger.Here().Information($"Settings loaded.");
                    ValidateRecentPackFilePaths();
                    ValidateSettings();
                }
                catch (Exception ex)
                {
                    _logger.Here().Error($"Failed to load settings at {SettingsFile} due to {ex.Message}. Creating default settings");
                    CurrentSettings = new ApplicationSettings();
                    Save();
                }
            }
            else
            {
                _logger.Here().Warning("No settings found, saving default settings and using that");
                CurrentSettings = new ApplicationSettings();
                Save();
            }
        }

        void ValidateRecentPackFilePaths()
        {
            var recentPackFiles = CurrentSettings.RecentPackFiles;
            var invalidPacks = recentPackFiles.Where(x => !File.Exists(x.Path) && !Directory.Exists(x.Path)).ToList();

            foreach (var invalid in invalidPacks)
                recentPackFiles.Remove(invalid);
        }


        void ValidateSettings()
        {
            var areSettingsValid = true;
            List<string> settingsError = [];

            if (Enum.IsDefined(CurrentSettings.Theme) == false)
            {
                settingsError.Add($"Unkown theme setting - {CurrentSettings.Theme}");
                areSettingsValid = false;
            }

            if (Enum.IsDefined(CurrentSettings.RenderEngineBackgroundColour) == false)
            {
                settingsError.Add($"Unkown RenderEngineBackgroundColour setting - {CurrentSettings.RenderEngineBackgroundColour}");
                areSettingsValid = false;
            }

            if (Enum.IsDefined(CurrentSettings.CurrentGame) == false)
            {
                settingsError.Add($"Unkown CurrentGame setting - {CurrentSettings.CurrentGame}");
                areSettingsValid = false;
            }

            foreach (var currentGameDir in CurrentSettings.GameDirectories)
            {
                if (Enum.IsDefined(currentGameDir.Game) == false)
                {
                    settingsError.Add($"Unkown GameDir setting - {currentGameDir.Game}");
                    areSettingsValid = false;
                }
            }

            if (areSettingsValid)
            {
                _logger.Here().Information("Settings validated - no errors found");
                return;
            }

            _logger.Here().Error($"Settings contained errors:\n{string.Join("\n", settingsError)}");
            _logger.Here().Error($"Creating new settings file");
            CurrentSettings = new ApplicationSettings();
            Save();
        }
    }
}
