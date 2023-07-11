using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Events.UiCommands;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class MergeObjectsCommand : IExecutableUiCommand
    {
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
