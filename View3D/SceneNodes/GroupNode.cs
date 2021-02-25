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

        public override SceneNode Clone()
        {
            var newItem = new GroupNode()
            {
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                Name = Name + " - Clone",
            };
            return newItem;
        }
    }
}
