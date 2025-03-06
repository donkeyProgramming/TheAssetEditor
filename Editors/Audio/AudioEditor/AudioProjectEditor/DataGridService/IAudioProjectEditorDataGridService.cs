using Editors.Audio.AudioEditor.Data.DataServices;

namespace Editors.Audio.AudioEditor.AudioProjectEditor.DataGridService
{
    public interface IAudioProjectEditorDataGridService
    {
        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void ConfigureDataGrid(DataServiceParameters parameters);
        public void SetDataGridData(DataServiceParameters parameters);
    }
}
