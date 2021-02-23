using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class LineRenderItem : IRenderItem
    {
        public LineMeshRender LineMesh { get; set; }

        public Matrix World { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            LineMesh.Render(device, parameters, World);
        }
    }
}
