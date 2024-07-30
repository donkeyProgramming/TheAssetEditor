using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading.Shaders
{
    public interface IShader
    {
        void Apply(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
        void SetTechnique(RenderingTechnique technique);
        bool SupportsTechnique(RenderingTechnique technique);
    }
}
