using System.Collections.Generic;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class PartialGeometryRenderItem : IRenderItem
    {
        private readonly MeshObject _geometry;
        private readonly Matrix _modelMatrix;
        private readonly IShader _shader;
        private readonly List<int> _selectedFaces;

        public PartialGeometryRenderItem(MeshObject geometry, Matrix modelMatrix, IShader shader, List<int> selectedFaces)
        {
            _geometry = geometry;
            _modelMatrix = modelMatrix;
            _shader = shader;
            _selectedFaces = selectedFaces;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            _shader.Apply(parameters, _modelMatrix);
            ApplyMeshPart(_shader, device, _selectedFaces, _geometry.GetGeometryContext());
        }

        void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection, IGraphicsCardGeometry geometry)
        {
            device.Indices = geometry.IndexBuffer;
            device.SetVertexBuffer(geometry.VertexBuffer);
            //foreach (var pass in effect.GetEffect().CurrentTechnique.Passes)
            {
               // pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }
        }
    }
}

