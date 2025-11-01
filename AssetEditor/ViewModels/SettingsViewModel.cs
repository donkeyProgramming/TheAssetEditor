using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Misc;
using Shared.Core.Settings;

namespace AssetEditor.ViewModels
{
    partial class SettingsViewModel : ObservableObject
    {
        private readonly ApplicationSettingsService _settingsService;

        public ObservableCollection<ThemeType> AvailableThemes { get; set; } = [];
        public ObservableCollection<BackgroundColour> RenderEngineBackgroundColours { get; set; } = [];
        public ObservableCollection<GameTypeEnum> Games { get; set; } = [];
        public ObservableCollection<GamePathItem> GameDirectores { get; set; } = [];

        [ObservableProperty] private ThemeType _currentTheme;
        [ObservableProperty] private BackgroundColour _currentRenderEngineBackgroundColour;
        [ObservableProperty] private bool _startMaximised;
        [ObservableProperty] private GameTypeEnum _currentGame;
        [ObservableProperty] private bool _loadCaPacksByDefault;
        [ObservableProperty] private bool _showCAWemFiles;
        [ObservableProperty] private string _wwisePath;
        [ObservableProperty] private bool _onlyLoadLod0ForReferenceMeshes;

        public SettingsViewModel(ApplicationSettingsService settingsService)
        {
            _settingsService = settingsService;
            AvailableThemes = new ObservableCollection<ThemeType>((ThemeType[])Enum.GetValues(typeof(ThemeType)));
            CurrentTheme = _settingsService.CurrentSettings.Theme;
            RenderEngineBackgroundColours = new ObservableCollection<BackgroundColour>((BackgroundColour[])Enum.GetValues(typeof(BackgroundColour)));
            CurrentRenderEngineBackgroundColour = _settingsService.CurrentSettings.RenderEngineBackgroundColour;
            StartMaximised = _settingsService.CurrentSettings.StartMaximised;
            Games = new ObservableCollection<GameTypeEnum>(GameInformationDatabase.Games.Values.OrderBy(game => game.DisplayName).Select(game => game.Type));
            CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            LoadCaPacksByDefault = _settingsService.CurrentSettings.LoadCaPacksByDefault;
            ShowCAWemFiles = _settingsService.CurrentSettings.ShowCAWemFiles;
            OnlyLoadLod0ForReferenceMeshes = _settingsService.CurrentSettings.OnlyLoadLod0ForReferenceMeshes;
            foreach (var game in GameInformationDatabase.Games.Values.OrderBy(game => game.DisplayName))
            {
                GameDirectores.Add(
                    new GamePathItem()
                    {
                        GameName = $"{game.DisplayName}",
                        GameType = game.Type,
                        Path = _settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type)?.Path
                    });
            }
            WwisePath = _settingsService.CurrentSettings.WwisePath;
        }


        [RelayCommand]
        private void Save()
        {
            _settingsService.CurrentSettings.Theme = CurrentTheme;
            _settingsService.CurrentSettings.RenderEngineBackgroundColour = CurrentRenderEngineBackgroundColour;
            _settingsService.CurrentSettings.StartMaximised = StartMaximised;
            _settingsService.CurrentSettings.CurrentGame = CurrentGame;
            _settingsService.CurrentSettings.LoadCaPacksByDefault = LoadCaPacksByDefault;
            _settingsService.CurrentSettings.ShowCAWemFiles = ShowCAWemFiles;
            _settingsService.CurrentSettings.OnlyLoadLod0ForReferenceMeshes = OnlyLoadLod0ForReferenceMeshes;
            _settingsService.CurrentSettings.GameDirectories.Clear();
            foreach (var item in GameDirectores)
                _settingsService.CurrentSettings.GameDirectories.Add(new ApplicationSettings.GamePathPair() { Game = item.GameType, Path = item.Path });
            _settingsService.CurrentSettings.WwisePath = WwisePath;
            _settingsService.Save();
            MessageBox.Show("Please restart the tool after updating settings!");
        }

        [RelayCommand]
        private void Browse()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Executable files (*.exe)|*.exe";
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == DialogResult.OK)
                WwisePath = dialog.FileName;
        }
    }

    class GamePathItem : NotifyPropertyChangedImpl
    {
        public GameTypeEnum GameType { get; set; }

        string _gameName;
        public string GameName { get => _gameName; set => SetAndNotify(ref _gameName, value); }

        string _path;
        public string Path { get => _path; set => SetAndNotify(ref _path, value); }

        public ICommand BrowseCommand { get; set; }

        public GamePathItem()
        {
            BrowseCommand = new RelayCommand(OnBrowse);
        }

        void OnBrowse()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Path = dialog.SelectedPath;
                var files = Directory.GetFiles(Path);
                var packFiles = files.Count(x => System.IO.Path.GetExtension(x) == ".pack");
                var manifest = files.Count(x => x.Contains("manifest.txt"));

                if (packFiles == 0 && manifest == 0)
                    System.Windows.MessageBox.Show($"The selected directory contains {packFiles} packfiles and {manifest} manifest files. It is probably not a game directory");
            }
        }
    }
}
