using System;
using CommonControls.FileTypes.PackFiles.Models;

namespace CommonControls.Common
{
    public interface IEditorViewModel: IFileEditor
    {
        NotifyAttr<string> DisplayName { get; set; }
        void Close();
    }

    public interface ISaveableEditor
    {
        bool Save();
        bool HasUnsavedChanges { get; set; }
    }

    public interface IFileEditor
    {
        PackFile MainFile { get; set; }
    }

    public interface IEditorScopeResolverHint
    { 
        Type GetScopeResolverType { get;}
    }

    public interface IEditorCreator
    {
        void OpenFile(PackFile file);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }

    public delegate void EditorSavedDelegate(PackFile newFile);
}
