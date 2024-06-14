using System.Windows.Input;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class FocusCameraCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Focus camera on selected";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.F, ModifierKeys.Control);

        private readonly FocusSelectableObjectService _cameraFocusComponent;

        public FocusCameraCommand(FocusSelectableObjectService cameraFocusComponent)
        {
            _cameraFocusComponent = cameraFocusComponent;
        }

        public void Execute() => _cameraFocusComponent.FocusSelection();
    }
}
