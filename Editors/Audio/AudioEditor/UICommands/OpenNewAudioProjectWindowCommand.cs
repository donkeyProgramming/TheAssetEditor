using System;
using Editors.Audio.AudioEditor.NewAudioProject;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class OpenNewAudioProjectWindowCommand : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider;

        public OpenNewAudioProjectWindowCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<NewAudioProjectWindow>();
            var viewModel = _serviceProvider.GetRequiredService<NewAudioProjectViewModel>();
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}
