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

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            if (renderingTechnique != RenderingTechnique.Normal)
                return;

            VertexRenderer.Update(Node.Geometry, Node.RenderMatrix, Node.Orientation, parameters.CameraPosition, SelectedVertices);
            VertexRenderer.Draw(parameters.View, parameters.Projection, device, new Vector3(0, 1, 0));
        }
    }
}
