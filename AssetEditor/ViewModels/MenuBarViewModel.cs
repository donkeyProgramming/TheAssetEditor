using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
using Common.GameInformation;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    public class MenuBarViewModel
    {
        ILogger _logger = Logging.Create<MainViewModel>();

        IServiceProvider _serviceProvider;
        PackFileService _packfileService;
        ToolFactory _toolFactory;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }
        public ICommand OpenAssetEditorFolderCommand { get; set; }
        public ICommand OpenAnimMetaDecocderCommand { get; set; }
        public ICommand OpenMountCreatorCommand { get; set; }


        public ICommand OpenRome2RePacksCommand { get; set; }
        public ICommand OpenThreeKingdomsPacksCommand { get; set; }
        public ICommand OpenWarhammer2PacksCommand { get; set; }
        public ICommand OpenTroyPacksCommand { get; set; }


        public ICommand OpenHelpCommand { get; set; }
        public ICommand OpenKitbashEditorCommand { get; set; }
        public ICommand OpenPropCreatorCommand { get; set; }
        public IEditorCreator EditorCreator { get; set; }

        public MenuBarViewModel(IServiceProvider provider, PackFileService packfileService, ToolFactory toolFactory)
        {
            _serviceProvider = provider;
            _packfileService = packfileService;
            _toolFactory = toolFactory;
            OpenSettingsWindowCommand = new RelayCommand(ShowSettingsDialog);
            OpenPackFileCommand = new RelayCommand(OpenPackFile);
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            OpenAssetEditorFolderCommand = new RelayCommand(OpenAssetEditorFolder);
            OpenKitbashEditorCommand = new RelayCommand(OpenKitbasherTool);
            OpenAnimMetaDecocderCommand = new RelayCommand(OpenAnimMetaDecocder);
            OpenMountCreatorCommand = new RelayCommand(OpenMountCreator);
            OpenPropCreatorCommand = new RelayCommand(OpenPropCreatorEditor);

            OpenRome2RePacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Rome_2_Remastered));
            OpenThreeKingdomsPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.ThreeKingdoms));
            OpenWarhammer2PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer2));
            OpenTroyPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Troy));

            OpenHelpCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://tw-modding.com/index.php/Tutorial:AssetEditor") { CreateNoWindow = true }));  
        }

        void OpenPackFile()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("Pack", ".pack"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _logger.Here().Information($"Loading pack file {dialog.FileName}");
                 if( _packfileService.Load(dialog.FileName, true) == null)
                    MessageBox.Show($"Unable to load packfiles {dialog.FileName}");
            }
        }

        void OpenGamePacks(GameTypeEnum game)
        {
            var settingsService = _serviceProvider.GetService<ApplicationSettingsService>();
            var settings = settingsService.CurrentSettings;
            var gamePath = settings.GameDirectories.FirstOrDefault(x => x.Game == game);
            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.Path))
            {
                MessageBox.Show("No path provided for game");
                return;
            }
            using (new WaitCursor())
            {
                _packfileService.LoadAllCaFiles(gamePath.Path, GameInformationFactory.GetGameById(game).DisplayName);
            }
        }

        void ShowSettingsDialog()
        {
            var window = _serviceProvider.GetRequiredService<SettingsWindow>();
            window.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
            window.ShowDialog();
        }

        void CreatePackFile()
        {
            TextInputWindow window = new TextInputWindow("New Packfile name", "");
            if (window.ShowDialog() == true)
            {
                var newPackFile = _packfileService.CreateNewPackFileContainer(window.TextValue, PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
        }

        void OpenAssetEditorFolder()
        {
            var path = DirectoryHelper.ApplicationDirectory;
            Process.Start("explorer.exe", path);
        }

        void OpenKitbasherTool()
        {
         //   var editorView = _toolFactory.CreateEdtior<KitbasherEditor.ViewModels.KitbasherViewModel>();
         //   EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenAnimMetaDecocder()
        {
            var editorView = _toolFactory.CreateEditorViewModel<AnimMetaEditor.ViewModels.MainDecoderViewModel>();

            editorView.ConfigureAsDecoder();
            EditorCreator.CreateEmptyEditor(editorView);
        }


        void OpenPropCreatorEditor()
        {
            var editorView = _toolFactory.CreateEditorViewModel<BaseAnimationViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenMountCreator()
        {
            var editorView = _toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }
    }
}
