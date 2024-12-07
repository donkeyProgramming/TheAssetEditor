using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Gizmo;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    internal class SelectGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel) : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Select Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.Q, ModifierKeys.None);

        public void Execute()
        {
            gizmoComponent.ResetScale();
            transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.None);
            gizmoComponent.Disable();
        }
    }

    internal class MoveGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel) : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Move Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = new Hotkey(Key.W, ModifierKeys.None);

        public void Execute()
        {
            gizmoComponent.ResetScale();
            transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Translate);
            gizmoComponent.SetGizmoMode(GizmoMode.Translate);
        }
    }

    internal class RotateGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel) : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Rotate Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = new Hotkey(Key.E, ModifierKeys.None);

        public void Execute()
        {
            gizmoComponent.ResetScale();
            transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Rotate);
            gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
        }
    }

    internal class ScaleGizmoModeCommand(GizmoComponent gizmoComponent, TransformToolViewModel transformToolViewModel) : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Scale Gizmo";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = new Hotkey(Key.R, ModifierKeys.None);

        public void Execute()
        {
            gizmoComponent.ResetScale();
            transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Scale);
            gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
        }
    }
}
