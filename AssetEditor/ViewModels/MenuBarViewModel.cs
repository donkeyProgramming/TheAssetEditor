using AssetEditor.Views.Settings;
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

        public ICommand OpenSettingsWindowCommand { get; set; }

        public MenuBarViewModel(IServiceProvider provider)
        {
            _serviceProvider = provider;
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
