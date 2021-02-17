using Microsoft.Xna.Framework;
using View3D.Components.Gizmo;

namespace View3D.SceneNodes
{
    public class GroupNode : SceneNode, ITransformable
    {
        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }

        public GroupNode(string name = "")
        {
            Name = name;
        }

        public override SceneNode Clone()
        {
            var newItem = new GroupNode()
            {
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                Name = Name + " - Clone",
            };
            return newItem;
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }
    }
}
