using System;
using System.Collections.Generic;
using AssetEditor.ViewModels;
using AssetEditor.Views.Updater;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class OpenUpdaterWindowCommand(IServiceProvider serviceProvider) : IAeCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private List<Release> _newerReleases = new();

        public void Configure(List<Release> newerReleases)
        {
            _newerReleases = newerReleases;
        }

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<UpdaterWindow>();
            var viewModel = _serviceProvider.GetRequiredService<UpdaterViewModel>();
            viewModel.SetReleaseInfo(_newerReleases);
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}

