using System.Collections.ObjectModel;
using System.Text.Json;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.Settings;

namespace Shared.Core.Services
{
    public class ApplicationSettings
    {
        public class GamePathPair
        {
            public GameTypeEnum Game { get; set; }
            public string Path { get; set; }
        }

        public ObservableCollection<string> RecentPackFilePaths { get; set; } = [];
        public ThemeType Theme { get; set; } = ThemeType.DarkTheme;
        public BackgroundColour RenderEngineBackgroundColour { get; set; } = BackgroundColour.DarkGrey;
        public bool StartMaximised { get; set; } = false;
        public List<GamePathPair> GameDirectories { get; set; } = [];
        public GameTypeEnum CurrentGame { get; set; } = GameTypeEnum.Warhammer3;
        public bool LoadCaPacksByDefault { get; set; } = true;
        public bool LoadWemFiles { get; set; } = true;
        public bool IsFirstTimeStartingApplication { get; set; } = true;
        public bool IsDeveloperRun { get; set; } = false;
        public string WwisePath { get; set; }
        public bool OnlyLoadLod0ForReferenceMeshes { get; set; } = true;

        public ApplicationSettings()
        {
            WwisePath = Environment.GetEnvironmentVariable("WWISEROOT") ?? "";
            if (!string.IsNullOrEmpty(WwisePath))
                WwisePath = Path.Combine(WwisePath, "Authoring", "x64", "Release", "bin", "WwiseCLI.exe");
        }
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

        public void AddRecentlyOpenedPackFile(string path)
        {
            var recentPackFilePaths = CurrentSettings.RecentPackFilePaths;

            if (recentPackFilePaths.Any() && recentPackFilePaths.Last() == path)
                return;

            if (recentPackFilePaths.Contains(path))
                recentPackFilePaths.Remove(path);

            recentPackFilePaths.Add(path);

            if (recentPackFilePaths.Count > 15)
                recentPackFilePaths.RemoveAt(0);
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
            else
            {
                _logger.Here().Warning("No settings found, saving default settings and using that");
                CurrentSettings = new ApplicationSettings();
                Save();
            }
        }

        void ValidateRecentPackFilePaths()
        {
            var recentPackfilePaths = CurrentSettings.RecentPackFilePaths;
            var invalidPacks = recentPackfilePaths.Where(path => !File.Exists(path)).ToList();

            foreach (var invalidPath in invalidPacks)
                recentPackfilePaths.Remove(invalidPath);
        }


        void ValidateSettings()
        {
            var areSettingsValid = true;
            List<string> settingsError = [];

            if (Enum.IsDefined(typeof(ThemeType), CurrentSettings.Theme) == false)
            {
                settingsError.Add($"Unkown theme setting - {CurrentSettings.Theme}");
                areSettingsValid = false;
            }

            if (Enum.IsDefined(typeof(BackgroundColour), CurrentSettings.RenderEngineBackgroundColour) == false)
            {
                settingsError.Add($"Unkown RenderEngineBackgroundColour setting - {CurrentSettings.RenderEngineBackgroundColour}");
                areSettingsValid = false;
            }

            if (Enum.IsDefined(typeof(GameTypeEnum), CurrentSettings.CurrentGame) == false)
            {
                settingsError.Add($"Unkown CurrentGame setting - {CurrentSettings.CurrentGame}");
                areSettingsValid = false;
            }

            foreach (var currentGameDir in CurrentSettings.GameDirectories)
            {
                if (Enum.IsDefined(typeof(GameTypeEnum), currentGameDir.Game) == false)
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
