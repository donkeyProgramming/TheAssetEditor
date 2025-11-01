using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class RemoveStateCommand(
        IAudioEditorStateService audioEditorStateService,
        IStateService stateService,
        IEventHub eventHub) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IStateService _stateService = stateService;
        private readonly IEventHub _eventHub = eventHub;

        public MutationType Action => MutationType.Remove;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.StateGroup;

        public void Execute(DataRow row)
        {
            var stateGroupName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var stateName = TableHelpers.GetStateNameFromRow(row);
            _stateService.RemoveState(stateGroupName, stateName);
            _eventHub.Publish(new ViewerTableRowRemoveRequestedEvent(row));
        }
    }
}
