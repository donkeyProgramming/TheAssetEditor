using System;
using Editors.Audio.AudioProjectMerger;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class OpenAudioProjectMergerWindowCommand(IServiceProvider serviceProvider) : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<AudioProjectMergerWindow>();
            var viewModel = _serviceProvider.GetRequiredService<AudioProjectMergerViewModel>();
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}
