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
            HircSettings hircSettings = null;
            var audioFiles = new List<AudioFile>();

            var dialogueEventName = _audioEditorStateService.CopiedFromAudioProjectExplorerNode.Name;
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePathName = TableHelpers.GetStatePathNameFromRow(row, _audioRepository, dialogueEventName);
            var statePath = dialogueEvent.GetStatePath(statePathName);
            var soundBankName = _audioEditorStateService.CopiedFromAudioProjectExplorerNode.GetParentSoundBankNode().Name;
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            if (statePath.TargetHircTypeIsSound())
            {
                var sound = soundBank.GetSound(statePath.TargetHircId);
                hircSettings = sound.HircSettings;
                audioFiles.Add(_audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId));
            }
            else if (statePath.TargetHircTypeIsRandomSequenceContainer())
            {
                var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                hircSettings = randomSequenceContainer.HircSettings;
                audioFiles = _audioEditorStateService.AudioProject.GetAudioFiles(soundBank, randomSequenceContainer);
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
