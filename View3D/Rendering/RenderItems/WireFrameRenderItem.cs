using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;

namespace View3D.Rendering.RenderItems
{
    public class WireFrameRenderItem : IRenderItem
    {
        public IEditableGeometry Node { get; set; }
        public Matrix World { get; set; }
        public List<int> SelectedFaces { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Node.DrawWireframeOverlay(device, World, parameters);
        }
    }
}
