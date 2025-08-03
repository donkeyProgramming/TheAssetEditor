using System.Data;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddEditorRowToViewerCommand(
    IAudioEditorService audioEditorService,
    IAudioProjectMutationUICommandFactory audioProjectMutationUICommandFactory,
    IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IAudioProjectMutationUICommandFactory _audioProjectMutationUICommandFactory = audioProjectMutationUICommandFactory;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(DataRow row)
        {
            // Store the row data in the Audio Project
            var selectedAudioProjectExplorerNode = _audioEditorService.SelectedAudioProjectExplorerNode;
            _audioProjectMutationUICommandFactory.Create(MutationType.Add, selectedAudioProjectExplorerNode.NodeType).Execute(row);

            // Display the row in the Viewer
            _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));

            // Reset the Editor
            _eventHub.Publish(new EditorTableRowAddedToViewerEvent());
        }
    }
}
