using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddActionEventCommand(IAudioEditorStateService audioEditorStateService, IActionEventService actionEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IActionEventService _actionEventService = actionEventService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventType;

        public void Execute(DataRow row)
        {
            var actionEventGroupName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var audioFiles = _audioEditorStateService.AudioFiles;
            var audioSettings = _audioEditorStateService.AudioSettings;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
            _actionEventService.AddActionEvent(actionEventGroupName, actionEventName, audioFiles, audioSettings);
        }
    }
}
