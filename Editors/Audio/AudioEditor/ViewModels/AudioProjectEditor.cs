using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.DataGridConfiguration;
using static Editors.Audio.Utility.SoundPlayer;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioEditorViewModel
    {
        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (_selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                LoadDialogueEventForAudioProjectEditor(selectedDialogueEvent, ShowModdedStatesOnly);
                LoadDialogueEventForAudioProjectViewer(selectedDialogueEvent, ShowModdedStatesOnly);

                _logger.Here().Information($"Loaded DialogueEvent: {selectedDialogueEvent.Name}");
            }
        }

        private void LoadActionEventSoundBankForAudioProjectEditor(SoundBank selectedSoundBank)
        {
            // Configure the DataGrids when necessary.
            if (selectedSoundBank.Name == "Movies" || _previousSelectedAudioProjectTreeItem == null)
                ConfigureAudioProjectEditorDataGridForActionEventSoundBank(this, _audioRepository, _dataGridBuilderName, AudioProjectEditorDataGrid);

            else if (_previousSelectedAudioProjectTreeItem is not SoundBank)
                ConfigureAudioProjectEditorDataGridForActionEventSoundBank(this, _audioRepository, _dataGridBuilderName, AudioProjectEditorDataGrid);

            else if (_previousSelectedAudioProjectTreeItem is SoundBank previousSelectedSoundBank)
            {
                if (previousSelectedSoundBank.Type != SoundBankType.ActionEventBnk.ToString())
                    ConfigureAudioProjectEditorDataGridForActionEventSoundBank(this, _audioRepository, _dataGridBuilderName, AudioProjectEditorDataGrid);
            }


            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectEditorDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectEditorDataGridToActionEventSoundBank();

        }

        private void LoadDialogueEventForAudioProjectEditor(DialogueEvent selectedDialogueEvent, bool showModdedStatesOnly, bool areStateGroupsEqual = false)
        {
            var dialogueEvent = _selectedAudioProjectTreeItem as DialogueEvent;

            // Configure the DataGrids when necessary.
            if (showModdedStatesOnly == true || areStateGroupsEqual == false || _previousSelectedAudioProjectTreeItem == null)
                ConfigureAudioProjectEditorDataGridForDialogueEvent(this, _audioRepository, dialogueEvent, showModdedStatesOnly, _dataGridBuilderName, AudioProjectEditorDataGrid, _audioProjectService.StateGroupsWithCustomStates);

            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectEditorDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectEditorDataGridToDialogueEvent(dialogueEvent);
        }

        private void LoadStateGroupForAudioProjectEditor(StateGroup selectedStateGroup, string stateGroupWithExtraUnderscores)
        {
            // Configure the DataGrids.
            ConfigureAudioProjectEditorDataGridForModdedStates(_dataGridBuilderName, AudioProjectEditorDataGrid, stateGroupWithExtraUnderscores);

            // Clear the previous DataGrid Data.
            ClearDataGrid(AudioProjectEditorDataGrid);

            // Set the format of the DataGrids.
            SetAudioProjectEditorDataGridToModdedStateGroup(stateGroupWithExtraUnderscores);
        }

        private void SetAudioProjectEditorDataGridToModdedStateGroup(string moddedStateGroupWithExtraUnderscores)
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow[moddedStateGroupWithExtraUnderscores] = string.Empty;

            AudioProjectEditorDataGrid.Add(dataGridRow);
        }

        private void SetAudioProjectEditorDataGridToActionEventSoundBank()
        {
            var dataGridRow = new Dictionary<string, object> { };
            dataGridRow["Event"] = string.Empty;
            dataGridRow["AudioFiles"] = new List<string> { };
            dataGridRow["AudioFilesDisplay"] = string.Empty;

            AudioProjectEditorDataGrid.Add(dataGridRow);
        }

        private void SetAudioProjectEditorDataGridToDialogueEvent(DialogueEvent dialogueEvent)
        {
            var dataGridRow = new Dictionary<string, object>();

            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name];

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var columnHeader = AddExtraUnderscoresToString(stateGroupWithQualifier);
                dataGridRow[columnHeader] = "";
            }

            dataGridRow["AudioFiles"] = new List<string> { };
            dataGridRow["AudioFilesDisplay"] = string.Empty;

            AudioProjectEditorDataGrid.Add(dataGridRow);
        }

        [RelayCommand] public void AddDataGridRowFromAudioProjectEditorToAudioProjectViewer()
        {
            if (AudioProjectEditorDataGrid.Count == 0)
                return;

            var newRow = new Dictionary<string, object>();

            foreach (var kvp in AudioProjectEditorDataGrid[0])
            {
                var column = kvp.Key;
                var cellValue = kvp.Value;

                if (column == "AudioFiles" && cellValue is List<string> stringList)
                {
                    var newList = new List<string>(stringList);
                    newRow[column] = newList;
                }

                else
                    newRow[column] = cellValue.ToString();
            }

            InsertDataGridRowAlphabetically(AudioProjectViewerDataGrid, newRow);

            ClearDataGrid(AudioProjectEditorDataGrid);

            if (_selectedAudioProjectTreeItem is SoundBank soundBank)
            {
                if (soundBank.ActionEvents != null)
                {
                    SetAudioProjectEditorDataGridToActionEventSoundBank();
                    AddDataGridRowToActionEventSoundBank(newRow, soundBank);
                }

                else if (soundBank.MusicEvents != null)
                {
                    throw new NotImplementedException();
                }
            }

            else if (_selectedAudioProjectTreeItem is DialogueEvent selectedDalogueEvent)
            {
                SetAudioProjectEditorDataGridToDialogueEvent(selectedDalogueEvent);
                AddDataGridRowToDialogueEvent(newRow, selectedDalogueEvent);
            }

            else if (_selectedAudioProjectTreeItem is StateGroup moddedStateGroup)
            {
                SetAudioProjectEditorDataGridToModdedStateGroup(moddedStateGroup.Name);
                AddDataGridRowToModdedStates(newRow, moddedStateGroup);
            }

        }

        [RelayCommand] public void RemoveDataGridRowFromAudioProjectViewer()
        {
            if (SelectedDataGridRows.Count == 1)
            {
                if (_selectedAudioProjectTreeItem is SoundBank selectedActionEventSoundBank)
                {
                    throw new NotImplementedException();
                }

                else if (_selectedAudioProjectTreeItem is DialogueEvent selectedDalogueEvent)
                {
                    RemoveDataGridRowFromDialogueEvent(AudioProjectViewerDataGrid, SelectedDataGridRows[0], selectedDalogueEvent);
                }
            }
        }

        [RelayCommand] public void PlayRandomAudioFile()
        {
            if (SelectedDataGridRows.Count == 1)
            {
                if (SelectedDataGridRows[0].TryGetValue("AudioFiles", out var audioFilesObj) && audioFilesObj is List<string> audioFiles && audioFiles.Any())
                {
                    var random = new Random();
                    var randomIndex = random.Next(audioFiles.Count);
                    var randomAudioFile = audioFiles[randomIndex];

                    PlaySound(randomAudioFile);
                }
            }
        }

        public static void AddAudioFiles(Dictionary<string, object> dataGridRow, TextBox textBox)
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = "WAV files (*.wav)|*.wav"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePaths = dialog.FileNames;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);
                var filePathsString = string.Join(", ", filePaths.Select(filePath => $"\"{filePath}\""));

                textBox.Text = fileNamesString;
                textBox.ToolTip = filePathsString;

                var audioFiles = new List<string>(filePaths);

                dataGridRow["AudioFiles"] = audioFiles;
                dataGridRow["AudioFilesDisplay"] = fileNamesString;
            }
        }
    }
}
