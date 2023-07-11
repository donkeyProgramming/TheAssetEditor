using CommonControls.Events.UiCommands;
using View3D.Components.Component.Selection;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class GroupItemsCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;

        public GroupItemsCommand(SelectionManager selectionManager, ObjectEditor objectEditor)
        {
            _selectionManager = selectionManager;
            _objectEditor = objectEditor;
        }

        public void Execute()
        {
            _objectEditor.GroupItems(_selectionManager.GetState() as ObjectSelectionState);
        }
    }
}
