﻿using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.ToolCreation
{
    public interface IEditorViewModel : IFileEditor
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
        Type GetScopeResolverType { get; }
    }

    public interface IEditorCreator
    {
        void OpenFile(PackFile file);
        void OpenFileInWindow(PackFile file, int width, int height);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }

    public delegate void EditorSavedDelegate(PackFile newFile);
}
