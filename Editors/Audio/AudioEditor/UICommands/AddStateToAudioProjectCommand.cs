using System.Data;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddStateToAudioProjectCommand : IAudioProjectUICommand
    {
        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.StateGroup;

        private readonly IAudioEditorService _audioEditorService;

        public AddStateToAudioProjectCommand(IAudioEditorService audioEditorService)
        {
            _audioEditorService = audioEditorService;
        }

        public void Execute(DataRow row)
        {
            var state = AudioProjectHelpers.CreateStateFromDataGridRow(row);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            AudioProjectHelpers.InsertStateAlphabetically(stateGroup, state);
        }
    }
}
