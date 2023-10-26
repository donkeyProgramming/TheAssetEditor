using CommonControls.Common.MenuSystem;
using KitbasherEditor.ViewModels.MenuBarViews;
using System.Windows.Input;
using View3D.Services;

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
