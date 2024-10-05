using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.Services;

namespace AssetEditor.ViewModels
{
    partial class SettingsViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<GamePathItem> GameDirectores { get; set; } = new ObservableCollection<GamePathItem>();

        GameTypeEnum _currentGame;
        public GameTypeEnum CurrentGame { get => _currentGame; set => SetAndNotify(ref _currentGame, value); }

        bool _UseTextEditorForUnknownFiles;
        public bool UseTextEditorForUnknownFiles { get => _UseTextEditorForUnknownFiles; set => SetAndNotify(ref _UseTextEditorForUnknownFiles, value); }

        bool _loadCaPacksByDefault;
        public bool LoadCaPacksByDefault { get => _loadCaPacksByDefault; set => SetAndNotify(ref _loadCaPacksByDefault, value); }

        bool _autoGenerateAttachmentPointsFromMeshes;
        public bool AutoGenerateAttachmentPointsFromMeshes { get => _autoGenerateAttachmentPointsFromMeshes; set => SetAndNotify(ref _autoGenerateAttachmentPointsFromMeshes, value); }

        bool _LoadWemFiles;
        public bool LoadWemFiles { get => _LoadWemFiles; set => SetAndNotify(ref _LoadWemFiles, value); }

        string _wwisepath;
        public string WwisePath { get => _wwisepath; set => SetAndNotify(ref _wwisepath, value); }

        private readonly ApplicationSettingsService _settingsService;

        public SettingsViewModel(ApplicationSettingsService settingsService, GameInformationFactory gameInformationFactory)
        {
            _settingsService = settingsService;

            foreach (var game in gameInformationFactory.Games.OrderBy(g => g.DisplayName))
            {
                GameDirectores.Add(
                    new GamePathItem()
                    {
                        GameName = $"{game.DisplayName} Data Location",
                        GameType = game.Type,
                        Path = _settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type)?.Path
                    });
            }

            CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            UseTextEditorForUnknownFiles = _settingsService.CurrentSettings.UseTextEditorForUnknownFiles;
            LoadCaPacksByDefault = _settingsService.CurrentSettings.LoadCaPacksByDefault;
            AutoGenerateAttachmentPointsFromMeshes = _settingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes;
            LoadWemFiles = _settingsService.CurrentSettings.LoadWemFiles;
            WwisePath = _settingsService.CurrentSettings.WwisePath;
        }

        [RelayCommand] private void Save()
        {
            _settingsService.CurrentSettings.CurrentGame = CurrentGame;
            _settingsService.CurrentSettings.UseTextEditorForUnknownFiles = UseTextEditorForUnknownFiles;
            _settingsService.CurrentSettings.LoadCaPacksByDefault = LoadCaPacksByDefault;
            _settingsService.CurrentSettings.LoadWemFiles = LoadWemFiles;
            _settingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes = AutoGenerateAttachmentPointsFromMeshes;
            _settingsService.CurrentSettings.WwisePath = WwisePath;

            _settingsService.CurrentSettings.GameDirectories.Clear();
            foreach (var item in GameDirectores)
                _settingsService.CurrentSettings.GameDirectories.Add(new ApplicationSettings.GamePathPair() { Game = item.GameType, Path = item.Path });

            _settingsService.Save();
            MessageBox.Show("Please restart the tool after updating settings!");
        }

        [RelayCommand] private void Browse()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Executable files (*.exe)|*.exe";
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                WwisePath = dialog.FileName;
            }
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
