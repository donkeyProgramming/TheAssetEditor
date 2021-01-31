using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering.Geometry;

namespace View3D.Rendering
{

    public class RenderItem
    {
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;
        public IGeometry Geometry { get; set; }

        public RenderItem(IGeometry geo, Matrix matrix)
        {
            Geometry = geo;
            ModelMatrix = matrix;
        }
    }




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
