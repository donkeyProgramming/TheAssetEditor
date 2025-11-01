using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.Storage;

namespace Editors.Audio.AudioEditor.Commands
{
    public class AddDialogueEventCommand(
        IAudioEditorStateService audioEditorStateService,
        IAudioRepository audioRepository,
        IDialogueEventService dialogueEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IDialogueEventService _dialogueEventService = dialogueEventService;

        public MutationType Action => MutationType.Add;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.DialogueEvent;

        public void Execute(DataRow row)
        {
            var dialogueEventName = _audioEditorStateService.SelectedAudioProjectExplorerNode.Name;
            var audioFiles = _audioEditorStateService.AudioFiles;
            var settings = _audioEditorStateService.HircSettings;

            var stateLookupByStateGroup = new Dictionary<string, string>();
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupByStateGroupByDialogueEvent[dialogueEventName];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupName = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, stateGroupWithQualifier.Key);
                var columnName = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                var stateName = TableHelpers.GetValueFromRow(row, columnName);
                stateLookupByStateGroup.Add(stateGroupName, stateName);
            }

            _dialogueEventService.AddStatePath(dialogueEventName, audioFiles, settings, stateLookupByStateGroup);
        }
    }
}
