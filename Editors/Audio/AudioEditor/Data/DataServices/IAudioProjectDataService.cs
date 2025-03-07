namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public interface IAudioProjectDataService
    {
        public void AddAudioProjectEditorDataGridDataToAudioProject(AudioEditorViewModel audioEditorViewModel);
        public void RemoveAudioProjectEditorDataGridDataFromAudioProject(AudioEditorViewModel audioEditorViewModel);
    }
}
