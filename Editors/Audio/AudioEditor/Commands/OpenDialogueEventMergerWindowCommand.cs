using System;
using Editors.Audio.DialogueEventMerger;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class OpenDialogueEventMergerWindowCommand(IServiceProvider serviceProvider) : IUiCommand
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public void Execute()
        {
            var window = _serviceProvider.GetRequiredService<DialogueEventMergerWindow>();
            var viewModel = _serviceProvider.GetRequiredService<DialogueEventMergerViewModel>();
            viewModel.SetCloseAction(window.Close);
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}
