using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    public class GroupItemsCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "(Un)Group";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = new Hotkey(Key.G, ModifierKeys.Control);

        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;

        public GroupItemsCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute() => _objectEditor.GroupItems(_selectionManager.GetState() as ObjectSelectionState);
    }
}
