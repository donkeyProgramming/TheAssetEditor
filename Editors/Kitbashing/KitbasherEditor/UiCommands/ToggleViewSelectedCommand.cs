using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ToggleViewSelectedCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "View only selected";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = new Hotkey(Key.Space, ModifierKeys.None);

        private readonly ViewOnlySelectedService _viewOnlySelectedComp;

        public ToggleViewSelectedCommand(ViewOnlySelectedService viewOnlySelectedComp)
        {
            _viewOnlySelectedComp = viewOnlySelectedComp;
        }

        public void Execute()
        {
            _viewOnlySelectedComp.Toggle();
        }
    }
}
