using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    internal class TextureRenderItem : IRenderItem
    {
        private readonly RenderEngineComponent _renderEngineComponent;

        private readonly Texture2D _texture;
        private readonly Vector2 _position;
        private readonly Color _color;
        private readonly float _roation;
        private readonly Vector2 _origin;
        private readonly Vector2 _scale;

        public TextureRenderItem(RenderEngineComponent renderEngineComponent, 
            Texture2D texture,
            Vector2 position,
            Color color,
            float roation,
            Vector2 origin,
            Vector2 scale)
        {
            _renderEngineComponent = renderEngineComponent;
            _texture = texture;
            _position = position;
            _color = color;
            _roation = roation;
            _origin = origin;
            _scale = scale;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            _renderEngineComponent.CommonSpriteBatch.Draw(
                _texture,
                _position,
                null,
                _color,
                _roation,
                _origin,
                _scale,
                SpriteEffects.None,
                0
            );
        }
    }
}
