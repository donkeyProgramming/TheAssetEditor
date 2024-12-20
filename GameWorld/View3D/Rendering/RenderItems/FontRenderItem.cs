using System;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    internal class FontRenderItem : IRenderItem
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly string _text;
        private readonly Vector2 _position;
        private readonly Color _color;

        public Matrix ModelMatrix { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public FontRenderItem(RenderEngineComponent resourceLibrary, string text, Vector2 position, Color color)
        {
            _renderEngineComponent = resourceLibrary;
            _text = text;
            _position = position;
            _color = color;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            _renderEngineComponent.CommonSpriteBatch.DrawString(_renderEngineComponent.DefaultFont, _text, _position, _color);
        }
    }
}
