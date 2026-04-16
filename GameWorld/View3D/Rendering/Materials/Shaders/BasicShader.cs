using System;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Materials.Shaders
{
    public class BasicShader : IShader, IDisposable
    {
        private readonly BasicEffect _effect;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        bool _enableDefaultLighting = false;

        public Vector3 DiffuseColour { get; set; }
        public Vector3 SpecularColour { get; set; }
        public void EnableDefaultLighting() { _enableDefaultLighting = true; }

        public BasicShader(GraphicsDevice device, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _graphicsResourceCreator = graphicsResourceCreator;
            _effect = graphicsResourceCreator.CreateBasicEffect();
        }

        protected BasicShader(BasicEffect effect, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _graphicsResourceCreator = graphicsResourceCreator;
            _effect = graphicsResourceCreator.Track(effect);
        }

        public BasicShader Clone()
        {
            var clonedEffect = _effect.Clone() as BasicEffect;
            return new BasicShader(clonedEffect!, _graphicsResourceCreator)
            {
                DiffuseColour = DiffuseColour,
                SpecularColour = SpecularColour,
            };
        }

        public void Apply(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            _effect.Projection = commonShaderParameters.Projection;
            _effect.View = commonShaderParameters.View;
            _effect.World = modelMatrix;

            _effect.DiffuseColor = DiffuseColour;
            _effect.SpecularColor = SpecularColour;
            if (_enableDefaultLighting)
                _effect.EnableDefaultLighting();

            _effect.CurrentTechnique.Passes[0].Apply();
        }

        Effect GetEffect() => _effect;

        public void Dispose()
        {
            _graphicsResourceCreator.DisposeTracked(_effect);
        }

        public void SetTechnique(RenderingTechnique technique)
        {
            // Only one supported, no need to change
        }

        public bool SupportsTechnique(RenderingTechnique technique)
        {
            var supported = new[] { RenderingTechnique.Normal };
            if (supported.Contains(technique))
                return true;
            return false;
        }
    }
}
