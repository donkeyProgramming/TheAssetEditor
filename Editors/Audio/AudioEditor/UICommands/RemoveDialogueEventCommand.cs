using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.Storage;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveDialogueEventCommand(
        IAudioEditorService audioEditorService,
        IDialogueEventService dialogueEventService,
        IAudioRepository audioRepository,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IDialogueEventService _dialogueEventService = dialogueEventService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.DialogueEvent;

        public void Execute(DataRow row)
        {
            var dialogueEventName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEventName);
            _dialogueEventService.RemoveStatePath(dialogueEventName, statePathName);

            // TODO: Do we need to then display a message to the user saying we can't do this until they fix the state path?
            var result = false;
            if (result == false)
                _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(row));
        }
    }
}
