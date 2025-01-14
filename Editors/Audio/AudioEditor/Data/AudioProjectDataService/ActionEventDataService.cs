using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioSettingsEditor;

namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public class ActionEventDataService : IAudioProjectDataService
    {
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.EditableTextBox);
            dataGrid.Columns.Add(eventColumn);

            var audioFilesColumn = DataGridHelpers.CreateColumn(parameters, "Audio Files", columnWidth, DataGridColumnType.AudioFilesEditableTextBox);
            dataGrid.Columns.Add(audioFilesColumn);
        }

        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters)
        {
            var rowData = new Dictionary<string, object>
            {
                { "Event", string.Empty },
                { "AudioFiles", new List<string>() },
                { "AudioFilesDisplay", string.Empty },
                { "AudioSettings", new AudioSettings() }
            };
            parameters.AudioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(rowData);
        }

        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters)
        {
            var dataGrid = DataGridHelpers.InitialiseDataGrid(parameters.AudioEditorViewModel.AudioProjectEditorFullDataGridTag);

            var columnsCount = 2;
            var columnWidth = 1.0 / columnsCount;

            var eventColumn = DataGridHelpers.CreateColumn(parameters, "Event", columnWidth, DataGridColumnType.ReadOnlyTextBlock);
            dataGrid.Columns.Add(eventColumn);

            var audioFilesColumn = DataGridHelpers.CreateColumn(parameters, "Audio Files", columnWidth, DataGridColumnType.AudioFilesReadOnlyTextBlock);
            dataGrid.Columns.Add(audioFilesColumn);
        }

        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters)
        {
            foreach (var actionEvent in parameters.SoundBank.ActionEvents)
            {
                var rowData = new Dictionary<string, object>
                {
                    { "Event", actionEvent.Name },
                    { "AudioFiles", actionEvent.AudioFiles },
                    { "AudioFilesDisplay", actionEvent.AudioFilesDisplay },
                    { "AudioSettings", actionEvent.AudioSettings }
                };
                parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Add(rowData);
            }
        }

        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var actionEvent = new ActionEvent();

            if (parameters.AudioProjectEditorRow.TryGetValue("Event", out var eventName))
                actionEvent.Name = eventName.ToString();

            if (parameters.AudioProjectEditorRow.TryGetValue("AudioFiles", out var audioFiles))
            {
                var filePaths = audioFiles as List<string>;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                actionEvent.AudioFiles = filePaths;
                actionEvent.AudioFilesDisplay = fileNamesString;
            }

            if (parameters.AudioProjectEditorRow.TryGetValue("AudioSettings", out var audioSettings))
                actionEvent.AudioSettings = AudioSettingsEditorViewModel.BuildAudioSettings(parameters.AudioEditorViewModel.AudioSettingsViewModel);

            var soundBank = parameters.SoundBank;
            AudioProjectHelpers.InsertActionEventAlphabetically(soundBank, actionEvent);
        }

        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters)
        {
            var soundBank = parameters.AudioEditorViewModel._selectedAudioProjectTreeItem as SoundBank;

            // Create a copy to prevent an error where dataGridRows is modified while being iterated over
            var dataGridRowsCopy = parameters.AudioEditorViewModel.SelectedDataGridRows.ToList();
            foreach (var dataGridRow in dataGridRowsCopy)
            {
                var actionEvent = AudioProjectHelpers.GetMatchingActionEvent(parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid, dataGridRow, soundBank);
                soundBank.ActionEvents.Remove(actionEvent);
                parameters.AudioEditorViewModel.AudioProjectEditorFullDataGrid.Remove(dataGridRow);
            }
        }
    }
}
