using System.Collections.Generic;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Shading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class SelectedFacesRenderItem : IRenderItem
    {
        public MeshObject Geometry { get; set; }
        public IShader Shader { get; set; }
        public Matrix ModelMatrix { get; set; }

        public List<int> Faces { get; set; }


        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            Shader.SetCommonParameters(parameters, ModelMatrix);
            Shader.ApplyObjectParameters();

            ApplyMeshPart(Shader, device, Faces, Geometry.GetGeometryContext());
        }

        void ApplyMeshPart(IShader effect, GraphicsDevice device, List<int> faceSelection, IGraphicsCardGeometry geometry)
        {
            device.Indices = geometry.IndexBuffer;
            device.SetVertexBuffer(geometry.VertexBuffer);
            foreach (var pass in effect.GetEffect().CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }
        }
    }
}

