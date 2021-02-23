using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Rendering.Geometry;
using View3D.Rendering.Shading;

namespace View3D.Rendering.RenderItems
{
    public class FaceRenderItem : IRenderItem
    {
        public IGeometry Geometry { get; set; }
        public Matrix ModelMatrix { get; set; }
        public IShader Shader { get; set; }

        public List<int> SelectedFaces { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Shader.SetCommonParmeters(parameters, ModelMatrix);
            Geometry.ApplyMeshPart(Shader, device, SelectedFaces);
        }
    }
}
