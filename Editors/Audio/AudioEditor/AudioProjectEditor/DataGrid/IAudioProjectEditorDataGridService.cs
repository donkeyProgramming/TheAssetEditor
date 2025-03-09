namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public interface IAudioProjectEditorDataGridService
    {
        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel);
        public void ResetDataGridData(AudioEditorViewModel audioEditorViewModel) => SetDataGridData(audioEditorViewModel);
    }
}
