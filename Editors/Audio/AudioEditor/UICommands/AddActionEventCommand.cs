using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddActionEventCommand(IAudioEditorService audioEditorService, IActionEventService actionEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IActionEventService _actionEventService = actionEventService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventSoundBank;

        public void Execute(DataRow row)
        {
            var audioFiles = _audioEditorService.AudioFiles;
            var audioSettings = _audioEditorService.AudioSettings;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
            var soundBankName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            _actionEventService.AddActionEvent(actionEventName, audioFiles, audioSettings, soundBankName);
        }
    }
}
