using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class PasteViewerRowsCommand(
        IAudioEditorStateService audioEditorStateService,
        IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
        IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(List<DataRow> copiedRows)
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            foreach (var row in copiedRows)
            {
                _audioProjectMutationUICommandFactory.Create(MutationType.Add, selectedAudioProjectExplorerNode.NodeType).Execute(row);
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
                _eventHub.Publish(new EditorAddRowButtonEnablementUpdateRequestedEvent());
            }
        }
    }
}
