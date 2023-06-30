using CommonControls.FileTypes.PackFiles.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Common
{
    public interface IEditorViewModel
    {
        IServiceScope ServiceScope { get; set; }
        NotifyAttr<string> DisplayName { get; set; }
        PackFile MainFile { get; set; }
        bool Save();
        void Close();
        bool HasUnsavedChanges { get; set; }
    }

    public interface IEditorCreator
    {
        void OpenFile(PackFile file);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }

    public delegate void EditorSavedDelegate(PackFile newFile);
}
