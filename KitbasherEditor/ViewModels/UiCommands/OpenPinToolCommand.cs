using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.PinTool;
using View3D.Commands;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenPinToolCommand : IExecutableUiCommand
    {
        SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        public OpenPinToolCommand(SelectionManager selectionManager, CommandFactory commandFactory)
        {
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
        }

        public void Execute()
        {
            PinToolViewModel.ShowWindow(_selectionManager, _commandFactory);
        }
    }
}
