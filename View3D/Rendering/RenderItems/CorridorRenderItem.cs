using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class CorridorRenderItem :IRenderItem
    {
        Effect _shader;
        Vector3 _startPos;
        Vector3 _endPos;
        private float _width;
        Color _colour = Color.Red;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float width)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _width = width;
        }

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float width, Color color)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _width = width;
            _colour = color;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            float halfWidth = _width / 2;
            Vector3 normal = _endPos - _startPos;
            normal.Normalize();
            Vector3 vectorZ = new Vector3(0, 0, 1);
            Vector3 planeVectorZ = Vector3.Cross(normal, Vector3.Cross(vectorZ, normal));
            Vector3 planeVectorZN = Vector3.Cross(vectorZ, normal);
            // Vector3 halfWidthVector = Vector3.Normalize(planeVectorZ + planeVectorZN) * _width / 2;
            
            var fullCircle = 2 * MathF.PI;
            var steps = 20;
            var stepsSize = fullCircle / steps;

            var _startCircleVertecies = new VertexPositionColor[steps+1];
            var _endCircleVertecies = new VertexPositionColor[steps+1];
            var _connectionCircleVertecies =  new VertexPositionColor[2*(steps + 1)];
            for (int i = 0; i < steps + 1; i++)
            {
                float angle = stepsSize * i;
                Vector3 rotatedVector = planeVectorZ * MathF.Cos(angle) + planeVectorZN * MathF.Sin(angle);
                rotatedVector.Normalize();
                _startCircleVertecies[i] = new VertexPositionColor(_startPos + rotatedVector * halfWidth, _colour);
                _endCircleVertecies[i] = new VertexPositionColor(_endPos + rotatedVector * halfWidth, _colour);
                _connectionCircleVertecies[2*i] = new VertexPositionColor(_startCircleVertecies[i].Position, _colour);
                _connectionCircleVertecies[2*i+1] = new VertexPositionColor(_endCircleVertecies[i].Position, _colour);
            }

            _shader.Parameters["View"].SetValue(parameters.View);
            _shader.Parameters["Projection"].SetValue(parameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);
        
            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineStrip, _startCircleVertecies, 0, _startCircleVertecies.Count()-1);
                device.DrawUserPrimitives(PrimitiveType.LineStrip, _endCircleVertecies, 0, _endCircleVertecies.Count()-1);
                device.DrawUserPrimitives(PrimitiveType.LineList, _connectionCircleVertecies, 0, _connectionCircleVertecies.Count() / 2);
            }
        }
    }
}