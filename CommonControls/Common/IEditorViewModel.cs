using CommonControls.FileTypes.PackFiles.Models;

namespace CommonControls.Common
{
    public interface IEditorViewModel
    {
        NotifyAttr<string> DisplayName { get; set; }
        PackFile MainFile { get; set; }
        bool Save();
        void Close();
        bool HasUnsavedChanges();
    }

    public interface IEditorCreator
    {
        void OpenFile(PackFile file);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }


    public delegate void EditorSavedDelegate(PackFile newFile);

}
