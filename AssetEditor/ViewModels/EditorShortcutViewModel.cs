using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor.ViewModels
{
    public partial class EditorShortcutViewModel
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly EditorEnums _editor;

        public string DisplayName { get; set; }
        public bool IsEnabled{ get; set; }

        public EditorShortcutViewModel(EditorInfo editorInfo, IUiCommandFactory commandFactory)
        {
            _uiCommandFactory = commandFactory;
            _editor = editorInfo.EditorEnum;
            DisplayName = editorInfo.ToolbarName;
            IsEnabled = editorInfo.IsToolbarButtonEnabled;
        }

        [RelayCommand] private void OpenEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute(_editor);
    }
}
