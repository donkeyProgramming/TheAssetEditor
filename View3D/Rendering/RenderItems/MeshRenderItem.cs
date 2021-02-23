using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.Shading;
using View3D.SceneNodes;

namespace View3D.Rendering.RenderItems
{
    public class GeoRenderItem : IRenderItem
    {
        public IGeometry Geometry { get; set; }
        public IShader Shader { get; set; }
        public Matrix ModelMatrix { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Shader.SetCommonParmeters(parameters, ModelMatrix);
            Geometry.ApplyMesh(Shader, device);
        }
    }
}
