using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace GameWorld.Core.Rendering.RenderItems
{
    public class LocatorRenderItem : IRenderItem
    {
        Effect _shader;
        Vector3 _pos;
        float _size;
        Color _colour = Color.Red;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public LocatorRenderItem(Effect shader, Vector3 pos, float size)
        {
            _shader = shader;
            _pos = pos;
            _size = size;
        }

        public LocatorRenderItem(Effect shader, Vector3 pos, float size, Color color)
        {
            _shader = shader;
            _pos = pos;
            _size = size;
            _colour = color;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            var _originalVertecies = new VertexPositionColor[6];

            var halfLength = _size / 2;
            _originalVertecies[0] = new VertexPositionColor(_pos + new Vector3(-halfLength, 0, 0), _colour);
            _originalVertecies[1] = new VertexPositionColor(_pos + new Vector3(halfLength, 0, 0), _colour);
            _originalVertecies[2] = new VertexPositionColor(_pos + new Vector3(0, -halfLength, 0), _colour);
            _originalVertecies[3] = new VertexPositionColor(_pos + new Vector3(0, halfLength, 0), _colour);
            _originalVertecies[4] = new VertexPositionColor(_pos + new Vector3(0, 0, -halfLength), _colour);
            _originalVertecies[5] = new VertexPositionColor(_pos + new Vector3(0, 0, halfLength), _colour);

            _shader.Parameters["View"].SetValue(parameters.View);
            _shader.Parameters["Projection"].SetValue(parameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);

            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertecies, 0, _originalVertecies.Count() / 2);
            }
        }
    }
}
