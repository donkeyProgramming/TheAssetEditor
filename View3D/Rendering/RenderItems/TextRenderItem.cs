using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Rendering;
using View3D.Utility;

namespace View3D.Rendering.RenderItems
{
    public class TextRenderItem : IRenderItem
    {
        Vector3 _pos;

        ResourceLibary _resourceLib;
        string _text;
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;
        public TextRenderItem(ResourceLibary resourceLib, string text, Vector3 pos)
        {
            _resourceLib = resourceLib;
            _text = text;
            _pos = pos;

        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Render(device, parameters, ModelMatrix);
        }

        float x = 1;
        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            Color colour = Color.Red;

            var measure = _resourceLib.DefaultFont.MeasureString(_text);
            var viewport = device.Viewport;

           
            var position2d = viewport.Project(_pos, commonShaderParameters.Projection, commonShaderParameters.View, ModelMatrix);
            var centeredPosition = new Vector2(position2d.X - measure.X / 2, position2d.Y - measure.Y / 2);
            var scale = 1.0f / (_pos - commonShaderParameters.CameraPosition).Length();

            x = 0;
            _resourceLib.CommonSpriteBatch.DrawString(_resourceLib.DefaultFont, _text, centeredPosition + new Vector2(measure.X * 0.5f, measure.Y * 0.5f), colour, x, new Vector2(measure.X*0.5f, measure.Y* 0.5f), scale * 5, SpriteEffects.None, 0.99f);

            x += 0.05f;
        }
    }
}
