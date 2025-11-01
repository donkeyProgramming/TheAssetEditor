using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;

namespace Editors.Audio.AudioEditor.Commands
{
    public class AddStateCommand(IAudioEditorStateService audioEditorStateService, IStateService stateService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IStateService _stateService = stateService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.StateGroup;

        public void Execute(DataRow row)
        {
            var stateName = TableHelpers.GetStateNameFromRow(row);
            var stateGroupName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            _stateService.AddState(stateGroupName, stateName);
        }
    }
}
