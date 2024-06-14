using System.Windows.Input;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleViewSelectedCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "View only selected";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.Space, ModifierKeys.None);

        ViewOnlySelectedService _viewOnlySelectedComp;

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
