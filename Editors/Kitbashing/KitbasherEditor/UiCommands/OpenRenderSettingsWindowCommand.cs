using System.Windows;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Utility.RenderSettingsDialog;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class OpenRenderSettingsWindowCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Open render settings window";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneLightParametersStore;

        public OpenRenderSettingsWindowCommand(RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneLightParametersStore)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneLightParametersStore = sceneLightParametersStore;
        }

        public void Execute()
        {
            var window = new RenderSettingsWindow(_renderEngineComponent, _sceneLightParametersStore) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        } 
    }
}


