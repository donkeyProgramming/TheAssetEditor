using CommonControls.FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common
{
    public interface IEditorViewModel
    {
        string DisplayName { get; set; }
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

}
