using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;

namespace Editors.Audio.AudioEditor
{
    public class ButtonEnablement
    {
        public static void SetIsAddRowButtonEnabled(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            audioEditorViewModel.IsAddRowButtonEnabled = false;

            if (audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Count == 0)
                return;

            // Check if everything is filled in. Don't use AudioFiles as that's a list of files rather than a string and we're usding AudioFilesDisplay anyway which is the string representation
            var emptyColumns = audioEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Value == string.Empty && kvp.Key != "AudioFiles").ToList();
            if (emptyColumns.Count > 0)
                audioEditorViewModel.IsAddRowButtonEnabled = false;
            else
                audioEditorViewModel.IsAddRowButtonEnabled = true;

            // Check if any of the State Groups are capable of being left empty i.e. if the State can be set to Any automatically by the AudioEditor
            var singleRowDataGridStringValueColumns = audioEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Key != "AudioFiles").ToDictionary();
            foreach (var kvp in singleRowDataGridStringValueColumns)
            {
                var columnName = kvp.Key;
                var columnValue = kvp.Value;

                // It's not okay for the AudioFiles display to be empty...
                if (columnName == "AudioFilesDisplay" && columnValue == string.Empty)
                {
                    audioEditorViewModel.IsAddRowButtonEnabled = false;
                    break;
                }

                if (audioEditorViewModel._selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent && (columnName != "AudioFilesDisplay" && columnName != "AudioSettings"))
                {
                    var stateGroup = GetStateGroupFromStateGroupWithQualifier(selectedDialogueEvent.Name, RemoveExtraUnderscoresFromString(columnName), audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
                    var stateGroupsWithAnyState = audioRepository.StateGroupsWithStates
                        .Where(kvp => kvp.Value.Contains("Any"))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (stateGroupsWithAnyState.ContainsKey(stateGroup))
                        audioEditorViewModel.IsAddRowButtonEnabled = true;
                    else
                    {
                        audioEditorViewModel.IsAddRowButtonEnabled = false;
                        return;
                    }
                }
            }

            // Check if the row exists already
            var audioProjectEditorSingleRowDataGridNullValues = audioEditorViewModel.AudioProjectEditorSingleRowDataGrid[0].Where(kvp => kvp.Value == null).ToList();
            if (audioProjectEditorSingleRowDataGridNullValues.Count == 0)
            {
                var singleRowDataGridRow = ExtractRowFromSingleRowDataGrid(audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, audioRepository.StateGroupsWithStates, audioEditorViewModel.AudioProjectEditorSingleRowDataGrid, audioEditorViewModel._selectedAudioProjectTreeItem);
                foreach (var dictionary in audioEditorViewModel.AudioProjectEditorFullDataGrid)
                {
                    // Filter out "AudioFiles" and "AudioFilesDisplay" kvps so we just get the State Group with Qualifier and State
                    var filteredRow = singleRowDataGridRow.Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay");
                    var filteredDictionary = dictionary.Where(kvp => kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay");
                    if (filteredRow.SequenceEqual(filteredDictionary))
                        audioEditorViewModel.IsAddRowButtonEnabled = false;
                }
            }
        }

        public static void SetButtonEnablement(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IList selectedItems)
        {
            // Reset button enablement
            audioEditorViewModel.IsUpdateRowButtonEnabled = false;
            audioEditorViewModel.IsRemoveRowButtonEnabled = false;
            audioEditorViewModel.IsAddAudioFilesButtonEnabled = false;
            audioEditorViewModel.IsPlayAudioButtonEnabled = false;
            audioEditorViewModel.IsShowModdedStatesCheckBoxEnabled = false;

            var selectedDataGridRows = audioEditorViewModel.SelectedDataGridRows;
            selectedDataGridRows.Clear();

            foreach (var item in selectedItems.OfType<Dictionary<string, object>>())
                selectedDataGridRows.Add(item);

            if (selectedDataGridRows.Count == 1)
            {
                audioEditorViewModel.IsUpdateRowButtonEnabled = true;
                audioEditorViewModel.IsRemoveRowButtonEnabled = true;

                if (selectedDataGridRows[0].ContainsKey("AudioFilesDisplay"))
                    audioEditorViewModel.IsPlayAudioButtonEnabled = true;
            }
            else if (selectedDataGridRows.Count > 1)
                audioEditorViewModel.IsRemoveRowButtonEnabled = true;

            if (audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                audioEditorViewModel.IsShowModdedStatesCheckBoxEnabled = true;

            if (audioEditorViewModel.AudioFilesExplorerViewModel.SelectedTreeNodes.Count > 0)
                audioEditorViewModel.IsAddAudioFilesButtonEnabled = true;
        }
    }
}
