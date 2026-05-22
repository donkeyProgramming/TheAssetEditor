using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectMutation
{
    public class AddActionEventCommand(IAudioEditorStateService audioEditorStateService, IActionEventService actionEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IActionEventService _actionEventService = actionEventService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventType;

        public void Execute(DataRow row)
        {
            var actionEventTypeName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var audioFiles = _audioEditorStateService.AudioFiles;
            var hircSettings = _audioEditorStateService.HircSettings;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(row);

            if (actionEventName.StartsWith("Play_"))
                _actionEventService.AddPlayActionEvent(actionEventTypeName, actionEventName, audioFiles, hircSettings);
            else if (actionEventName.StartsWith("Pause_") || actionEventName.StartsWith("Resume_") || actionEventName.StartsWith("Stop_"))
                _actionEventService.AddPauseResumeStopActionEvent(actionEventTypeName, actionEventName);
        }
    }
}
