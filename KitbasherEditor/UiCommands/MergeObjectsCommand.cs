using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using System.Windows.Input;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class MergeObjectsCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Merge selected meshes";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.TwoOrMoreObjectsSelected;
        public Hotkey HotKey { get; } = new Hotkey(Key.M, ModifierKeys.Control);

        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;

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
