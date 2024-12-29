using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Twui.Editor.Datatypes;
using Editors.Twui.Editor.PreviewRendering;
using Editors.Twui.Editor.Serialization;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Twui.Editor
{
    public partial class TwuiEditor : ObservableObject, IEditorInterface, ISaveableEditor, IFileEditor
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        [ObservableProperty] string _displayName = "Twui Editor";

        public bool HasUnsavedChanges { get; set; } = false;
        public PackFile CurrentFile { get; set; }

        [ObservableProperty] TwuiFile _parsedTwuiFile;
        [ObservableProperty] ComponentEditor _componentEditor;
        [ObservableProperty] PreviewRenderer _previewRenderer;

        public TwuiEditor(IUiCommandFactory uiCommandFactory, ComponentEditor componentEditor, PreviewRenderer previewRenderer)
        {
            _uiCommandFactory = uiCommandFactory;
            _componentEditor = componentEditor;
            _previewRenderer = previewRenderer;
        }

        public bool Save() { return true; } 
        public void Close() { }

        public void LoadFile(PackFile file)
        {
            if (file == CurrentFile)
                return;

            var serializer = new TwuiSerializer();
            ParsedTwuiFile = serializer.Load(file);
            DisplayName = "Twui Editor:" + Path.GetFileName(file.Name);

            ComponentEditor.SetFile(ParsedTwuiFile);
            PreviewRenderer.SetFile(ParsedTwuiFile);
        }
    }
}
