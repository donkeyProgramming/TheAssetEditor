using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.AudioEditor.Services;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddDialogueEventCommand(
        IAudioEditorService audioEditorService,
        IAudioRepository audioRepository,
        IDialogueEventService dialogueEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IDialogueEventService _dialogueEventService = dialogueEventService;

        public MutationType Action => MutationType.Add;
        public AudioProjectExplorerTreeNodeType NodeType => AudioProjectExplorerTreeNodeType.DialogueEvent;

        public void Execute(DataRow row)
        {
            var dialogueEventName = _audioEditorService.SelectedAudioProjectExplorerNode.Name;
            var audioFiles = _audioEditorService.AudioFiles;
            var audioSettings = _audioEditorService.AudioSettings;

            var stateLookupByStateGroup = new Dictionary<string, string>();
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEventName];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupName = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, stateGroupWithQualifier.Key);
                var columnName = TableHelpers.DuplicateUnderscores(stateGroupWithQualifier.Key);
                var stateName = TableHelpers.GetValueFromRow(row, columnName);
                stateLookupByStateGroup.Add(stateGroupName, stateName);
            }

            _dialogueEventService.AddStatePath(dialogueEventName, audioFiles, audioSettings, stateLookupByStateGroup);
        }
    }
}
