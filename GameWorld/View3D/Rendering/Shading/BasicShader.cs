using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading
{
    public class BasicShader : IShader, IDisposable
    {
        private readonly BasicEffect _effect;

        bool _enableDefaultLighting = false;

        public Vector3 DiffuseColour { get; set; }
        public Vector3 SpecularColour { get; set; }
        public void EnableDefaultLighting() { _enableDefaultLighting = true; }

        public BasicShader(GraphicsDevice device)
        {
            _effect = new BasicEffect(device);
        }

        protected BasicShader(BasicEffect effect)
        {
            _effect = effect;
        }

        public BasicShader Clone()
        {
            var clonedEffect = _effect.Clone() as BasicEffect;
            return new BasicShader(clonedEffect!)
            {
                DiffuseColour = DiffuseColour,
                SpecularColour = SpecularColour,
            };
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            _effect.Projection = commonShaderParameters.Projection;
            _effect.View = commonShaderParameters.View;
            _effect.World = modelMatrix;
        }

        public void ApplyObjectParameters()
        {
            _effect.DiffuseColor = DiffuseColour;
            _effect.SpecularColor = SpecularColour;
            if (_enableDefaultLighting)
                _effect.EnableDefaultLighting();
        }

        public Effect GetEffect() => _effect;

        public void Dispose()
        {
            _effect.Dispose();
        }
    }
}
