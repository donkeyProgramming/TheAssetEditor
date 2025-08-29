using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveActionEventCommand(
        IAudioEditorStateService audioEditorStateService,
        IActionEventService actionEventService,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IActionEventService _actionEventService = actionEventService;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.ActionEventType;

        public void Execute(DataRow row)
        {
            var soundBankName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
            _actionEventService.RemoveActionEvent(soundBankName, actionEventName);

            _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(row));
        }
    }
}
