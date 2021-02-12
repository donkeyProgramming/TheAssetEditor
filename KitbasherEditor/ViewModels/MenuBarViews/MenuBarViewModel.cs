using Common;
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


        ToolbarCommandFactory _commandFactory = new ToolbarCommandFactory();

        public MenuBarViewModel(IComponentManager componentManager)
        {
            Gizmo = new GizmoModeMenuBarViewModel(componentManager, _commandFactory);
            General = new GeneralMenuBarViewModel(componentManager, _commandFactory);
        }

        public bool HandleKeyUp(Key key, ModifierKeys modifierKeys)
        {
            return _commandFactory.TriggerCommand(key, modifierKeys);
        }
    }


    public class ToolsMenuBarViewModel : NotifyPropertyChangedImpl
    {

        public ICommand SplitObjectCommand { get; set; }
        public ICommand MergeObjectCommand { get; set; }
        public ICommand FreezeTransformCommand { get; set; }
        public ICommand DuplicateObjectCommand { get; set; }
        public ICommand DeleteObjectCommand { get; set; }
        public ICommand MergeVertexCommand { get; set; }

    }
}
