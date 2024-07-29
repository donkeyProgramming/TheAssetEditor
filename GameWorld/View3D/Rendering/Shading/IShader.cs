using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading
{
    public interface IShader
    {
        void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);// ApplyObjectParameters
        Effect GetEffect();

        void SetTechnique(RenderingTechnique technique);
        bool SupportsTechnique(RenderingTechnique technique);
    }
}
