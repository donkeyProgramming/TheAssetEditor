using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ResetCameraCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Reset camera";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = new Hotkey(Key.F4, ModifierKeys.None);


        private readonly FocusSelectableObjectService _cameraFocusComponent;

        public ResetCameraCommand(FocusSelectableObjectService cameraFocusComponent)
        {
            _cameraFocusComponent = cameraFocusComponent;
        }

        public void Execute() => _cameraFocusComponent.ResetCamera();
    }
}
