using AssetEditor.Views.Settings;
using Common;
using CommonControls.Services;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
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

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }


        public ICommand OpenKitbashEditorCommand { get; set; }

        public MenuBarViewModel(IServiceProvider provider, PackFileService packfileService)
        {
            _serviceProvider = provider;
            _packfileService = packfileService;
            OpenSettingsWindowCommand = new RelayCommand(ShowSettingsDialog);
            OpenPackFileCommand = new RelayCommand(OpenPackFile);
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            OpenKitbashEditorCommand = new RelayCommand(OpenKitbasherTool);
        }

        void OpenPackFile()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("Pack", ".pack"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _logger.Here().Information($"Loading pack file {dialog.FileName}");
                 if( _packfileService.Load(dialog.FileName) == null)
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
                _packfileService.CreateNewPackFileContainer(window.TextValue, PackFileCAType.MOD);
            }
        }

        void OpenKitbasherTool()
        { 
        
        }
    }
}
