using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
{
    public class PbrShader_MetalRoughness : PbrShader
    {
        public override  RenderFormats RenderFormat { get => RenderFormats.MetalRoughness; }
        public override Effect Effect { get; protected set; }
        public PbrShader_MetalRoughness(ResourceLibrary resourceLibrary) : base(resourceLibrary )
        {
            Effect = resourceLibrary.GetEffect(ShaderTypes.Pbs_MetalRough);

            _textureEffectParams.Add(TextureType.BaseColour, Effect.Parameters["DiffuseTexture"]);
            _textureEffectParams.Add(TextureType.Normal, Effect.Parameters["NormalTexture"]);
            _textureEffectParams.Add(TextureType.MaterialMap, Effect.Parameters["GlossTexture"]);

            _useTextureParams.Add(TextureType.BaseColour, Effect.Parameters["UseDiffuse"]);
            _useTextureParams.Add(TextureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextureParams.Add(TextureType.MaterialMap, Effect.Parameters["UseGloss"]);

            Effect.Parameters["tex_cube_diffuse"]?.SetValue(resourceLibrary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"]?.SetValue(resourceLibrary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"]?.SetValue(resourceLibrary.PbrLut);
        }

        public override IShader Clone()
        {
            var newShader = new PbrShader_MetalRoughness(_resourceLibrary);

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
