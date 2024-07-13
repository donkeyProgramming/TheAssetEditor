using System.Collections.Generic;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
{
    public abstract class PbrShader : IShader, IShaderTextures, IShaderAnimation
    {
        public abstract Effect Effect { get; protected set; }
        public abstract RenderFormats RenderFormat { get; }

        protected Dictionary<TextureType, EffectParameter> _textureEffectParams = [];
        protected Dictionary<TextureType, EffectParameter> _useTextureParams = [];
        protected ResourceLibrary _resourceLibrary;

        public PbrShader(ResourceLibrary resourceLibrary)
        {
            _resourceLibrary = resourceLibrary;
        }

        public bool UseAlpha { set { Effect.Parameters["UseAlpha"].SetValue(value); } }
        public Vector3 TintColour { set { Effect.Parameters["TintColour"].SetValue(value); } }

        public void SetTexture(Texture2D texture, TextureType type)
        {
            if (_textureEffectParams.ContainsKey(type))
                _textureEffectParams[type]?.SetValue(texture);

            UseTexture(texture != null, type);
        }

        public void UseTexture(bool value, TextureType type)
        {
            if (_useTextureParams.ContainsKey(type))
                _useTextureParams[type].SetValue(value);
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            Effect.Parameters["View"].SetValue(commonShaderParameters.View);
            Effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            Effect.Parameters["EnvMapTransform"].SetValue(Matrix.CreateRotationY(commonShaderParameters.EnvLightRotationsRadians_Y));
            Effect.Parameters["DirLightTransform"].SetValue(Matrix.CreateRotationY(commonShaderParameters.DirLightRotationRadians_Y) * Matrix.CreateRotationX(commonShaderParameters.DirLightRotationRadians_X));
            Effect.Parameters["LightMult"].SetValue(commonShaderParameters.LightIntensityMult);
            Effect.Parameters["World"].SetValue(modelMatrix);
            Effect.Parameters["CameraPos"].SetValue(commonShaderParameters.CameraPosition);
        }

        public bool UseAnimation { set { Effect.Parameters["doAnimation"].SetValue(value); } }


        public void SetAnimationParameters(Matrix[] transforms, int weightCount)
        {
            Effect.Parameters["WeightCount"].SetValue(weightCount);
            Effect.Parameters["tranforms"].SetValue(transforms);
        }

        public virtual void SetScaleMult(float scaleMult)
        { }

        public abstract IShader Clone();
    }
}
