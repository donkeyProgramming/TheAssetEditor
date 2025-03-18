namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGrid
{
    public interface IAudioProjectEditorDataGridService
    {
        public void LoadDataGrid();
        public void ConfigureDataGrid();
        public void SetDataGridData();
        public void ResetDataGridData() => SetDataGridData();
    }
}
