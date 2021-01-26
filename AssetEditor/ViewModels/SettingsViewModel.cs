using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    class SettingsViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<GamePathItem> GameDirectores { get; set; } = new ObservableCollection<GamePathItem>();

        GameTypeEnum _currentGame;
        public GameTypeEnum CurrentGame { get => _currentGame; set => SetAndNotify(ref _currentGame, value); }

        bool _UseTextEditorForUnknownFiles;
        public bool UseTextEditorForUnknownFiles { get => _UseTextEditorForUnknownFiles; set => SetAndNotify(ref _UseTextEditorForUnknownFiles, value); }
        
        bool _loadCaPacksByDefault;
        public bool LoadCaPacksByDefault { get => _loadCaPacksByDefault; set => SetAndNotify(ref _loadCaPacksByDefault, value); }

        public ICommand SaveCommand { get; set; }

        ApplicationSettingsService _settingsService;
        public SettingsViewModel(ApplicationSettingsService settingsService)
        {
            _settingsService = settingsService;

            foreach (var game in GameInformationFactory.Games)
            {
                GameDirectores.Add(
                    new GamePathItem()
                    {
                        GameName = game.DisplayName,
                        GameType = game.Type,
                        Path = "I am a cool path"//_settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type)?.Path
                    });
            }

            CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            UseTextEditorForUnknownFiles = _settingsService.CurrentSettings.UseTextEditorForUnknownFiles;
            LoadCaPacksByDefault = _settingsService.CurrentSettings.LoadCaPacksByDefault;

            SaveCommand = new RelayCommand(OnSave);
        }

        void OnSave()
        {
            _settingsService.CurrentSettings.CurrentGame = CurrentGame;
            _settingsService.CurrentSettings.UseTextEditorForUnknownFiles = UseTextEditorForUnknownFiles;
            _settingsService.CurrentSettings.LoadCaPacksByDefault = LoadCaPacksByDefault;

            _settingsService.CurrentSettings.GameDirectories.Clear();
            foreach (var item in GameDirectores)
                _settingsService.CurrentSettings.GameDirectories.Add(new ApplicationSettings.GamePathPair() { Game = item.GameType, Path = item.Path });

            _settingsService.Save();
        }
    }

    class GamePathItem : NotifyPropertyChangedImpl
    {
        public GameTypeEnum GameType{ get; set; }

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
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                Path = dialog.FileName;



        }
    }
}
