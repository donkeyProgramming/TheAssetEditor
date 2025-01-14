using System;
using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.DataGrids.AudioProjectDataService;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public interface IAudioProjectDataService
    {
        void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters);
        void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters);
        void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters);
        void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters);
        void AddAudioProjectEditorDataGridDataToAudioProject (AudioProjectDataServiceParameters parameters);
        void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters);
    }

    public class AudioProjectDataService : IAudioProjectDataService
    {
        public static class AudioProjectDataServiceFactory
        {
            public static IAudioProjectDataService GetService(object selectedItem)
            {
                return selectedItem switch
                {
                    SoundBank => new ActionEventDataService(),
                    DialogueEvent => new DialogueEventDataService(),
                    StateGroup => new StatesDataService(),
                    _ => throw new NotSupportedException("Unsupported tree item type")
                };
            }
        }

        public class AudioProjectDataServiceParameters
        {
            public AudioEditorViewModel AudioEditorViewModel { get; set; }
            public IAudioProjectService AudioProjectService { get; set; }
            public IAudioRepository AudioRepository { get; set; }
            public SoundBank SoundBank { get; set; }
            public DialogueEvent DialogueEvent { get; set; }
            public StateGroup StateGroup { get; set; }
            public Dictionary<string, object> AudioProjectEditorRow { get; set; }
        }

        public static void AddAudioProjectViewerDataGridDataToAudioProjectEditor(AudioEditorViewModel audioEditorViewModel)
        {
            audioEditorViewModel.AudioProjectEditorSingleRowDataGrid.Add(audioEditorViewModel.SelectedDataGridRows[0]);
        }

        public static void AddAudioProjectEditorDataGridDataToAudioProjectViewerDataGrid(AudioEditorViewModel audioEditorViewModel, Dictionary<string, object> audioProjectEditorRow)
        {
            InsertDataGridRowAlphabetically(audioEditorViewModel.AudioProjectEditorFullDataGrid, audioProjectEditorRow);
        }

        // Not required
        public void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters) { }
        public void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters) { }
        public void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters) { }
        public void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters) { }
        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters) { }
        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters) { }
    }
}
