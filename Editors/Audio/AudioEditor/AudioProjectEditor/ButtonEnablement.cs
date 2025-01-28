using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public class ButtonEnablement
    {
        public static void SetAddRowButtonEnablement(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.ResetAddRowButtonEnablement();

            if (audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid.Count == 0)
                return;

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType != NodeType.StateGroup)
            {
                var audioFilesDisplayCheckResult = CheckAudioFilesDisplay(audioEditorViewModel);
                if (audioFilesDisplayCheckResult == false)
                    return;
            }

            var rowExistsCheckResult = CheckIfAudioProjectViewerRowExists(audioEditorViewModel, audioRepository, audioProjectService);
            if (rowExistsCheckResult == false)
                return;

            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                var dialogueEvent = AudioProjectHelpers.GetDialogueEventFromName(audioProjectService, audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.Name);
                var statesCheckResult = CheckIfStatesCanBeLeftEmpty(audioEditorViewModel, audioRepository, dialogueEvent);
                if (statesCheckResult == false)
                    return;
            }
            else
            {
                var columnsFilledInCheck = CheckIfAllColumnsAreFilledIn(audioEditorViewModel);
                if (columnsFilledInCheck == false)
                return;
            }
        }

        private static bool CheckIfAllColumnsAreFilledIn(AudioEditorViewModel audioEditorViewModel)
        {
            var columnsToFilterOut = new HashSet<string> { "AudioFilesDisplay", "AudioFiles", "AudioSettings" };

            var filteredColumns = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0]
                .Where(kvp => !columnsToFilterOut.Contains(kvp.Key));

            var emptyColumns = filteredColumns.Where(kvp => kvp.Value is string value && string.IsNullOrEmpty(value)).ToList();

            if (emptyColumns.Count > 0)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return false;
            }

            audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;
            return true;
        }

        private static bool CheckIfAudioProjectViewerRowExists(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            var columnsToFilterOut = new HashSet<string> { "AudioFilesDisplay", "AudioFiles", "AudioSettings" };
            var audioProjectEditorData = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository, audioProjectService);

            var filteredSingleRow = audioProjectEditorData
                .Where(kvp => !columnsToFilterOut.Contains(kvp.Key))
                .ToList();

            var rowExists = audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectViewerDataGrid
                .Any(dictionary => filteredSingleRow.SequenceEqual(dictionary
                .Where(kvp => !columnsToFilterOut.Contains(kvp.Key))));

            if (rowExists)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return false;
            }

            return true;
        }

        private static bool CheckIfStatesCanBeLeftEmpty(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, DialogueEvent selectedDialogueEvent)
        {
            var columnsToFilterOut = new HashSet<string> { "AudioFilesDisplay", "AudioFiles", "AudioSettings" };

            var filteredColumns = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0]
                .Where(kvp => !columnsToFilterOut.Contains(kvp.Key));

            foreach (var kvp in filteredColumns)
            {
                var columnName = kvp.Key;
                var columnValue = kvp.Value;

                var stateGroup = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, AudioProjectHelpers.RemoveExtraUnderscoresFromString(columnName));
                var stateGroupsWithAnyState = audioRepository.StateGroupsWithStates
                    .Where(kvp => kvp.Value.Contains("Any"))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                    audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;
                else
                {
                    audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                    return false;
                }
            }

            return true;
        }

        private static bool CheckAudioFilesDisplay(AudioEditorViewModel audioEditorViewModel)
        {
            var audioFilesDisplayEntry = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorDataGrid[0]
                .FirstOrDefault(kvp => kvp.Key == "AudioFilesDisplay");

            if (string.IsNullOrEmpty(audioFilesDisplayEntry.Value.ToString()))
            {
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                return false;
            }

            return true;
        }

        public static void SetShowModdedStatesOnlyButtonEnablementAndVisibility(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeNode.NodeType == NodeType.DialogueEvent)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = true;

                if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                    audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;
                else if (audioProjectService.StateGroupsWithModdedStatesRepository.Count == 0)
                    audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;
            }
            else
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxVisible = false;
        }
    }
}
