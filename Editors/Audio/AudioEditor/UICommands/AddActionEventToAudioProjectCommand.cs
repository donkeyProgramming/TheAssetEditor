using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Factories;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddActionEventToAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IActionEventFactory _actionEventFactory;

        public AddActionEventToAudioProjectCommand(IAudioEditorService audioEditorService, IActionEventFactory actionEventFactory)
        {
            _audioEditorService = audioEditorService;
            _actionEventFactory = actionEventFactory;
        }

        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.ActionEventSoundBank;

        public void Execute(DataRow row)
        {
            var audioFiles = _audioEditorService.AudioFiles;
            var audioSettings = _audioEditorService.AudioSettings;
            var actionEventName = DataGridHelpers.GetActionEventNameFromRow(row);

            var actionEvent = _actionEventFactory.Create(actionEventName, audioFiles, audioSettings);
            
            var soundBankName = _audioEditorService.SelectedExplorerNode.Name;
            var soundBank = _audioEditorService.AudioProject.GetSoundBank(soundBankName);
            soundBank.InsertAlphabetically(actionEvent);
        }
    }
}
