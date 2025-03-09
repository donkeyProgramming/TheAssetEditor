namespace Editors.Audio.AudioEditor.AudioProjectData
{
    public interface IAudioProjectDataService
    {
        public void AddToAudioProject(AudioEditorViewModel audioEditorViewModel);
        public void RemoveFromAudioProject(AudioEditorViewModel audioEditorViewModel);
    }
}
