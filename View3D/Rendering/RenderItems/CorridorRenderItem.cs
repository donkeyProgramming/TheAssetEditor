using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
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
        Matrix _transformationM;
        private float _width;
        Color _colour = Color.Red;
        private float _halfWidth;
        private VertexPositionColor[] _startCircleVertecies;
        private VertexPositionColor[] _endCircleVertecies;
        private VertexPositionColor[] _connectionCircleVertecies;
        private VertexPositionColor[] _edgesVertecies;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, Matrix transformationM,  float width)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _width = width;
            _transformationM = transformationM;
            Init();
        }

        public CorridorRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, Matrix transformationM, float width, Color color) : this(shader, startPos, endPos, transformationM, width)
        {
            _colour = color;
        }
        
        private void Init()
        {
            _halfWidth = _width / 2;
            Vector3 diffVector = _endPos - _startPos;

            var fullCircle = 2 * MathF.PI;
            var steps = 30;
            var stepsSize = fullCircle / steps;

            _startCircleVertecies = new VertexPositionColor[steps+1];
            _endCircleVertecies = new VertexPositionColor[steps+1];
            _connectionCircleVertecies = new VertexPositionColor[2*(steps + 1)];
            _edgesVertecies =  new VertexPositionColor[4*(steps + 1)];
            for (int i = 0; i < steps + 1; i++)
            {
                float angle = stepsSize * i;
                Vector3 trandformedVector = Vector3.Transform(new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0), _transformationM);
                _startCircleVertecies[i] = new VertexPositionColor(trandformedVector, _colour);
                _endCircleVertecies[i] = new VertexPositionColor(diffVector + trandformedVector, _colour);
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