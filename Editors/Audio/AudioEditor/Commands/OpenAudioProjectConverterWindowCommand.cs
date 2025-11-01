using System;
using Editors.Audio.AudioProjectConverter;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class OpenAudioProjectConverterWindowCommand(IServiceProvider serviceProvider) : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<AudioProjectConverterWindow>();
            var viewModel = _serviceProvider.GetRequiredService<AudioProjectConverterViewModel>();
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}
