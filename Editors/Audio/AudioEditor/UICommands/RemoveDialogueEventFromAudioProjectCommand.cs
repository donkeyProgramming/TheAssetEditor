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
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var statePath = AudioProjectHelpers.GetStatePathFromRow(_audioRepository, row, dialogueEvent);
            if (statePath != null)
            {
                dialogueEvent.StatePaths.Remove(statePath);
                _eventHub.Publish(new RemoveViewerTableRowEvent(row));
            }
        }
    }
}
