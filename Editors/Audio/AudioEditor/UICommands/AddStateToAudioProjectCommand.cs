using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;

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
            var state = AudioProjectHelpers.CreateStateFromRow(row);
            var stateGroup = AudioProjectHelpers.GetStateGroupFromName(_audioEditorService.AudioProject, _audioEditorService.SelectedExplorerNode.Name);
            AudioProjectHelpers.InsertStateAlphabetically(stateGroup, state);
        }
    }
}
