using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Commands.AudioProjectMutation;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioProjectEditor.Enablement;
using Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectViewer
{
    public class PasteViewerRowsCommand(
        IAudioEditorStateService audioEditorStateService,
        IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
        IEventHub eventHub) : IAeCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;
        private List<DataRow> _copiedRows = new();

        public void Configure(List<DataRow> copiedRows)
        {
            _copiedRows = copiedRows;
        }

        public void Execute()
        {
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            foreach (var row in _copiedRows)
            {
                var cmd = _audioProjectMutationUICommandFactory.Create(MutationType.AddByPaste, selectedAudioProjectExplorerNode.Type);
                cmd.Configure(row);
                cmd.Execute();
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
                _eventHub.Publish(new EditorAddRowButtonEnablementUpdateRequestedEvent());
            }
        }
    }
}
