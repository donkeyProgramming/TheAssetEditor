using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveStateCommand(
        IAudioEditorService audioEditorService,
        IStateService stateService,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IStateService _stateService = stateService;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectExplorerTreeNodeType NodeType => AudioProjectExplorerTreeNodeType.StateGroup;

        public void Execute(DataRow row)
        {
            var stateGroupName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var stateName = TableHelpers.GetStateNameFromRow(row);
            _stateService.RemoveState(stateGroupName, stateName);
            _eventHub.Publish(new ViewerTableRowRemovedEvent(row));
        }
    }
}
