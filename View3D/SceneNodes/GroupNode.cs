using Microsoft.Xna.Framework;
using View3D.Components.Gizmo;

namespace View3D.SceneNodes
{
    public class GroupNode : SceneNode
    {
        public GroupNode(string name = "")
        {
            Name = name;
        }

        public override ISceneNode Clone()
        {
            var newItem = new GroupNode()
            {
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                IsUngroupable = IsUngroupable,
                IsLockable = IsLockable,
                Name = Name + " - Clone",
            };
            return newItem;
        }

        public bool IsUngroupable { get; set; } = false;
        bool _isSelectable = false;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }
        public bool IsLockable { get; set; } = false;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }

        private void UpdateModelMatrix(Matrix value)
        {
            base.ModelMatrix = value;
            RenderMatrix = value;
        }
    }

    public class WsModelGroup : GroupNode
    {
        public WsModelGroup(string name = "") : base(name)
        {
        }
    }
}
