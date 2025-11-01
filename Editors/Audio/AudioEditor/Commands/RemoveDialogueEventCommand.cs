using System.Data;
using System.Windows;
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
            var result = _dialogueEventService.RemoveStatePath(dialogueEventNodeName, statePathName);
            if (result)
                _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(row));
            else
            {
                var message = "State Path is incomplete probably due to a change to the Dialogue Event by CA. Add the missing State(s).";
                MessageBox.Show(message, "Error");
            }

        }
    }
}
