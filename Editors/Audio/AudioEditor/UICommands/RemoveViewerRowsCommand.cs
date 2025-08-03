using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveViewerRowsCommand(
        IAudioEditorService audioEditorService,
        IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
        IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(List<DataRow> selectedViewerRows)
        {
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            foreach (var row in selectedViewerRows)
                _audioProjectMutationUICommandFactory.Create(MutationType.Remove, selectedAudioProjectExplorerNode.NodeType).Execute(row);

            _eventHub.Publish(new EditorAddRowButtonEnablementUpdateRequestedEvent());
        }
    }
}
