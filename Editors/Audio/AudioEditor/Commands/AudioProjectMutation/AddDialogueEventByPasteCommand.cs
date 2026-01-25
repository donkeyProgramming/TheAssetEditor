using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Shared.Ui.Common;
using HircSettings = Editors.Audio.Shared.AudioProject.Models.HircSettings;

namespace Editors.Audio.AudioEditor.Commands.AudioProjectMutation
{
    public class AddDialogueEventByPasteCommand(
        IAudioEditorStateService audioEditorStateService,
        IAudioRepository audioRepository,
        IDialogueEventService dialogueEventService) : IAudioProjectMutationUICommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IDialogueEventService _dialogueEventService = dialogueEventService;

        public MutationType Action => MutationType.AddByPaste;
        public AudioProjectTreeNodeType NodeType => AudioProjectTreeNodeType.DialogueEvent;

        public void Execute(DataRow row)
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var copiedFromAudioProjectExplorerNode = _audioEditorStateService.CopiedFromAudioProjectExplorerNode;

            HircSettings hircSettings = null;
            var audioFiles = new List<AudioFile>();

            var dialogueEventName = copiedFromAudioProjectExplorerNode.Name;
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEventName);
            var statePath = dialogueEvent.GetStatePath(statePathName);
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(copiedFromAudioProjectExplorerNode.Parent.Parent.Name);

            if (statePath.TargetHircTypeIsSound())
            {
                var sound = soundBank.GetSound(statePath.TargetHircId);
                hircSettings = sound.HircSettings;
                audioFiles.Add(audioProject.GetAudioFile(sound.SourceId));
            }
            else if (statePath.TargetHircTypeIsRandomSequenceContainer())
            {
                var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                hircSettings = randomSequenceContainer.HircSettings;
                audioFiles = audioProject.GetAudioFiles(soundBank, randomSequenceContainer);
            }

            var statePathList = new List<KeyValuePair<string, string>>();
            foreach (DataColumn dataColumn in row.Table.Columns)
            {
                var columnNameWithQualifier = WpfHelpers.DeduplicateUnderscores(dataColumn.ColumnName);
                var stateGroupName = TableHelpers.GetStateGroupFromStateGroupWithQualifier(_audioRepository, dialogueEventName, columnNameWithQualifier);
                var stateName = TableHelpers.GetValueFromRow(row, dataColumn.ColumnName);
                statePathList.Add(new KeyValuePair<string, string>(stateGroupName, stateName));
            }

            _dialogueEventService.AddStatePath(_audioEditorStateService.SelectedAudioProjectExplorerNode.Name, audioFiles, hircSettings, statePathList);
        }
    }
}
