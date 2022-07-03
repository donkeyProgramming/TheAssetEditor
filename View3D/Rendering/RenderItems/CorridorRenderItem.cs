using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using SharpDX;
using View3D.Components.Rendering;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace View3D.Rendering.RenderItems
{
    public class CorridorRenderItem :IRenderItem
    {
        Effect _shader;
        Vector3 _startPos;
        Vector3 _endPos;
        private float _width;
        Color _colour = Color.Red;
        private float _halfWidth;
        private VertexPositionColor[] _startCircleVertecies;
        private VertexPositionColor[] _endCircleVertecies;
        private VertexPositionColor[] _connectionCircleVertecies;
        private VertexPositionColor[] _edgesVertecies;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float width)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _width = width;
            Init();
        }

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float width, Color color)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _width = width;
            _colour = color;
            Init();
        }
        
        private void Init()
        {
            _halfWidth = _width / 2;
            Vector3 normal = _endPos - _startPos;
            normal.Normalize();
            var random = new Random();
            Vector3 vectorP = new Vector3(RandomUtil.NextFloat(random, -1.0f, 1.0f), RandomUtil.NextFloat(random, -1.0f, 1.0f), RandomUtil.NextFloat(random, -1.0f, 1.0f));
            vectorP.Normalize();            
            Vector3 _planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal));
            Vector3 _planeVectorPN = Vector3.Cross(vectorP, normal);
            _planeVectorP.Normalize();
            _planeVectorPN.Normalize();
            
            var fullCircle = 2 * MathF.PI;
            var steps = 16;
            var stepsSize = fullCircle / steps;

            _startCircleVertecies = new VertexPositionColor[steps+1];
            _endCircleVertecies = new VertexPositionColor[steps+1];
            _connectionCircleVertecies = new VertexPositionColor[2*(steps + 1)];
            _edgesVertecies =  new VertexPositionColor[4*(steps + 1)];
            for (int i = 0; i < steps + 1; i++)
            {
                float angle = stepsSize * i;
                Vector3 rotatedVector = _planeVectorP * MathF.Cos(angle) + _planeVectorPN * MathF.Sin(angle);
                rotatedVector.Normalize();
                _startCircleVertecies[i] = new VertexPositionColor(_startPos + rotatedVector * _halfWidth, _colour);
                _endCircleVertecies[i] = new VertexPositionColor(_endPos + rotatedVector * _halfWidth, _colour);
                _connectionCircleVertecies[2*i] = new VertexPositionColor(_startCircleVertecies[i].Position, _colour);
                _connectionCircleVertecies[2*i+1] = new VertexPositionColor(_endCircleVertecies[i].Position, _colour);
                _edgesVertecies[4*i] = new VertexPositionColor(_startPos, _colour);
                _edgesVertecies[4*i+1] = new VertexPositionColor(_startCircleVertecies[i].Position, _colour);
                _edgesVertecies[4*i+2] = new VertexPositionColor(_endPos, _colour);
                _edgesVertecies[4*i+3] = new VertexPositionColor(_endCircleVertecies[i].Position, _colour);
            }
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            _shader.Parameters["View"].SetValue(parameters.View);
            _shader.Parameters["Projection"].SetValue(parameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);
        
            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineStrip, _startCircleVertecies, 0, _startCircleVertecies.Count()-1);
                device.DrawUserPrimitives(PrimitiveType.LineStrip, _endCircleVertecies, 0, _endCircleVertecies.Count()-1);
                device.DrawUserPrimitives(PrimitiveType.LineList, _connectionCircleVertecies, 0, _connectionCircleVertecies.Count() / 2);
                device.DrawUserPrimitives(PrimitiveType.LineList, _edgesVertecies, 0, _edgesVertecies.Count() / 2);
            }
        }
    }
}