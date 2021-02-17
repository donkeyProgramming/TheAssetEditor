using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class BoundingBoxRenderItem : IRenderItem
    {
        public BoundingBoxRenderer BoundingBoxRenderer { get; set; }

        public BoundingBox BoundingBox { get; set; }
        public Matrix World { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            BoundingBoxRenderer.Render(device, parameters, BoundingBox, World);
        }
    }
}
