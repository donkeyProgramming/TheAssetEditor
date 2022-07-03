using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{
    public class LineSegmentRenderItem: IRenderItem
    {
        Effect _shader;
        Vector3 _startPos;
        Vector3 _endPos;
        Color _colour = Color.Red;

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;


        public LineSegmentRenderItem(Effect shader, Vector3 startPos, Vector3 endPos)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
        }
        
        public LineSegmentRenderItem(Effect shader, Vector3 startPos, Vector3 endPos, Color color)
        {
            _shader = shader;
            _startPos = startPos;
            _endPos = endPos;
            _colour = color;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            var _originalVertecies = new VertexPositionColor[2];
            _originalVertecies[0] = new VertexPositionColor(_startPos, _colour);
            _originalVertecies[1] = new VertexPositionColor(_endPos, _colour);

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