using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class VertexRenderItem : IRenderItem
    {
        public VertexInstanceMesh VertexRenderer { get; set; }

        public Rmv2MeshNode Node { get; set; }
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;
        public VertexSelectionState SelectedVertices { get; set; }

        const float CameraFovRadians = MathHelper.PiOver4;

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            if (renderingTechnique != RenderingTechnique.Normal)
                return;

            var viewportHeight = parameters.ViewportHeight > 0 ? parameters.ViewportHeight : device.Viewport.Height;

            VertexRenderer.Update(
                Node.Geometry,
                Node.RenderMatrix,
                parameters.CameraPosition,
                CameraFovRadians,
                viewportHeight,
                SelectedVertices);

            VertexRenderer.Draw(parameters.View, parameters.Projection, parameters.CameraPosition, device);
        }
    }
}
