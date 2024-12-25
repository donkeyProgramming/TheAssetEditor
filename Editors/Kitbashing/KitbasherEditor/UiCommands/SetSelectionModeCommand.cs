using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Components.Selection;
using Shared.Ui.Common.MenuSystem;
using System.Windows.Input;

namespace Editors.KitbasherEditor.UiCommands
{
    abstract public class SetSelectionModeCommand : ITransientKitbasherUiCommand
    {
        public abstract string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public abstract Hotkey? HotKey { get; }

        private readonly SelectionComponent _selectionComponent;

        protected SetSelectionModeCommand(SelectionComponent selectionComponent)
        {
            _selectionComponent = selectionComponent;
        }

        protected void UpdateSelectionMode(GeometrySelectionMode mode)
        {
            if (!_selectionComponent.Isinitialized)
                return;

            if (mode == GeometrySelectionMode.Object)
                _selectionComponent.SetObjectSelectionMode();
            else if (mode == GeometrySelectionMode.Face)
                _selectionComponent.SetFaceSelectionMode();
            else if (mode == GeometrySelectionMode.Vertex)
                _selectionComponent.SetVertexSelectionMode();
            else
                throw new NotImplementedException("Unknown state");
        }

        public abstract void Execute();
    }

    public class ObjectSelectionModeCommand : SetSelectionModeCommand
    {
        public override string ToolTip { get; set; } = "Object mode";
        public override Hotkey? HotKey { get; } = new Hotkey(Key.F1, ModifierKeys.None);

        public ObjectSelectionModeCommand(SelectionComponent selectionComponent) : base(selectionComponent)
        {
        }

        public override void Execute() => UpdateSelectionMode(GeometrySelectionMode.Object);
    }

    public class FaceSelectionModeCommand : SetSelectionModeCommand
    {
        public override string ToolTip { get; set; } = "Face mode";
        public override Hotkey? HotKey { get; } = new Hotkey(Key.F2, ModifierKeys.None);

        public FaceSelectionModeCommand(SelectionComponent selectionComponent) : base(selectionComponent)
        {
        }

        public override void Execute() => UpdateSelectionMode(GeometrySelectionMode.Face);
    }

    public class VertexSelectionModeCommand : SetSelectionModeCommand
    {
        public override string ToolTip { get; set; } = "Vertex mode";
        public override Hotkey? HotKey { get; } = new Hotkey(Key.F3, ModifierKeys.None);

        public VertexSelectionModeCommand(SelectionComponent selectionComponent) : base(selectionComponent)
        {
        }

        public override void Execute() => UpdateSelectionMode(GeometrySelectionMode.Vertex);
    }
}
