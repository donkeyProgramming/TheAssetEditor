using System.Windows;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.ToolCreation
{
    public interface IEditorInterface
    {
        string DisplayName { get; set; }
        
        void Close();
    }

    public interface ISaveableEditor
    {
        bool Save();
        bool HasUnsavedChanges { get; set; }
    }

    public interface IFileEditor
    {
        PackFile CurrentFile { get; }
        public void LoadFile(PackFile file);
    }

    public interface IEditorCreator
    {
        IEditorInterface CreateFromFile(PackFile file, EditorEnums? preferedEditor = null);
        IEditorInterface Create(EditorEnums editor, Action<IEditorInterface>? onInitializeCallback = null);
        Window CreateWindow(PackFile packFile, EditorEnums? preferedEditor = null);
    }
}
