using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddActionEventToAudioProjectCommand : IAudioProjectUICommand
    {
        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.ActionEventSoundBank;

        private readonly IAudioEditorService _audioEditorService;

        public AddActionEventToAudioProjectCommand(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void Execute(DataRow row)
        {
            var actionEvent = AudioProjectHelpers.CreateActionEventFromRow(_audioEditorService.AudioFiles, _audioEditorService.AudioSettings, row);
            var soundBank = AudioProjectHelpers.GetSoundBankFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }
    }
}
