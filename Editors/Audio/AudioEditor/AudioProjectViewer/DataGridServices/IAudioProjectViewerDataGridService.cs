using Editors.Audio.AudioEditor.Data.DataServices;

namespace Editors.Audio.AudioEditor.AudioProjectViewer.DataGridServices
{
    public interface IAudioProjectViewerDataGridService
    {
        public void LoadDataGrid(AudioEditorViewModel audioEditorViewModel);
        public void ConfigureDataGrid(DataServiceParameters parameters);
        public void SetDataGridData(DataServiceParameters parameters);
    }
}
