using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
{
    public interface IShader
    {
        public Effect Effect { get; }
        IShader Clone();
        void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
    }

    public interface IShaderTextures
    {
        bool UseAlpha { set; }
        void SetTexture(Texture2D texture, TextureType type);
        void UseTexture(bool value, TextureType type);
    }

    public interface IShaderAnimation
    {
        public bool UseAnimation { set; }
        public void SetAnimationParameters(Matrix[] transforms, int weightCount);
    }
}
