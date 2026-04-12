using CommunityToolkit.Diagnostics;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Grid
{
    public class GridRenderItem : IRenderItem
    {
        readonly private Effect _gridEffect;
        private readonly VertexPositionTexture[] _quadVertices = new VertexPositionTexture[4];

        private float _cameraDist;
        private bool _isOrthographic;
        private Vector3 _gridColur = new(0f, 0f, 0f);


        public GridRenderItem(Effect effect)
        {
            Guard.IsNotNull(effect);
            _gridEffect = effect;
        }

        public void Update(ArcBallCamera camera, Vector3 gridColur)
        {
            // Calculate quad size based on camera distance
            _cameraDist = Vector3.Distance(camera.Position, camera.LookAt);
            if (camera.CurrentProjectionType == ProjectionType.Orthographic)
                _cameraDist = camera.OrthoSize;

            var halfSize = Math.Clamp(_cameraDist * 5.0f, 25f, 8000f);

            // Snap quad center to integer grid positions (camera following)
            var cx = (float)Math.Round(camera.Position.X);
            var cz = (float)Math.Round(camera.Position.Z);

            // Build ground plane quad at Y=0 (triangle strip order)
            _quadVertices[0] = new VertexPositionTexture(new Vector3(cx - halfSize, 0, cz + halfSize), Vector2.Zero);
            _quadVertices[1] = new VertexPositionTexture(new Vector3(cx + halfSize, 0, cz + halfSize), Vector2.Zero);
            _quadVertices[2] = new VertexPositionTexture(new Vector3(cx - halfSize, 0, cz - halfSize), Vector2.Zero);
            _quadVertices[3] = new VertexPositionTexture(new Vector3(cx + halfSize, 0, cz - halfSize), Vector2.Zero);

            _isOrthographic = camera.CurrentProjectionType == ProjectionType.Orthographic;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique)
        {

            _gridEffect.Parameters["World"].SetValue(Matrix.Identity);
            _gridEffect.Parameters["View"].SetValue(parameters.View);
            _gridEffect.Parameters["Projection"].SetValue(parameters.Projection);
            _gridEffect.Parameters["CameraPosition"].SetValue(parameters.CameraPosition);
            _gridEffect.Parameters["GridColor"].SetValue(_gridColur);
            _gridEffect.Parameters["CameraDistance"].SetValue(_cameraDist);
            _gridEffect.Parameters["IsOrthographic"].SetValue(_isOrthographic ? 1 : 0);
            _gridEffect.Techniques["Grid"].Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quadVertices, 0, 2);
        }
    }   
}
