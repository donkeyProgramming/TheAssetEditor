using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly PackFileService _pfs;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        public NotifyAttr<int> ExampleVariable { get; set; } = new NotifyAttr<int>(3);

        public AudioEditorViewModel(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public void ExampleAction()
        {
            ExampleVariable.Value++;
            HasUnsavedChanges = true;
        }

        public void Close() { }
        public bool Save() => true;
        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }
}
