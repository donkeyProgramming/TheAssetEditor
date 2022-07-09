using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using View3D.Components.Rendering;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;


namespace View3D.Rendering.RenderItems
{
    public class ConeRenderItem :IRenderItem
    {
        Effect _shader;
        Vector3 _startPos;
        Vector3 _endPos;
        private float _coneAngleDegrees;
        Color _colour = Color.Red;
        private VertexPositionColor[] _circles; // Array of {#circleCount} circles X {#steps} points each X 2 (to draw it with 1 device.DrawUserPrimitives call using PrimitiveType.LineList)
        private VertexPositionColor[] _lastCircle;
        private VertexPositionColor[] _rays;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public ConeRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float coneAngleDegrees)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _coneAngleDegrees = coneAngleDegrees;
            Init();
        }

        public ConeRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float coneAngleDegrees, Color color): this(shader, startPos, endPos, coneAngleDegrees)
        {
            _colour = color;
        }

        private void Init()
        {
            float halfAngle = MathHelper.ToRadians(_coneAngleDegrees / 2);
            Vector3 normal = _endPos - _startPos;
            normal.Normalize();
            float distance = Vector3.Distance(_startPos, _endPos);
            var random = new Random();
            Func<Random, float> RandomFloat = random => (float)(2 * random.NextDouble() - 1);
            Vector3 vectorP = new Vector3(RandomFloat(random), RandomFloat(random), RandomFloat(random));
            vectorP.Normalize();
            
            Vector3 planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal));
            Vector3 planeVectorPN = Vector3.Cross(vectorP, normal);
            planeVectorP.Normalize();
            planeVectorPN.Normalize();

            var circleSteps = new List<float>();
            var angleStep = MathF.PI / 60;
            for (float angle = angleStep; angle < MathF.PI; angle += angleStep)
            {
                if (angle < halfAngle)
                {
                    circleSteps.Add(angle);
                }
            }
            int circleCount = circleSteps.Count;
            
            var fullCircle = 2 * MathF.PI;
            var steps = 20;
            var stepsSize = fullCircle / steps;

            if (!(Math.Abs(_coneAngleDegrees - 360.0f) < 0.001f))
            {
                _lastCircle = new VertexPositionColor[steps + 1];
                _rays = new VertexPositionColor[2 * (steps + 1)];
                for (int j = 0; j < steps + 1; j++)
                {
                    float angle = stepsSize * j;
                    Vector3 rotatedVector = normal * MathF.Cos(halfAngle) + planeVectorP * MathF.Sin(halfAngle) * MathF.Sin(angle) + planeVectorPN * MathF.Cos(angle) * MathF.Sin(halfAngle);
                    rotatedVector.Normalize();
                    _lastCircle[j] = new VertexPositionColor(_startPos + rotatedVector * distance, _colour);
                    _rays[2*j] = new VertexPositionColor(_startPos, _colour);
                    _rays[2*j+1] = new VertexPositionColor(_lastCircle[j].Position, _colour);
                }
            }
            
            _circles = new VertexPositionColor[circleCount * 2 * steps];
            for (int i = 0; i < circleCount; i++)
            {
                float sectorAngle = circleSteps[i];
                
                for (int j = 0; j < steps ; j++)
                {
                    float angle = stepsSize * j;
                    Vector3 rotatedVector = normal * MathF.Cos(sectorAngle) + planeVectorP * MathF.Sin(sectorAngle) * MathF.Sin(angle) + planeVectorPN * MathF.Cos(angle) * MathF.Sin(sectorAngle);
                    rotatedVector.Normalize();
                    _circles[i * (2* steps) + 2*j] = new VertexPositionColor(_startPos + rotatedVector * distance, _colour);
                    _circles[i * (2* steps) + 2*j + 1] = new VertexPositionColor(_startPos + rotatedVector * distance, _colour);
                }
                
                // fix points
                for (int j = 0; j < steps; j++)
                {
                    _circles[i * (2* steps) + 2 * j + 1] = _circles[i * (2* steps) + (2 * j + 2) % (2 * steps)];
                }
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
                device.DrawUserPrimitives(PrimitiveType.LineList, _circles, 0, _circles.Count()/2);
                if (!(Math.Abs(_coneAngleDegrees - 360.0f) < 0.001f))
                {
                    device.DrawUserPrimitives(PrimitiveType.LineStrip, _lastCircle, 0, _lastCircle.Count()-1);
                    device.DrawUserPrimitives(PrimitiveType.LineList, _rays, 0, _rays.Count()/ 2);
                }
            }
        }
    }
}