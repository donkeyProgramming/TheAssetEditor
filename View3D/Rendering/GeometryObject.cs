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


    /*
     
     public event TransformChanged OnTranformChanged;
  
        Vector3 _position = Vector3.Zero;
        public Vector3 Position { get { return _position; } set { _position = value; OnTranformChanged?.Invoke(this); } }

        Vector3 _scale = Vector3.One;
        public Vector3 Scale { get { return _scale; } set { _scale = value; OnTranformChanged?.Invoke(this); } }

        Quaternion _orientation = Quaternion.Identity;
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; OnTranformChanged?.Invoke(this); } }


        public Matrix CurrentOriantati = Matrix.Identity;
        public Vector3 Forward
        {
            get
            {
               return Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(Orientation));
            }
        }
        public Vector3 Up
        {
            get
            {
                return Vector3.Transform(Vector3.Up, Matrix.CreateFromQuaternion(Orientation));
            }
        }
     */

    enum GeometrySelectionMode
    { 
        Object,
        Face,
        Vertex
    };

    class SelectionInput    // Can be ray or frustrum
    { 
    }

    class CommonShaderParameters
    { 
    }

    interface IGeometryObject
    {
        bool IsSelectable();
        bool IsSelected();

        bool TrySelect(SelectionInput input);

        GeometrySelectionMode CurrentSelectionMode();
        GeometrySelectionMode[] SupportedSelectionModes();

        void Transform(Matrix delta);

        void Draw(CommonShaderParameters commonShaderParameters);
        void DrawWireframe(CommonShaderParameters commonShaderParameters);
     
    }


    class PolygonGeometryObject
    { 
    
    }


    class Mesh
    { 
    
    }







    class RenderObject
    { }

}
