namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public interface IAudioProjectDataService
    {
        public abstract void ConfigureAudioProjectEditorDataGrid(AudioProjectDataServiceParameters parameters);
        public abstract void SetAudioProjectEditorDataGridData(AudioProjectDataServiceParameters parameters);
        public abstract void ConfigureAudioProjectViewerDataGrid(AudioProjectDataServiceParameters parameters);
        public abstract void SetAudioProjectViewerDataGridData(AudioProjectDataServiceParameters parameters);
        public abstract void AddAudioProjectEditorDataGridDataToAudioProject(AudioProjectDataServiceParameters parameters);
        public abstract void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioProjectDataServiceParameters parameters);
    }
}
