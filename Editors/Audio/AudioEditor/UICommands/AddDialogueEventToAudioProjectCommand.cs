using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddDialogueEventToAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IAudioRepository _audioRepository;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.DialogueEvent;

        public AddDialogueEventToAudioProjectCommand(IAudioEditorService audioEditorService, IAudioRepository audioRepository)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
        }

        public void Execute(DataRow row)
        {
            var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            var statePath = AudioProjectHelpers.CreateStatePathFromRow(_audioRepository, _audioEditorService.AudioFiles, _audioEditorService.AudioSettings, row, dialogueEvent);
            AudioProjectHelpers.InsertStatePathAlphabetically(dialogueEvent, statePath);
        }
    }
}
