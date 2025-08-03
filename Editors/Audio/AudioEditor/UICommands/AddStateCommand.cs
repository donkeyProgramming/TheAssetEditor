using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddStateCommand(IAudioEditorService audioEditorService, IStateService stateService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IStateService _stateService = stateService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.StateGroup;

        public void Execute(DataRow row)
        {
            var stateName = TableHelpers.GetStateNameFromRow(row);
            var stateGroupName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            _stateService.AddState(stateGroupName, stateName);
        }
    }
}
