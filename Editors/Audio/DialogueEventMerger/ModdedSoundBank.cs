using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.DialogueEventMerger
{
    public partial class ModdedSoundBank(string filePath, bool isChecked = true) : ObservableObject
    {
        public string FilePath { get; } = filePath;

        [ObservableProperty] private bool _isChecked = isChecked;

        public override string ToString() => FilePath;
    }
}
