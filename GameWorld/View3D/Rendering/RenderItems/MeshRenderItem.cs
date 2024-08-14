using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class GeometryRenderItem : IRenderItem
    {
        private readonly MeshObject _geometry;
        private readonly IShader _shader;
        private readonly Matrix _modelMatrix;

        public GeometryRenderItem(MeshObject geometry, IShader shader, Matrix modelMatrix)
        {
            _geometry = geometry;
            _shader = shader;
            _modelMatrix = modelMatrix;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            if (_shader.SupportsTechnique(renderingTechnique) == false)
                return;

            _shader.SetTechnique(renderingTechnique);
            _shader.Apply(parameters, _modelMatrix);

            ApplyMesh(_shader, device, _geometry.GetGeometryContext());
        }

        void ApplyMesh(IShader effect, GraphicsDevice device, IGraphicsCardGeometry geometry)
        {
            device.Indices = geometry.IndexBuffer;
            device.SetVertexBuffer(geometry.VertexBuffer);
            //foreach (var pass in effect.GetEffect().CurrentTechnique.Passes)
            {
              //  pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geometry.IndexBuffer.IndexCount);
            }
        }
    }
}

