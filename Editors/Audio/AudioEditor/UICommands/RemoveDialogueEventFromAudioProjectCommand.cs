using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveDialogueEventFromAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;
        private readonly IEventHub _eventHub;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.RemoveFromAudioProject;
        public NodeType NodeType => NodeType.DialogueEvent;

        public RemoveDialogueEventFromAudioProjectCommand(IAudioEditorService audioEditorService, IAudioRepository audioRepository, IEventHub eventHub)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
            _eventHub = eventHub;
        }

        public void Execute(DataRow row)
        {
            var dialogueEventName = _audioEditorService.SelectedExplorerNode.Name;
            var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(dialogueEventName);

            // TODO: Do we need to then display a message to the user saying we can't do this until they fix the state path?
            var statePath = dialogueEvent.GetStatePath(_audioRepository, row);
            if (statePath != null)
            {
                dialogueEvent.StatePaths.Remove(statePath);
                _eventHub.Publish(new RemoveViewerTableRowEvent(row));
            }
        }
    }
}
