using AssetEditor.Views.Settings;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.Command;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    public class MenuBarViewModel
    {
        IServiceProvider _serviceProvider;
        PackFileService _packfileService;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }

        public MenuBarViewModel(IServiceProvider provider, PackFileService packfileService)
        {
            _serviceProvider = provider;
            _packfileService = packfileService;
            OpenSettingsWindowCommand = new RelayCommand(OnButtonPressed);
        }

        void OnButtonPressed()
        {
            var window = _serviceProvider.GetRequiredService<SettingsWindow>();
            window.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
            window.ShowDialog();
        }
    }
}
