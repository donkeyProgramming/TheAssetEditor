using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class CricleRenderItem : IRenderItem
    {
        Effect _shader;
        Vector3 _pos;
        float _size;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public CricleRenderItem(Effect shader, Vector3 pos, float size)
        {
            _shader = shader;
            _pos = pos;
            _size = size;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Render(device, parameters, ModelMatrix);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            Color colour = Color.Red;

            var fullCircle = 2 * MathF.PI;
            var steps = 20;
            var stepsSize = fullCircle / steps;

            var _originalVertecies = new VertexPositionColor[steps+1];

            for (int i = 0; i < steps+1; i++)
            {
                var x = _pos.X + _size * MathF.Cos(stepsSize * i);
                var z = _pos.Z + _size * MathF.Sin(stepsSize * i);

                _originalVertecies[i] = new VertexPositionColor(new Vector3(x, _pos.Y, z), colour);
            }

            _shader.Parameters["View"].SetValue(commonShaderParameters.View);
            _shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);

            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineStrip, _originalVertecies, 0, steps);
            }
        }
    }
}
