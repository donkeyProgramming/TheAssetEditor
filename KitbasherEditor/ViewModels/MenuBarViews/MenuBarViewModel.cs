using MonoGame.Framework.WpfInterop;
using System;
using System.Text;
using System.Windows.Input;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{

    public class MenuBarViewModel : IKeyboardHandler
    {
        public GizmoModeMenuBarViewModel Gizmo { get; set; }
        public GeneralMenuBarViewModel General { get; set; }
        public ToolsMenuBarViewModel Tools { get; set; }


        ToolbarCommandFactory _commandFactory = new ToolbarCommandFactory();

        public MenuBarViewModel(IComponentManager componentManager)
        {
            Gizmo = new GizmoModeMenuBarViewModel(componentManager, _commandFactory);
            General = new GeneralMenuBarViewModel(componentManager, _commandFactory);
            Tools = new ToolsMenuBarViewModel(componentManager, _commandFactory);
        }

        public bool HandleKeyUp(Key key, ModifierKeys modifierKeys)
        {
            return _commandFactory.TriggerCommand(key, modifierKeys);
        }
    }
}
