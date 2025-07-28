using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveActionEventCommand(
        IAudioEditorService audioEditorService,
        IActionEventService actionEventService,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IActionEventService _actionEventService = actionEventService;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectExplorerTreeNodeType NodeType => AudioProjectExplorerTreeNodeType.ActionEventSoundBank;

        public void Execute(DataRow row)
        {
            var soundBankName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var actionEventName = TableHelpers.GetActionEventNameFromRow(row);
            _actionEventService.RemoveActionEvent(soundBankName, actionEventName);

            _eventHub.Publish(new ViewerTableRowRemovedEvent(row));
        }
    }
}
