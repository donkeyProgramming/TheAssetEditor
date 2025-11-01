using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class AddEditorRowToViewerCommand(
    IAudioEditorStateService audioEditorStateService,
    IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
    IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(DataRow row)
        {
            // Store the row data in the Audio Project
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            _audioProjectMutationUICommandFactory.Create(MutationType.Add, selectedAudioProjectExplorerNode.Type).Execute(row);

            // Display the row in the Viewer
            _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));

            // Reset the Editor
            _eventHub.Publish(new EditorTableRowAddedToViewerEvent());
        }
    }
}
