using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Rendering.Materials.Shaders
{
    public interface IShader
    {
        void Apply(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
        void SetTechnique(RenderingTechnique technique);
        bool SupportsTechnique(RenderingTechnique technique);
    }
}
