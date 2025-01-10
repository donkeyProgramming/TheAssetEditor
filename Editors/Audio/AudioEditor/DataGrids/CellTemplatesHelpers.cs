using System.Collections.Generic;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.ButtonEnablement;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public static class CellTemplatesHelpers
    {
        public static void OnAudioFilesButtonClicked(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, TextBox textBox, Dictionary<string, object> dataGridRowContext)
        {
            var result = AddAudioFilesToAudioProjectEditorSingleRowDataGrid(dataGridRowContext, textBox);
            if (result)
            {
                var audioFiles = dataGridRowContext["AudioFiles"] as List<string>;
                if (audioFiles.Count > 1)
                    audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = true;
                else
                    audioEditorViewModel.AudioSettingsViewModel.IsUsingMultipleAudioFiles = false;

                AudioSettingsViewModel.SetAudioSettingsEnablement(audioEditorViewModel.AudioSettingsViewModel);
                SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
            }
        }
    }
}
