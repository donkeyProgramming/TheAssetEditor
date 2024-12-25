using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class FocusCameraCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Focus camera on selected";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = new Hotkey(Key.F, ModifierKeys.Control);

        private readonly FocusSelectableObjectService _cameraFocusComponent;

        public FocusCameraCommand(FocusSelectableObjectService cameraFocusComponent)
        {
            _cameraFocusComponent = cameraFocusComponent;
        }

        public void Execute() => _cameraFocusComponent.FocusSelection();
    }
}
