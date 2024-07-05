using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading
{
    public class BasicShader : IShader
    {
        public BasicEffect _effect;
        public Effect Effect => _effect;

        public BasicShader(GraphicsDevice device)
        {
            _effect = new BasicEffect(device);
        }

        protected BasicShader(BasicEffect effect)
        {
            _effect = effect;
        }

        public Vector3 DiffuseColour { set { _effect.DiffuseColor = value; } }
        public Vector3 SpecularColour { set { _effect.SpecularColor = value; } }
        public void EnableDefaultLighting() { _effect.EnableDefaultLighting(); }

        public IShader Clone()
        {
            var clonedEffect = _effect.Clone() as BasicEffect;
            return new BasicShader(clonedEffect!);
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            _effect.Projection = commonShaderParameters.Projection;
            _effect.View = commonShaderParameters.View;
            _effect.World = modelMatrix;
        }
    }
}
