using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectMutation
{
    public class AddStateCommand(IAudioEditorStateService audioEditorStateService, IStateService stateService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IStateService _stateService = stateService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.StateGroup;

        private DataRow _row = null!;

        public void Configure(DataRow row)
        {
            _row = row;
        }

        public void Execute()
        {
            var stateName = TableHelpers.GetStateNameFromRow(_row);
            var stateGroupName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            _stateService.AddState(stateGroupName, stateName);
        }
    }
}
