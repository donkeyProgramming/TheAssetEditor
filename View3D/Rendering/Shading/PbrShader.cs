using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using View3D.Utility;

namespace View3D.Rendering.Shading
{
    public abstract class PbrShader : IShader, IShaderTextures, IShaderAnimation
    {
        public Effect Effect { get; protected set; }
        public RenderFormats RenderFormat { get; protected set; }

        protected Dictionary<TexureType, EffectParameter> _textures = new Dictionary<TexureType, EffectParameter>();
        protected Dictionary<TexureType, EffectParameter> _useTextures = new Dictionary<TexureType, EffectParameter>();
        protected ResourceLibary _resourceLibary;
        
        public PbrShader(ResourceLibary resourceLibary, RenderFormats renderFormat)
        {
            _resourceLibary = resourceLibary;
            RenderFormat = renderFormat;
        }

        public bool UseAlpha { set { Effect.Parameters["UseAlpha"].SetValue(value); } }
        public Vector3 TintColour { set { Effect.Parameters["TintColour"].SetValue(value); } }

        public void SetTexture(Texture2D texture, TexureType type)
        {
            if (_textures.ContainsKey(type))
                _textures[type]?.SetValue(texture);

            UseTexture(texture != null, type);
        }

        public void UseTexture(bool value, TexureType type)
        {
            if (_useTextures.ContainsKey(type))
                _useTextures[type].SetValue(value);
        }

        public void SetCommonParmeters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            Effect.Parameters["View"].SetValue(commonShaderParameters.View);
            Effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            Effect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(commonShaderParameters.LightRotationRadians)));
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

        public abstract IShader Clone();
    }

    public class PbrShader_SpecGloss : PbrShader
    {
        public PbrShader_SpecGloss(ResourceLibary resourceLibary) : base(resourceLibary, RenderFormats.SpecGloss)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.Pbr_SpecGloss);

            _textures.Add(TexureType.Diffuse, Effect.Parameters["DiffuseTexture"]);
            _textures.Add(TexureType.Specular, Effect.Parameters["SpecularTexture"]);
            _textures.Add(TexureType.Normal, Effect.Parameters["NormalTexture"]);
            _textures.Add(TexureType.Gloss, Effect.Parameters["GlossTexture"]);

            _useTextures.Add(TexureType.Diffuse, Effect.Parameters["UseDiffuse"]);
            _useTextures.Add(TexureType.Specular, Effect.Parameters["UseSpecular"]);
            _useTextures.Add(TexureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextures.Add(TexureType.Gloss, Effect.Parameters["UseGloss"]);

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
        public PbrShader_MetalRoughness(ResourceLibary resourceLibary) : base(resourceLibary, RenderFormats.MetalRoughness)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.Pbs_MetalRough);

            _textures.Add(TexureType.BaseColour, Effect.Parameters["DiffuseTexture"]);
            _textures.Add(TexureType.Normal, Effect.Parameters["NormalTexture"]);
            _textures.Add(TexureType.MaterialMap, Effect.Parameters["GlossTexture"]);

            _useTextures.Add(TexureType.BaseColour, Effect.Parameters["UseDiffuse"]);
            _useTextures.Add(TexureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextures.Add(TexureType.MaterialMap, Effect.Parameters["UseGloss"]);

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
    }
}
