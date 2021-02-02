using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Gizmo;
using View3D.Rendering.Geometry;

namespace View3D.Rendering
{
    public delegate void TransformChangedDelegate(ITransformable item);

    public class RenderItem : ITransformable
    {
        public Matrix ModelMatrix { get; private set; } = Matrix.Identity;
        public IGeometry Geometry { get; set; }
        public string Id { get; set; } = String.Empty;

        public RenderItem(IGeometry geo, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Geometry = geo;
            Position = position;
            Orientation = rotation;
            Scale = scale;
        }

        public event TransformChangedDelegate TransformChanged;

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix();  } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix();  } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix();  } }
        public Vector3 Forward => Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(Orientation));
        public Vector3 Up => Vector3.Transform(Vector3.Up, Matrix.CreateFromQuaternion(Orientation));

        private void UpdateMatrix()
        {
            TransformChanged?.Invoke(this);
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }
    }

    public enum GeometrySelectionMode
    { 
        Object,
        Face,
        Vertex
    };




}
