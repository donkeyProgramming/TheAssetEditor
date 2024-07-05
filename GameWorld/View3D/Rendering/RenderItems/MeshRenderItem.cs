using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class GeoRenderItem : IRenderItem
    {
        public Geometry.MeshObject Geometry { get; set; }
        public IShader Shader { get; set; }
        public Matrix ModelMatrix { get; set; }

        public List<int> Faces { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Shader.SetCommonParameters(parameters, ModelMatrix);
            if (Faces != null)
                Geometry.ApplyMeshPart(Shader, device, Faces);
            else
                Geometry.ApplyMesh(Shader, device);
        }
    }
}
