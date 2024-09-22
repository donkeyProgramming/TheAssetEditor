using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.DataGridConfiguration;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioEditorViewModel
    {
        private void OnAudioProjectViewerDataGridChanged(object audioProjectViewerDataGrid, NotifyCollectionChangedEventArgs e)
        {
            SetIsPasteEnabled();
        }

        private void LoadActionEventSoundBankForAudioProjectViewer(SoundBank selectedSoundBank)
        {
            // Configure the DataGrids when necessary.
            if (selectedSoundBank.Name == "Movies" || _previousSelectedAudioProjectTreeItem == null)
                ConfigureAudioProjectViewerDataGridForActionEventSoundBank(this, _audioRepository, selectedSoundBank, _dataGridNameName, AudioProjectViewerDataGrid);

            else if (_previousSelectedAudioProjectTreeItem is not SoundBank)
                ConfigureAudioProjectViewerDataGridForActionEventSoundBank(this, _audioRepository, selectedSoundBank, _dataGridNameName, AudioProjectViewerDataGrid);

            else if (_previousSelectedAudioProjectTreeItem is SoundBank previousSelectedSoundBank)
            {
                if (previousSelectedSoundBank.Type != SoundBankType.ActionEventBnk.ToString())
                    ConfigureAudioProjectViewerDataGridForActionEventSoundBank(this, _audioRepository, selectedSoundBank, _dataGridNameName, AudioProjectViewerDataGrid);
            }


            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectViewerDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectViewerDataGridToActionEventSoundBank(AudioProjectViewerDataGrid, selectedSoundBank);

            _logger.Here().Information($"Loaded Action Event SoundBank: {selectedSoundBank.Name}");
        }

        private void LoadDialogueEventForAudioProjectViewer(DialogueEvent selectedDialogueEvent, bool showModdedStatesOnly, bool areStateGroupsEqual = false)
        {
            var dialogueEvent = _selectedAudioProjectTreeItem as DialogueEvent;

            // Configure the DataGrids when necessary.
            if (showModdedStatesOnly == true || areStateGroupsEqual == false || _previousSelectedAudioProjectTreeItem == null)
                ConfigureAudioProjectViewerDataGridForDialogueEvent(this, _audioRepository, dialogueEvent, _dataGridNameName, AudioProjectViewerDataGrid);

            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectViewerDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectViewerDataGridToDialogueEvent(AudioProjectViewerDataGrid, dialogueEvent);
        }

        private void LoadStateGroupForAudioProjectViewer(StateGroup selectedStateGroup, string stateGroupWithExtraUnderscores)
        {
            // Configure the DataGrids.
            ConfigureAudioProjectViewerDataGridForModdedStates(_dataGridNameName, AudioProjectViewerDataGrid, stateGroupWithExtraUnderscores);

            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectViewerDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectViewerDataGridToModdedStateGroup(AudioProjectViewerDataGrid, selectedStateGroup);
        }

        public static void SetAudioProjectViewerDataGridToModdedStateGroup(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, StateGroup stateGroup)
        {
            foreach (var state in stateGroup.States)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow[AddExtraUnderscoresToString(stateGroup.Name)] = state.Name;

                audioProjectViewerDataGrid.Add(dataGridRow);
            }
        }

        public static void SetAudioProjectViewerDataGridToActionEventSoundBank(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, SoundBank audioProjectItem)
        {
            foreach (var soundBankEvent in audioProjectItem.ActionEvents)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow["Event"] = soundBankEvent.Name;
                dataGridRow["AudioFiles"] = soundBankEvent.AudioFiles;
                dataGridRow["AudioFilesDisplay"] = soundBankEvent.AudioFilesDisplay;

                audioProjectViewerDataGrid.Add(dataGridRow);
            }
        }

        public static void SetAudioProjectViewerDataGridToDialogueEvent(ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, DialogueEvent dialogueEvent)
        {
            foreach (var decisionNode in dialogueEvent.DecisionTree)
            {
                var dataGridRow = new Dictionary<string, object>();
                dataGridRow["AudioFiles"] = decisionNode.AudioFiles;
                dataGridRow["AudioFilesDisplay"] = decisionNode.AudioFilesDisplay;

                var stateGroupsWithQualifiersList = DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name].ToList();

                foreach (var (node, kvp) in decisionNode.StatePath.Nodes.Zip(stateGroupsWithQualifiersList, (node, kvp) => (node, kvp)))
                {
                    var stateGroupfromDialogueEvent = node.StateGroup.Name;
                    var stateFromDialogueEvent = node.State.Name;

                    var stateGroupWithQualifierKey = kvp.Key;
                    var stateGroup = kvp.Value;

                    if (stateGroupfromDialogueEvent == stateGroup)
                        dataGridRow[AddExtraUnderscoresToString(stateGroupWithQualifierKey)] = stateFromDialogueEvent;
                }

                audioProjectViewerDataGrid.Add(dataGridRow);
            }
        }

        [RelayCommand] public void CopyRows()
        {
            if (_selectedAudioProjectTreeItem is DialogueEvent)
            {
                // Initialise new data rather than setting it directly so it's not referencing the original object which for example gets cleared when a new Event is selected.
                CopiedDataGridRows = new ObservableCollection<Dictionary<string, object>>();

                foreach (var item in SelectedDataGridRows)
                    CopiedDataGridRows.Add(new Dictionary<string, object>(item));

                SetIsPasteEnabled();
            }
        }

        [RelayCommand] public void PasteRows()
        {
            if (IsPasteEnabled && _selectedAudioProjectTreeItem is DialogueEvent selectedDalogueEvent)
            {
                foreach (var copiedDataGridRow in CopiedDataGridRows)
                {
                    AudioProjectViewerDataGrid.Add(copiedDataGridRow);
                    AddDataGridRowToDialogueEvent(copiedDataGridRow, selectedDalogueEvent);
                }

                SetIsPasteEnabled();
            }
        }

        private void SetIsPasteEnabled()
        {
            if (CopiedDataGridRows.Count == 0)
                return;

            var areAnyCopiedRowsInDataGrid = CopiedDataGridRows.Any(copiedRow => AudioProjectViewerDataGrid.Any(dataGridRow => copiedRow.Count == dataGridRow.Count && !copiedRow.Except(dataGridRow).Any()));

            if (_selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[selectedDialogueEvent.Name];

                var dialogueEventStateGroups = new List<string>();

                foreach (var kvp in stateGroupsWithQualifiers)
                {
                    var stateGroupWithQualifier = AddExtraUnderscoresToString(kvp.Key);
                    dialogueEventStateGroups.Add(stateGroupWithQualifier);
                }

                var copiedDataGridRowStateGroups = new List<string>();

                foreach (var kvp in CopiedDataGridRows[0])
                {
                    if (kvp.Key != "AudioFiles" && kvp.Key != "AudioFilesDisplay")
                        copiedDataGridRowStateGroups.Add(kvp.Key);
                }

                var areStateGroupsEqual = dialogueEventStateGroups.SequenceEqual(copiedDataGridRowStateGroups);

                if (!areStateGroupsEqual || areAnyCopiedRowsInDataGrid)
                    IsPasteEnabled = false;

                else if (areStateGroupsEqual && !areAnyCopiedRowsInDataGrid)
                    IsPasteEnabled = true;
            }
        }
    }
}
