using Common.GameInformation;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Common.ApplicationSettings
{
   
    public class ApplicationSettingsService
    {
        public delegate void SettingsChangedDelegate(ApplicationSettings settings);
        public event SettingsChangedDelegate SettingsChanged;

        ILogger _logger = Logging.Create<ApplicationSettingsService>();

        string SettingsFile
        {
            get
            {
                return Path.Combine(DirectoryHelper.ApplicationDirectory, "ApplicationSettings.json");
            }
        }

        public ApplicationSettings CurrentSettings { get; set; }


        public ApplicationSettingsService()
        {
            _logger.Here().Information("Creating ApplicationSettingsService");
            Load();
        }

        public string GetGamePathForCurrentGame()
        {
            var game = CurrentSettings.CurrentGame;
            if (game == GameTypeEnum.Unknown)
                return null;
            return GetGamePathForGame(game);
        }

        public string GetGamePathForGame(GameTypeEnum game)
        {
            var gameDirInfo = CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game);
            return gameDirInfo?.Path;
        }

        //public void AddLastUsedFile(string filePath)
        //{
        //    int maxRecentFiles = 5;
        //
        //    // Remove the file if it is add already
        //    var index = CurrentSettings.RecentUsedFiles.IndexOf(filePath);
        //    if (index != -1)
        //        CurrentSettings.RecentUsedFiles.RemoveAt(index);
        //
        //    // Add the file
        //    CurrentSettings.RecentUsedFiles.Insert(0, filePath);
        //
        //    // Ensure we only have maxRecentFiles in the list
        //    var currentFileCount = CurrentSettings.RecentUsedFiles.Count;
        //    if (currentFileCount > maxRecentFiles)
        //    {
        //        CurrentSettings.RecentUsedFiles.RemoveRange(maxRecentFiles, currentFileCount - maxRecentFiles);
        //    }
        //    Save();
        //}

        public void Save()
        {
            _logger.Here().Information($"Saving settings file {SettingsFile}");

            var jsonStr = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
            File.WriteAllText(SettingsFile, jsonStr);

            SettingsChanged?.Invoke(CurrentSettings);
        }

        void Load()
        {
            if (File.Exists(SettingsFile))
            {
                _logger.Here().Information($"Loading existing settings file {SettingsFile}");

                var content = File.ReadAllText(SettingsFile);
                _logger.Here().Information(content);
                CurrentSettings = JsonConvert.DeserializeObject<ApplicationSettings>(content);

                _logger.Here().Information($"Settings loaded.");
            }
            else
            {
                _logger.Here().Warning("No settings found, saving default settings and using that");
                CurrentSettings = new ApplicationSettings();
                Save();
            }
        }
    }
}
