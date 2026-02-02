using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Commands.AudioProjectMutation;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioProjectEditor.Table;
using Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectEditor
{
    public class AddRowsToViewerCommand(
    IAudioEditorStateService audioEditorStateService,
    IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
    IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(List<DataRow> rows)
        {
            foreach (var row in rows)
            {
                _audioProjectMutationUICommandFactory.Create(MutationType.Add, _audioEditorStateService.SelectedAudioProjectExplorerNode.Type).Execute(row);
                _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
                _eventHub.Publish(new EditorTableRowAddedToViewerEvent());
            }
        }
    }
}
