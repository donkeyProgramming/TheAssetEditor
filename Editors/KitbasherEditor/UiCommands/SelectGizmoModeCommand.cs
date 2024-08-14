using GameWorld.Core.Components.Gizmo;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    internal class SelectGizmoModeCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Select Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.Q, ModifierKeys.None);

        private readonly GizmoComponent _gizmoComponent;
        private readonly TransformToolViewModel _transformToolViewModel;

        public SelectGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel)
        {
            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
        }

        public void Execute()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.None);
            _gizmoComponent.Disable();
        }
    }

    internal class MoveGizmoModeCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Move Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.W, ModifierKeys.None);

        private readonly GizmoComponent _gizmoComponent;
        private readonly TransformToolViewModel _transformToolViewModel;

        public MoveGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel)
        {
            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
        }


        public void Execute()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Translate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);
        }


    }

    internal class RotateGizmoModeCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Rotate Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.E, ModifierKeys.None);

        private readonly GizmoComponent _gizmoComponent;
        private readonly TransformToolViewModel _transformToolViewModel;

        public RotateGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel)
        {
            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
        }


        public void Execute()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Rotate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
        }


    }

    internal class ScaleGizmoModeCommand : IKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Scale Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.R, ModifierKeys.None);

        private readonly GizmoComponent _gizmoComponent;
        private readonly TransformToolViewModel _transformToolViewModel;

        public ScaleGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel)
        {
            _gizmoComponent = gizmoComponent;
            _transformToolViewModel = transformToolViewModel;
        }

        public void Execute()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Scale);
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
        }
    }
}
