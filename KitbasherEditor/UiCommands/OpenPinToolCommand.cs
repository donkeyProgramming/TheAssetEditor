using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.PinTool;
using View3D.Commands;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenPinToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the pin tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

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
