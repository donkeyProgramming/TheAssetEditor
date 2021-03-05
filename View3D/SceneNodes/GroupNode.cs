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
                Name = Name + " - Clone",
            };
            return newItem;
        }

        public bool IsUngroupable { get; set; } = false;
        public bool IsSelectable { get; set; } = false;
    }
}
