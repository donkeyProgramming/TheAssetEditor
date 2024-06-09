using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monogame.WpfInterop.ResourceHandling;
using Shared.GameFormats.RigidModel.Types;
using System.Collections.Generic;

namespace View3D.Rendering.Shading
{
    public abstract class PbrShader : IShader, IShaderTextures, IShaderAnimation
    {
        public Effect Effect { get; protected set; }
        public RenderFormats RenderFormat { get; protected set; }

        protected Dictionary<TextureType, EffectParameter> _textures = new Dictionary<TextureType, EffectParameter>();
        protected Dictionary<TextureType, EffectParameter> _useTextures = new Dictionary<TextureType, EffectParameter>();
        protected ResourceLibrary _resourceLibary;

        public PbrShader(ResourceLibrary resourceLibary, RenderFormats renderFormat)
        {
            _resourceLibary = resourceLibary;
            RenderFormat = renderFormat;
        }

        public bool UseAlpha { set { Effect.Parameters["UseAlpha"].SetValue(value); } }
        public Vector3 TintColour { set { Effect.Parameters["TintColour"].SetValue(value); } }

        public void SetTexture(Texture2D texture, TextureType type)
        {
            if (_textures.ContainsKey(type))
                _textures[type]?.SetValue(texture);

            UseTexture(texture != null, type);
        }

        public void UseTexture(bool value, TextureType type)
        {
            if (_useTextures.ContainsKey(type))
                _useTextures[type].SetValue(value);
        }

        public void SetCommonParmeters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            Effect.Parameters["View"].SetValue(commonShaderParameters.View);
            Effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            Effect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(commonShaderParameters.EnvLightRotationsRadians_Y)));
            Effect.Parameters["DirLightTransform"].SetValue(Matrix.CreateRotationY(commonShaderParameters.DirLightRotationRadians_Y) * Matrix.CreateRotationX(commonShaderParameters.DirLightRotationRadians_X));
            Effect.Parameters["LightMult"].SetValue(commonShaderParameters.LightIntensityMult);
            Effect.Parameters["World"].SetValue(modelMatrix);
            Effect.Parameters["CameraPos"].SetValue(commonShaderParameters.CameraPosition);
        }

        public bool UseAnimation { set { Effect.Parameters["doAnimation"].SetValue(value); } }


        public void SetAnimationParameters(Matrix[] transforms, int weightCount)
        {
            Effect.Parameters["WeightCount"].SetValue((int)weightCount);
            Effect.Parameters["tranforms"].SetValue(transforms);
        }

        public virtual void SetScaleMult(float scaleMult)
        { }

        public abstract IShader Clone();
    }

    public class PbrShader_SpecGloss : PbrShader
    {
        public PbrShader_SpecGloss(ResourceLibrary resourceLibary) : base(resourceLibary, RenderFormats.SpecGloss)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.Pbr_SpecGloss);

            _textures.Add(TextureType.Diffuse, Effect.Parameters["DiffuseTexture"]);
            _textures.Add(TextureType.Specular, Effect.Parameters["SpecularTexture"]);
            _textures.Add(TextureType.Normal, Effect.Parameters["NormalTexture"]);
            _textures.Add(TextureType.Gloss, Effect.Parameters["GlossTexture"]);

            _useTextures.Add(TextureType.Diffuse, Effect.Parameters["UseDiffuse"]);
            _useTextures.Add(TextureType.Specular, Effect.Parameters["UseSpecular"]);
            _useTextures.Add(TextureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextures.Add(TextureType.Gloss, Effect.Parameters["UseGloss"]);

            Effect.Parameters["tex_cube_diffuse"]?.SetValue(resourceLibary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"]?.SetValue(resourceLibary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"]?.SetValue(resourceLibary.PbrLut);
        }

        public override IShader Clone()
        {
            var newShader = new PbrShader_SpecGloss(_resourceLibary);

            newShader.Effect.Parameters["DiffuseTexture"].SetValue(Effect.Parameters["DiffuseTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["SpecularTexture"].SetValue(Effect.Parameters["SpecularTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["NormalTexture"].SetValue(Effect.Parameters["NormalTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["GlossTexture"].SetValue(Effect.Parameters["GlossTexture"].GetValueTexture2D());

            return newShader;
        }
    }

    public class PbrShader_MetalRoughness : PbrShader
    {
        public PbrShader_MetalRoughness(ResourceLibrary resourceLibary) : base(resourceLibary, RenderFormats.MetalRoughness)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.Pbs_MetalRough);

            _textures.Add(TextureType.BaseColour, Effect.Parameters["DiffuseTexture"]);
            _textures.Add(TextureType.Normal, Effect.Parameters["NormalTexture"]);
            _textures.Add(TextureType.MaterialMap, Effect.Parameters["GlossTexture"]);

            _useTextures.Add(TextureType.BaseColour, Effect.Parameters["UseDiffuse"]);
            _useTextures.Add(TextureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextures.Add(TextureType.MaterialMap, Effect.Parameters["UseGloss"]);

            Effect.Parameters["tex_cube_diffuse"]?.SetValue(resourceLibary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"]?.SetValue(resourceLibary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"]?.SetValue(resourceLibary.PbrLut);
        }

        public override IShader Clone()
        {
            var newShader = new PbrShader_MetalRoughness(_resourceLibary);

            newShader.Effect.Parameters["DiffuseTexture"].SetValue(Effect.Parameters["DiffuseTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["NormalTexture"].SetValue(Effect.Parameters["NormalTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["GlossTexture"].SetValue(Effect.Parameters["GlossTexture"].GetValueTexture2D());

            return newShader;
        }

        public override void SetScaleMult(float scaleMult)
        {
            Effect.Parameters["ModelRenderScale"].SetValue(scaleMult);
        }
    }
}
