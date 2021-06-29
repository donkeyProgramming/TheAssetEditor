using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public interface IEditorViewModel
    {
        string DisplayName { get; set; }
        IPackFile MainFile { get; set; }
        bool Save();
        void Close();
        bool HasUnsavedChanges();
    }

    public interface IKitBashEditor : IEditorViewModel
    {
        IPackFile ReferenceModel { get; set; }
    }


    public interface IEditorCreator
    {
        void OpenFile(IPackFile file);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }

}
