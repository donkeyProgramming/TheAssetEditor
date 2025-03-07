namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public interface IAudioProjectViewerDataGridService
    {
        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel);
    }
}
