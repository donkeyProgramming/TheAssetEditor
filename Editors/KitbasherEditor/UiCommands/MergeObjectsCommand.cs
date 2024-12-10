using System.Windows.Input;
using CommonControls.BaseDialogs.ErrorListDialog;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class MergeObjectsCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Merge selected meshes";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.TwoOrMoreObjectsSelected;
        public Hotkey? HotKey { get; } = new Hotkey(Key.M, ModifierKeys.Control);

        private readonly SelectionManager _selectionManager;
        private readonly ObjectEditor _objectEditor;

        public MergeObjectsCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
            {
                if (objectSelectionState.CurrentSelection().Count >= 2)
                {
                    if (!_objectEditor.CombineMeshes(objectSelectionState, out var errorList))
                        ErrorListWindow.ShowDialog("Combine Errors", errorList, false);
                }
            }
        }
    }
}
