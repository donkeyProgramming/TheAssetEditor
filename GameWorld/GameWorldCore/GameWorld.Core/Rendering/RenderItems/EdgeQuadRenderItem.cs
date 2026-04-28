using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class EdgeQuadRenderItem : IRenderItem
    {
        public EdgeQuadInstanceMesh EdgeQuadRenderer { get; set; }
        public EdgeData[] Edges { get; set; }
        public int EdgeCount { get; set; }
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        private bool _dirty = true;
        private EdgeData[] _lastEdges;

        public void MarkDirty() => _dirty = true;

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            if (renderingTechnique != RenderingTechnique.Normal)
                return;

            if (Edges == null || EdgeQuadRenderer == null || EdgeCount == 0)
                return;

            if (_dirty || _lastEdges != Edges)
            {
                EdgeQuadRenderer.Update(Edges, EdgeCount, parameters);
                _lastEdges = Edges;
                _dirty = false;
            }

            EdgeQuadRenderer.Draw(parameters, device);
        }
    }
}
