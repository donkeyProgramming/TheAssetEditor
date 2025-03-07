namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridServices
{
    public interface IAudioProjectEditorDataGridService
    {
        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void ConfigureDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void SetDataGridData(AudioEditorViewModel audioEditorViewModel);
    }
}
