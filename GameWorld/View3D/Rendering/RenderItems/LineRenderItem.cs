using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class LineRenderItem : IRenderItem
    {
        public LineMeshRender LineMesh { get; set; }

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            LineMesh.Render(device, parameters, ModelMatrix);
        }
    }
}
