using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading
{
    public interface IShader
    {
        void ApplyObjectParameters();
        void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
        Effect GetEffect();
    }
}
