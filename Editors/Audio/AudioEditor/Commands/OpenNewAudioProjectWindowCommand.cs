using System;
using Editors.Audio.AudioEditor.Presentation.NewAudioProject;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class OpenNewAudioProjectWindowCommand(IServiceProvider serviceProvider) : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

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
