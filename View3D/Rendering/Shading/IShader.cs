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

    public class NewShader : IShader, IShaderTextures
    {
        public Effect Effect { get; private set; }
        Dictionary<TexureType, EffectParameter> _textures = new Dictionary<TexureType, EffectParameter>();

        public NewShader(ResourceLibary resourceLibary)
        {
            Effect = resourceLibary.GetEffect(ShaderTypes.BasicEffect);
            _textures.Add(TexureType.Diffuse, Effect.Parameters["DiffuseTexture"]);
            //_textures.Add(TexureType.Specular, Effect.Parameters["SpecularTexture"]);
            _textures.Add(TexureType.Normal, Effect.Parameters["NormalTexture"]);
            //_textures.Add(TexureType.Gloss, Effect.Parameters["GlossTexture"]);

            Effect.Parameters["DiffuseColor"].SetValue(Vector3.One);
           // Effect.Parameters["SpecularColor"].SetValue(Vector3.One);
           // Effect.Parameters["SpecularPower"].SetValue(16.0f);

            SetLight(0, new Vector3(-0.5265408f, -0.5735765f, -0.6275069f), new Vector3(1, 0.9607844f, 0.8078432f), new Vector3(1, 0.9607844f, 0.8078432f));
            SetLight(1, new Vector3(0.7198464f, 0.3420201f, 0.6040227f), new Vector3(0.9647059f, 0.7607844f, 0.4078432f), Vector3.Zero);
            SetLight(2, new Vector3(0.4545195f, -0.7660444f, 0.4545195f), new Vector3(0.3231373f, 0.3607844f, 0.3937255f), new Vector3(0.3231373f, 0.3607844f, 0.3937255f));
        }

        void SetLight(int lightIndex, Vector3 direction, Vector3 diffuseColour, Vector3 specularColour)
        {
            Effect.Parameters[$"DirLight{lightIndex}Direction"].SetValue(direction);
            Effect.Parameters[$"DirLight{lightIndex}DiffuseColor"].SetValue(diffuseColour);
            Effect.Parameters[$"DirLight{lightIndex}SpecularColor"].SetValue(specularColour);
        }

        public bool UseAlpha { set { /*Effect.Parameters["UseAlpha"].SetValue(value); */} }
        public void SetTexture(Texture2D texture, TexureType type)
        {
            if(_textures.ContainsKey(type))
                _textures[type].SetValue(texture);
        }

        public IShader Clone()
        {
            throw new NotImplementedException();
        }

        public void SetCommonParmeters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            Effect.Parameters["EyePosition"].SetValue(commonShaderParameters.CameraPosition);
            Effect.Parameters["World"].SetValue(modelMatrix);
            Effect.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(modelMatrix)));
            Effect.Parameters["WorldViewProj"].SetValue(modelMatrix * commonShaderParameters.View * commonShaderParameters.Projection);

            //Effect.Parameters["View"].SetValue(commonShaderParameters.View);
            //Effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            //
            //Effect.Parameters["cameraLookAt"].SetValue(commonShaderParameters.CameraLookAt);
            //Effect.Parameters["ViewInverse"].SetValue(Matrix.Invert(commonShaderParameters.View));
            //Effect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(commonShaderParameters.EnvRotate)));
            
        }

        public void UseTexture(bool value, TexureType type)
        {
            throw new NotImplementedException();
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

            Effect.Parameters["tex_cube_diffuse"].SetValue(resourceLibary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"].SetValue(resourceLibary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"].SetValue(resourceLibary.PbrLut);

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

        public void SetTexture(Texture2D texture, TexureType type)
        {
            if (_textures.ContainsKey(type))
                _textures[type].SetValue(texture);
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
            Effect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(commonShaderParameters.EnvRotate)));
            Effect.Parameters["World"].SetValue(modelMatrix);
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
