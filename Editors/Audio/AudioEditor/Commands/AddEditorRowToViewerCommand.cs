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
            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            _audioProjectMutationUICommandFactory.Create(MutationType.Add, selectedAudioProjectExplorerNode.Type).Execute(row);
            _eventHub.Publish(new ViewerTableRowAddRequestedEvent(row));
            _eventHub.Publish(new EditorTableRowAddedToViewerEvent());
        }
    }
}
