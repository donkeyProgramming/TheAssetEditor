using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Models;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddStateToAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.StateGroup;

        public AddStateToAudioProjectCommand(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void Execute(DataRow row)
        {
            var stateName = DataGridHelpers.GetStateNameFromRow(row);
            var state = State.Create(stateName);

            var stateGroupName = _audioEditorService.SelectedExplorerNode.Name;
            var stateGroup = _audioEditorService.AudioProject.GetStateGroup(stateGroupName);

            stateGroup.InsertAlphabetically(state);
        }
    }
}
