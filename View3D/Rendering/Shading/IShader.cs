using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
         void SetTexture(Texture2D texture, TextureType type);
        void UseTexture(bool value, TextureType type);
    }

    public interface IShaderAnimation
    {
        public bool UseAnimation { set; }
        public void SetAnimationParameters(Matrix[] transforms, int weightCount);
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

   
}
