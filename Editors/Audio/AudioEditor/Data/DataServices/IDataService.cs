using Editors.Audio.Storage;
using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public interface IDataService
    {
        public abstract void ConfigureAudioProjectEditorDataGrid(DataServiceParameters parameters);
        public abstract void SetAudioProjectEditorDataGridData(DataServiceParameters parameters);
        public abstract void ConfigureAudioProjectViewerDataGrid(DataServiceParameters parameters);
        public abstract void SetAudioProjectViewerDataGridData(DataServiceParameters parameters);
        public abstract void AddAudioProjectEditorDataGridDataToAudioProject(DataServiceParameters parameters);
        public abstract void RemoveAudioProjectEditorDataGridDataFromAudioProject(DataServiceParameters parameters);
    }

    public class DataServiceParameters
    {
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public IAudioProjectService AudioProjectService { get; set; }
        public IAudioRepository AudioRepository { get; set; }
        public SoundBank SoundBank { get; set; }
        public DialogueEvent DialogueEvent { get; set; }
        public StateGroup StateGroup { get; set; }
        public Dictionary<string, string> AudioProjectEditorRow { get; set; }
    }
}
