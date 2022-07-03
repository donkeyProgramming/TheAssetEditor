using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using SharpDX;
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
        private float _coneAngle; // in degrees
        Color _colour = Color.Red;
        private VertexPositionColor[][] _circles;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public ConeRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float coneAngle)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _coneAngle = coneAngle;
            Init();
        }

        public ConeRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, float coneAngle, Color color)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _coneAngle = coneAngle;
            _colour = color;
            Init();
        }

        private void Init()
        {
            float halfAngle = MathHelper.ToRadians(_coneAngle / 2);
            Vector3 normal = _endPos - _startPos;
            normal.Normalize();
            float distance = Vector3.Distance(_startPos, _endPos);
            var random = new Random();
            Vector3 vectorP = new Vector3(RandomUtil.NextFloat(random, -1.0f, 1.0f), RandomUtil.NextFloat(random, -1.0f, 1.0f), RandomUtil.NextFloat(random, -1.0f, 1.0f));
            vectorP.Normalize();
            
            Vector3 planeVectorP = Vector3.Cross(normal, Vector3.Cross(vectorP, normal));
            Vector3 planeVectorPN = Vector3.Cross(vectorP, normal);
            planeVectorP.Normalize();
            planeVectorPN.Normalize();

            float sectorAngleStep = MathF.PI / 12; // 15 degree sectors
            int sectorCount = (int)Math.Floor((halfAngle - sectorAngleStep / 2) / sectorAngleStep);

            var fullCircle = 2 * MathF.PI;
            var steps = 20;
            var stepsSize = fullCircle / steps;
            
            // Array of {#sectorCount} circles of {#steps+1} points each
            _circles = new VertexPositionColor[sectorCount][];
            for (int i = 0; i < sectorCount; i++)
            {
                _circles[i] = new VertexPositionColor[steps + 1];
                float sectorAngle = sectorAngleStep * (i + 1);
                
                for (int j = 0; j < steps + 1; j++)
                {
                    float angle = stepsSize * j;
                    Vector3 rotatedVector = normal * MathF.Cos(sectorAngle) + planeVectorP * MathF.Sin(sectorAngle) * MathF.Sin(angle) + planeVectorPN * MathF.Cos(angle);
                    rotatedVector.Normalize();
                    _circles[i][j] = new VertexPositionColor(_startPos + rotatedVector * distance, _colour);
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
                for (int i = 0; i < _circles.Length; i++)
                {
                    device.DrawUserPrimitives(PrimitiveType.LineStrip, _circles[i], 0, _circles[i].Count()-1);
                }
            }
        }
    }
}