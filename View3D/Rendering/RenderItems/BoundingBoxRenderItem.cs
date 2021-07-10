using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering.RenderItems
{

    public class BoundingBoxRenderItem : IRenderItem
    {
        Effect _shader;
        BoundingBox _bb;

        public BoundingBoxRenderItem(Effect shader, BoundingBox bb)
        {
            _shader = shader;
            _bb = bb;
        }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            Render(device, parameters, Matrix.Identity);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            var _originalVertecies = new VertexPositionColor[24];
            var corners = _bb.GetCorners();
            Color colour = Color.Red;

            _originalVertecies[0] = new VertexPositionColor(corners[0], colour);
            _originalVertecies[1] = new VertexPositionColor(corners[1], colour);

            _originalVertecies[2] = new VertexPositionColor(corners[1], colour);
            _originalVertecies[3] = new VertexPositionColor(corners[2], colour);

            _originalVertecies[4] = new VertexPositionColor(corners[2], colour);
            _originalVertecies[5] = new VertexPositionColor(corners[3], colour);

            _originalVertecies[6] = new VertexPositionColor(corners[3], colour);
            _originalVertecies[7] = new VertexPositionColor(corners[0], colour);


            var offset = 4;
            _originalVertecies[8] = new VertexPositionColor(corners[0 + offset], colour);
            _originalVertecies[9] = new VertexPositionColor(corners[1 + offset], colour);

            _originalVertecies[10] = new VertexPositionColor(corners[1 + offset], colour);
            _originalVertecies[11] = new VertexPositionColor(corners[2 + offset], colour);

            _originalVertecies[12] = new VertexPositionColor(corners[2 + offset], colour);
            _originalVertecies[13] = new VertexPositionColor(corners[3 + offset], colour);

            _originalVertecies[14] = new VertexPositionColor(corners[3 + offset], colour);
            _originalVertecies[15] = new VertexPositionColor(corners[0 + offset], colour);

            
            _originalVertecies[16] = new VertexPositionColor(corners[0], colour);
            _originalVertecies[17] = new VertexPositionColor(corners[0 + offset], colour);

            _originalVertecies[18] = new VertexPositionColor(corners[1], colour);
            _originalVertecies[19] = new VertexPositionColor(corners[1 + offset], colour);

            _originalVertecies[20] = new VertexPositionColor(corners[2], colour);
            _originalVertecies[21] = new VertexPositionColor(corners[2 + offset], colour);

            _originalVertecies[22] = new VertexPositionColor(corners[3], colour);
            _originalVertecies[23] = new VertexPositionColor(corners[3 + offset], colour);


            _shader.Parameters["View"].SetValue(commonShaderParameters.View);
            _shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);

            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertecies.ToArray(), 0, _originalVertecies.Count() / 2);
            }
        }
    }
}
