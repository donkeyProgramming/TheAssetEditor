using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.Rendering
{
    public class LineMeshRender
    {
        Effect _shader;
        VertexPosition[] _originalVertecies;

        public LineMeshRender(Effect effect)
        {
            _shader = effect;
        }

        public LineMeshRender(ContentManager content)
        {
            _shader = content.Load<Effect>("Shaders\\LineShader");
        }

        public void CreateLineList((Vector3, Vector3)[] lines)
        {
            _originalVertecies = new VertexPosition[lines.Length * 2];
            for (int i = 0; i < lines.Length; i++)
            {
                _originalVertecies[i * 2] = new VertexPosition(lines[i].Item1);
                _originalVertecies[i * 2 + 1] = new VertexPosition(lines[i].Item2);
            }
        }

        public void CreateGrid()
        {
            int lineCount = 10;
            float spacing = 1;
            float length = 10;
            float offset = (lineCount * spacing) / 2;

            var list = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= lineCount; i++)
            {
                var start = new Vector3((i * spacing) - offset, 0, -length * 0.5f);
                var stop = new Vector3((i * spacing) - offset, 0, length * 0.5f);
                list.Add((start, stop));
            }

            for (int i = 0; i <= lineCount; i++)
            {
                var start = new Vector3(-length * 0.5f, 0, (i * spacing) - offset);
                var stop = new Vector3(length * 0.5f, 0, (i * spacing) - offset);
                list.Add((start, stop));
            }
            CreateLineList(list.ToArray());
        }

        public void CreateFromBoundingBox(BoundingBox b)
        {
            var corners = b.GetCorners();
            var data = new (Vector3, Vector3)[12];
            data[0] = (corners[0], corners[1]);
            data[1] = (corners[2], corners[3]);
            data[2] = (corners[0], corners[3]);
            data[3] = (corners[1], corners[2]);

            data[4] = (corners[4], corners[5]);
            data[5] = (corners[6], corners[7]);
            data[6] = (corners[4], corners[7]);
            data[7] = (corners[5], corners[6]);

            data[8] = (corners[0], corners[4]);
            data[9] = (corners[1], corners[5]);
            data[10] = (corners[2], corners[6]);
            data[11] = (corners[3], corners[7]);

            CreateLineList(data);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix ModelMatrix)
        {
            _shader.Parameters["View"].SetValue(commonShaderParameters.View);
            _shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            _shader.Parameters["World"].SetValue(ModelMatrix);

            foreach (var pass in _shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertecies, 0, _originalVertecies.Count() / 2);
            }
        }
    }
}
