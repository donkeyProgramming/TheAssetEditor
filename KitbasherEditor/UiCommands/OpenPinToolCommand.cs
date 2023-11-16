using CommonControls.BaseDialogs;
using CommonControls.Common.MenuSystem;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.Events.UiCommands;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using KitbasherEditor.ViewModels.PinTool;
using KitbasherEditor.Views.EditorViews.PinTool;
using View3D.Commands;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class OpenPinToolCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open the pin tool";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        private readonly IWindowFactory _windowFactory;

        public OpenPinToolCommand( IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public void Execute()
        {
            var window = _windowFactory.Create<PinToolViewModel, PinToolView>("Pin tool", 360, 415);
            window.AlwaysOnTop = true;
            window.ShowWindow();
        }
    }
}
