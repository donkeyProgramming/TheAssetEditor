using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.AudioProjectEditor
{
    public class ButtonEnablement
    {
        public static void SetIsAddRowButtonEnabled(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;

            if (audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid.Count == 0)
                return;

            // Check if everything is filled in. Don't use AudioFiles as that's a list of files rather than a string and we're usding AudioFilesDisplay anyway which is the string representation
            var emptyColumns = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Value == string.Empty && kvp.Key != "AudioFiles").ToList();
            if (emptyColumns.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
            else
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;

            // Check if any of the State Groups are capable of being left empty i.e. if the State can be set to Any automatically by the AudioEditor
            var singleRowDataGridStringValueColumns = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Key != "AudioFiles").ToDictionary();
            foreach (var kvp in singleRowDataGridStringValueColumns)
            {
                var columnName = kvp.Key;
                var columnValue = kvp.Value;

                // It's not okay for the AudioFiles display to be empty...
                if (columnName == "AudioFilesDisplay" && columnValue == string.Empty)
                {
                    audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                    break;
                }

                if (audioEditorViewModel.AudioProjectExplorerViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent && columnName != "AudioFilesDisplay" && columnName != "AudioSettings")
                {
                    var stateGroup = audioRepository.GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, AudioProjectHelpers.RemoveExtraUnderscoresFromString(columnName));
                    var stateGroupsWithAnyState = audioRepository.StateGroupsWithStates
                        .Where(kvp => kvp.Value.Contains("Any"))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                        audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = true;
                    else
                    {
                        audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                        return;
                    }
                }
            }

            // Check if the row exists already
            var audioProjectEditorSingleRowDataGridNullValues = audioEditorViewModel.AudioProjectEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Value == null).ToList();
            if (audioProjectEditorSingleRowDataGridNullValues.Count == 0)
            {
                var singleRowDataGridRow = AudioProjectHelpers.ExtractRowFromSingleRowDataGrid(audioEditorViewModel, audioRepository);
                foreach (var dictionary in audioEditorViewModel.AudioProjectViewerViewModel.AudioProjectEditorFullDataGrid)
                {
                    // Filter out "AudioFiles" and "AudioFilesDisplay" kvps so we just get the State Group with Qualifier and State
                    var filteredRow = singleRowDataGridRow.Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay");
                    var filteredDictionary = dictionary.Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay");
                    if (filteredRow.SequenceEqual(filteredDictionary))
                        audioEditorViewModel.AudioProjectEditorViewModel.IsAddRowButtonEnabled = false;
                }
            }
        }

        public static void SetButtonEnablement(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IList selectedItems)
        {
            // Reset button enablement
            audioEditorViewModel.AudioProjectEditorViewModel.IsUpdateRowButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsRemoveRowButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsAddAudioFilesButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsPlayAudioButtonEnabled = false;
            audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;

            var selectedDataGridRows = audioEditorViewModel.AudioProjectViewerViewModel.SelectedDataGridRows;
            selectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, object>>())
                selectedDataGridRows.Add(item);

            if (selectedDataGridRows.Count == 1)
            {
                audioEditorViewModel.AudioProjectEditorViewModel.IsUpdateRowButtonEnabled = true;
                audioEditorViewModel.AudioProjectEditorViewModel.IsRemoveRowButtonEnabled = true;

                if (selectedDataGridRows[0].ContainsKey("AudioFilesDisplay"))
                    audioEditorViewModel.AudioProjectEditorViewModel.IsPlayAudioButtonEnabled = true;
            }
            else if (selectedDataGridRows.Count > 1)
                audioEditorViewModel.AudioProjectEditorViewModel.IsRemoveRowButtonEnabled = true;

            if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            if (audioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes.Count > 0)
                audioEditorViewModel.AudioProjectEditorViewModel.IsAddAudioFilesButtonEnabled = true;
        }
    }
}
