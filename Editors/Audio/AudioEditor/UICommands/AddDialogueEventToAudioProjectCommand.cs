using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class AddDialogueEventToAudioProjectCommand : IAudioProjectUICommand
    {
        private readonly IAudioEditorService _audioEditorService;  
        private readonly IAudioRepository _audioRepository;
        private readonly IStatePathFactory _statePathFactory;

        public AudioProjectCommandAction Action => AudioProjectCommandAction.AddToAudioProject;
        public NodeType NodeType => NodeType.DialogueEvent;

        public AddDialogueEventToAudioProjectCommand(IAudioEditorService audioEditorService, IAudioRepository audioRepository, IStatePathFactory statePathFactory)
        {
            _audioEditorService = audioEditorService;
            _audioRepository = audioRepository;
            _statePathFactory = statePathFactory;
        }

        public void Execute(DataRow row)
        {
            var audioFiles = _audioEditorService.AudioFiles;
            var audioSettings = _audioEditorService.AudioSettings;
            var dialogueEventName = _audioEditorService.SelectedExplorerNode.Name;

            var stateLookupByStateGroup = new Dictionary<string, string>();
            var stateGroupsWithQualifiers = _audioRepository.QualifiedStateGroupLookupByStateGroupByDialogueEvent[dialogueEventName];
            foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
            {
                var stateGroupName = DataGridHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, stateGroupWithQualifier.Value);
                var stateGroupNameWithDuplicatedUnderscores = DataGridHelpers.DuplicateUnderscores(stateGroupName);
                var stateName = DataGridHelpers.GetValueFromRow(row, stateGroupNameWithDuplicatedUnderscores);
                stateLookupByStateGroup.Add(stateName, stateGroupName);
            }

            var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePath = _statePathFactory.Create(stateLookupByStateGroup, audioFiles, audioSettings);
            dialogueEvent.InsertAlphabetically(statePath);
        }
    }
}
