using AnimationEditor.PropCreator.ViewModels;
using AssetEditor.Services;
using AssetEditor.Views.Settings;
using Common;
using Common.ApplicationSettings;
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
            OpenPropCreatorCommand = new RelayCommand(OpenPropCreatorEditor);
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
            var editorView = _toolFactory.CreateEditorViewModel<MainPropCreatorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }
    }
}
