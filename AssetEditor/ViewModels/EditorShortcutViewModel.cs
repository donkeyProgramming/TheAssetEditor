using CommunityToolkit.Mvvm.Input;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace AssetEditor.ViewModels
{
    public partial class EditorShortcutViewModel
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly EditorEnums _editor;

        public string DisplayName { get; set; }
        public bool IsEnabled{ get; set; }
        public bool IsSectionHeader => false;

        public EditorShortcutViewModel(EditorInfo editorInfo, IUiCommandFactory commandFactory)
        {
            _uiCommandFactory = commandFactory;
            _editor = editorInfo.EditorEnum;
            DisplayName = editorInfo.ToolbarName;
            IsEnabled = editorInfo.IsToolbarButtonEnabled;
        }

        [RelayCommand] private void OpenEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute(_editor);
    }

    /// <summary>A non-clickable rule and small title between groups of tools in the Tools menu.</summary>
    public class ToolsMenuSectionHeaderViewModel(string title)
    {
        public string Title { get; } = title;
        public bool IsSectionHeader => true;
    }
}
