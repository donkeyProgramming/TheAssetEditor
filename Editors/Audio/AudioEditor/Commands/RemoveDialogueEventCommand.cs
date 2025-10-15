using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class RemoveDialogueEventCommand(
        IAudioEditorStateService audioEditorStateService,
        IDialogueEventService dialogueEventService,
        IAudioRepository audioRepository,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IDialogueEventService _dialogueEventService = dialogueEventService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.DialogueEvent;

        public void Execute(DataRow row)
        {
            var dialogueEventNodeName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEventNodeName);
            _dialogueEventService.RemoveStatePath(dialogueEventNodeName, statePathName);

            // TODO: Do we need to then display a message to the user saying we can't do this until they fix the state path?
            var result = false;
            if (result == false)
                _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(row));
        }
    }
}
