using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Shading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class GeometryRenderItem : IRenderItem
    {
        public MeshObject Geometry { get; set; }
        public IShader Shader { get; set; }
        public Matrix ModelMatrix { get; set; }

        public List<int> Faces { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Shader.GetEffect().CurrentTechnique = Shader.GetEffect().Techniques.FirstOrDefault(x => x.Name == "BasicColorDrawing");

            Shader.SetCommonParameters(parameters, ModelMatrix);
            Shader.ApplyObjectParameters();

           

            if (Faces != null)
                ApplyMeshPart(Shader, device, Faces, Geometry.GetGeometryContext());
            else
                ApplyMesh(Shader, device, Geometry.GetGeometryContext());
        }

        public void DrawGlowPass(GraphicsDevice device, CommonShaderParameters parameters)
        {
            var effect = Shader.GetEffect();

            effect.CurrentTechnique = effect.Techniques.FirstOrDefault(x => x.Name == "GlowDrawing");

            Shader.SetCommonParameters(parameters, ModelMatrix);
            Shader.ApplyObjectParameters();

            ApplyMesh(Shader, device, Geometry.GetGeometryContext());
        }

        void ApplyMesh(IShader effect, GraphicsDevice device, IGraphicsCardGeometry geometry)
        {
            device.Indices = geometry.IndexBuffer;
            device.SetVertexBuffer(geometry.VertexBuffer);
            foreach (var pass in effect.GetEffect().CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geometry.IndexBuffer.IndexCount);
            }
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

