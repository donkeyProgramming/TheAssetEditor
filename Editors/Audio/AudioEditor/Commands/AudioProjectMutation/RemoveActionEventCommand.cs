using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectMutation
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

        private DataRow _row = null!;

        public void Configure(DataRow row)
        {
            _row = row;
        }

        public void Execute()
        {
            var soundBankName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(_row);
            _actionEventService.RemoveActionEvent(soundBankName, actionEventName);
            _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(_row));
        }
    }
}
