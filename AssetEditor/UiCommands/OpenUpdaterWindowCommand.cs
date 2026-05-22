using System;
using System.Collections.Generic;
using AssetEditor.ViewModels;
using AssetEditor.Views.Updater;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class OpenUpdaterWindowCommand(IServiceProvider serviceProvider) : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public void Execute(List<Release> newerReleases)
        {
            var window = _serviceProvider.GetRequiredService<UpdaterWindow>();
            var viewModel = _serviceProvider.GetRequiredService<UpdaterViewModel>();
            viewModel.SetReleaseInfo(newerReleases);
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}

