using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Utility;

namespace View3D.Rendering.Shading
{
    public interface IShader
    {
        public Effect Effect { get; }
        IShader Clone();
        void SetCommonParmeters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
    }

    public interface IShaderTextures
    {
        bool UseAlpha { set; }
         void SetTexture(Texture2D texture, TexureType type);
        void UseTexture(bool value, TexureType type);
    }

    public interface IShaderAnimation
    {
        public bool UseAnimation { set; }
        public void SetAnimationParameters(Matrix[] transforms, int weightCount = 4);
    }


    public class BasicShader : IShader
    {
        public Effect Effect { get; private set; }
 
        public BasicShader(GraphicsDevice device)
        {
            Effect = new BasicEffect(device);
        }

        protected BasicShader()
        { }

        public Vector3 DiffuseColor { set { (Effect as BasicEffect).DiffuseColor = value; } }
        public Vector3 SpecularColor { set { (Effect as BasicEffect).SpecularColor = value; } }
        public void EnableDefaultLighting() { (Effect as BasicEffect).EnableDefaultLighting(); }

        public IShader Clone()
        {
            var newShader = new BasicShader() { Effect = Effect.Clone() };
            return newShader;
        }

        public void SetCommonParmeters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            var typedEffect = (Effect as BasicEffect);
            typedEffect.Projection = commonShaderParameters.Projection;
            typedEffect.View = commonShaderParameters.View;
            typedEffect.World = modelMatrix;
        }
    }

    public class PbrShader : IShader, IShaderTextures, IShaderAnimation
    {
        public Effect Effect { get; private set; }

        Dictionary<TexureType, EffectParameter> _textures = new Dictionary<TexureType, EffectParameter>();
        Dictionary<TexureType, EffectParameter> _useTextures = new Dictionary<TexureType, EffectParameter>();
        ResourceLibary _resourceLibary;
        public PbrShader(ResourceLibary resourceLibary)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.Phazer);

            Effect.Parameters["tex_cube_diffuse"]?.SetValue(resourceLibary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"]?.SetValue(resourceLibary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"]?.SetValue(resourceLibary.PbrLut);

            _textures.Add(TexureType.Diffuse, Effect.Parameters["DiffuseTexture"]);
            _textures.Add(TexureType.Specular, Effect.Parameters["SpecularTexture"]);
            _textures.Add(TexureType.Normal, Effect.Parameters["NormalTexture"]);
            _textures.Add(TexureType.Gloss, Effect.Parameters["GlossTexture"]);

            _useTextures.Add(TexureType.Diffuse, Effect.Parameters["UseDiffuse"]);
            _useTextures.Add(TexureType.Specular, Effect.Parameters["UseSpecular"]);
            _useTextures.Add(TexureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextures.Add(TexureType.Gloss, Effect.Parameters["UseGloss"]);

            _resourceLibary = resourceLibary;
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
            if(_useTextures.ContainsKey(type))
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
        

        public void SetAnimationParameters(Matrix[] transforms, int weightCount = 4)
        {
            Effect.Parameters["WeightCount"].SetValue(weightCount);
            Effect.Parameters["tranforms"].SetValue(transforms);
        }

        public IShader Clone()
        {
            var newShader = new PbrShader(_resourceLibary);

            newShader.Effect.Parameters["DiffuseTexture"].SetValue(Effect.Parameters["DiffuseTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["SpecularTexture"].SetValue(Effect.Parameters["SpecularTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["NormalTexture"].SetValue(Effect.Parameters["NormalTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["GlossTexture"].SetValue(Effect.Parameters["GlossTexture"].GetValueTexture2D());

            return newShader;
        }
    }
}
