using System;
using Shared.Core.Events;
using AssetEditor.ViewModels;
using AssetEditor.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AssetEditor.UiCommands
{
    public class OpenSettingsDialogCommand : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider;

        public OpenSettingsDialogCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<SettingsWindow>();
            window.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
            window.ShowDialog();
        }
    }
}
