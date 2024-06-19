using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public AudioEditorDataViewModel AudioEditor { get; }
        public AudioEditorSettingsViewModel AudioEditorSettings { get; }

        public AudioEditorViewModel(AudioEditorDataViewModel audioEditor, AudioEditorSettingsViewModel audioEditorSettings)
        {
            AudioEditor = audioEditor;
            AudioEditorSettings = audioEditorSettings;
        }

        public void Close() { }
        public bool Save() => true;
        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }
}
