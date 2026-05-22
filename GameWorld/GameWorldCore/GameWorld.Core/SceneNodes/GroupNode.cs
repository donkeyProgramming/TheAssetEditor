using Microsoft.Xna.Framework;

namespace GameWorld.Core.SceneNodes
{
    public class GroupNode : SceneNode
    {
        public GroupNode(string name = "")
        {
            Name = name;
        }

        public override ISceneNode CreateCopyInstance() => new GroupNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as GroupNode;
            typedTarget.IsUngroupable = IsUngroupable;
            typedTarget.IsLockable = IsLockable;
            base.CopyInto(target);
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
