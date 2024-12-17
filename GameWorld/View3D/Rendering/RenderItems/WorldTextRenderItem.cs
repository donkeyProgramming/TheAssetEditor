using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class WorldTextRenderItem : IRenderItem
    {
        Vector3 _pos;

        readonly RenderEngineComponent _resourceLib;
        readonly string _text;
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public WorldTextRenderItem(RenderEngineComponent resourceLib, string text, Vector3 pos)
        {
            _resourceLib = resourceLib;
            _text = text;
            _pos = pos;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {
            var colour = Color.Red;
            float x = 1;

            var measure = _resourceLib.DefaultFont.MeasureString(_text);
            var viewport = device.Viewport;

            var position2d = viewport.Project(_pos, parameters.Projection, parameters.View, ModelMatrix);
            var centeredPosition = new Vector2(position2d.X - measure.X / 2, position2d.Y - measure.Y / 2);
            var scale = 1.0f / (_pos - parameters.CameraPosition).Length();

            x = 0;
            _resourceLib.CommonSpriteBatch.DrawString(_resourceLib.DefaultFont, _text, centeredPosition + new Vector2(measure.X * 0.5f, measure.Y * 0.5f), colour, x, new Vector2(measure.X * 0.5f, measure.Y * 0.5f), scale * 5, SpriteEffects.None, 0.99f);
            x += 0.05f;
        }
    }
}
