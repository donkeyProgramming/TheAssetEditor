using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    public partial class AudioFile : ObservableObject
    {
        [ObservableProperty] public string _fileName;
        [ObservableProperty] public string _filePath;
    }
}
