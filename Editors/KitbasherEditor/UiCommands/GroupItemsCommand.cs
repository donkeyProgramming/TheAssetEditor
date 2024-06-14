using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class GroupItemsCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "(Un)Group";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey HotKey { get; } = new Hotkey(Key.G, ModifierKeys.Control);

        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;

        public GroupItemsCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute() => _objectEditor.GroupItems(_selectionManager.GetState() as ObjectSelectionState);
    }
}
