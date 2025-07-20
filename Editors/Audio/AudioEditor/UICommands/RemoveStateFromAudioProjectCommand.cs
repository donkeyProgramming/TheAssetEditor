using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class RemoveStateFromAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;
        private readonly IEventHub _eventHub;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.RemoveFromAudioProject;
        public NodeType NodeType => NodeType.StateGroup;

        public RemoveStateFromAudioProjectCommand(IAudioEditorService audioEditorService, IEventHub eventHub)
        {
            _audioEditorService = audioEditorService;
            _eventHub = eventHub;
        }

        public void Execute(DataRow row)
        {
            var stateGroupName = _audioEditorService.SelectedExplorerNode.Name;
            var stateGroup = _audioEditorService.AudioProject.GetStateGroup(stateGroupName);

            var stateName = DataGridHelpers.GetStateNameFromRow(row);
            var state = stateGroup.GetState(stateName);

            stateGroup.States.Remove(state);
            _eventHub.Publish(new RemoveViewerTableRowEvent(row));
        }
    }
}
