using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Misc;

namespace GameWorld.Core.Rendering
{
    public static class LineHelper
    {

        public static VertexPositionColor[] CreateCube(Matrix transform, Color color)
        {
            var output = new VertexPositionColor[24];

            var offset = new Vector3(-0.5f, -0.5f, -0.5f);
            var a0 = Vector3.Transform(new Vector3(0, 0, 0) + offset, transform);
            var b0 = Vector3.Transform(new Vector3(1, 0, 0) + offset, transform);
            var c0 = Vector3.Transform(new Vector3(1, 1, 0) + offset, transform);
            var d0 = Vector3.Transform(new Vector3(0, 1, 0) + offset, transform);

            var a1 = Vector3.Transform(new Vector3(0, 0, 1) + offset, transform);
            var b1 = Vector3.Transform(new Vector3(1, 0, 1) + offset, transform);
            var c1 = Vector3.Transform(new Vector3(1, 1, 1) + offset, transform);
            var d1 = Vector3.Transform(new Vector3(0, 1, 1) + offset, transform);

            var index = 0;
            output[index++] = new VertexPositionColor(a0, color);
            output[index++] = new VertexPositionColor(b0, color);
            output[index++] = new VertexPositionColor(c0, color);
            output[index++] = new VertexPositionColor(d0, color);
            output[index++] = new VertexPositionColor(b0, color);
            output[index++] = new VertexPositionColor(c0, color);
            output[index++] = new VertexPositionColor(a0, color);
            output[index++] = new VertexPositionColor(d0, color);

            output[index++] = new VertexPositionColor(a1, color);
            output[index++] = new VertexPositionColor(b1, color);
            output[index++] = new VertexPositionColor(c1, color);
            output[index++] = new VertexPositionColor(d1, color);
            output[index++] = new VertexPositionColor(b1, color);
            output[index++] = new VertexPositionColor(c1, color);
            output[index++] = new VertexPositionColor(a1, color);
            output[index++] = new VertexPositionColor(d1, color);

            output[index++] = new VertexPositionColor(a0, color);
            output[index++] = new VertexPositionColor(a1, color);
            output[index++] = new VertexPositionColor(b0, color);
            output[index++] = new VertexPositionColor(b1, color);
            output[index++] = new VertexPositionColor(c0, color);
            output[index++] = new VertexPositionColor(c1, color);
            output[index++] = new VertexPositionColor(d0, color);
            output[index++] = new VertexPositionColor(d1, color);

            return output;
        }

        public static VertexPositionColor[] CreateGrid()
        {
            const int LineCount = 10;
            const float Spacing = 1;
            const float Length = 10;
            const float Offset = LineCount * Spacing / 2;

            var list = new List<(Vector3, Vector3)>();
            for (var i = 0; i <= LineCount; i++)
            {
                var start = new Vector3(i * Spacing - Offset, 0, -Length * 0.5f);
                var stop = new Vector3(i * Spacing - Offset, 0, Length * 0.5f);
                list.Add((start, stop));
            }

            for (var i = 0; i <= LineCount; i++)
            {
                var start = new Vector3(-Length * 0.5f, 0, i * Spacing - Offset);
                var stop = new Vector3(Length * 0.5f, 0, i * Spacing - Offset);
                list.Add((start, stop));
            }


            var output = new List<VertexPositionColor>();
            for (var i = 0; i < list.Count; i++)
            {
                output.Add(new VertexPositionColor(list[i].Item1, Color.Black));
                output.Add(new VertexPositionColor(list[i].Item2, Color.Black));
            }

            return output.ToArray();
        }

        public static VertexPositionColor[] AddBoundingBox(BoundingBox b, Color color, Vector3 offset)
        {
            var output = new VertexPositionColor[24];
            var corners = b.GetCorners();
            var index = 0;

            output[index++] = new VertexPositionColor(corners[0] + offset, color); output[index++] = new VertexPositionColor(corners[1] + offset, color);
            output[index++] = new VertexPositionColor(corners[2] + offset, color); output[index++] = new VertexPositionColor(corners[3] + offset, color);
            output[index++] = new VertexPositionColor(corners[0] + offset, color); output[index++] = new VertexPositionColor(corners[3] + offset, color);
            output[index++] = new VertexPositionColor(corners[1] + offset, color); output[index++] = new VertexPositionColor(corners[2] + offset, color);

            output[index++] = new VertexPositionColor(corners[4] + offset, color); output[index++] = new VertexPositionColor(corners[5] + offset, color);
            output[index++] = new VertexPositionColor(corners[6] + offset, color); output[index++] = new VertexPositionColor(corners[7] + offset, color);
            output[index++] = new VertexPositionColor(corners[4] + offset, color); output[index++] = new VertexPositionColor(corners[7] + offset, color);
            output[index++] = new VertexPositionColor(corners[5] + offset, color); output[index++] = new VertexPositionColor(corners[6] + offset, color);

            output[index++] = new VertexPositionColor(corners[0] + offset, color); output[index++] = new VertexPositionColor(corners[4] + offset, color);
            output[index++] = new VertexPositionColor(corners[1] + offset, color); output[index++] = new VertexPositionColor(corners[5] + offset, color);
            output[index++] = new VertexPositionColor(corners[2] + offset, color); output[index++] = new VertexPositionColor(corners[6] + offset, color);
            output[index++] = new VertexPositionColor(corners[3] + offset, color); output[index++] = new VertexPositionColor(corners[7] + offset, color);

            return output;
        }

        public static VertexPositionColor[] AddLocator(Vector3 pos, float size, Color color)
        {
            var vertices = new VertexPositionColor[6];
            var halfLength = size / 2;
            vertices[0] = new VertexPositionColor(pos + new Vector3(-halfLength, 0, 0), color);
            vertices[1] = new VertexPositionColor(pos + new Vector3(halfLength, 0, 0), color);
            vertices[2] = new VertexPositionColor(pos + new Vector3(0, -halfLength, 0), color);
            vertices[3] = new VertexPositionColor(pos + new Vector3(0, halfLength, 0), color);
            vertices[4] = new VertexPositionColor(pos + new Vector3(0, 0, -halfLength), color);
            vertices[5] = new VertexPositionColor(pos + new Vector3(0, 0, halfLength), color);
            
            return vertices;
        }

        private static IEnumerable<(int, float, float)> CircleAnglesGenerator(int steps = 20)
        {
            var stepSize = 2 * MathF.PI / steps;
            for (var i = 0; i < steps + 1; i++)
                yield return (i, MathF.Cos(stepSize * i), MathF.Sin(stepSize * i));
        }

        private static Vector3[] CreateCircle(int steps = 20)
        {
            var vertices = new Vector3[2 * (steps + 1)];
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                vertices[2 * i] = new Vector3(cos, sin, 0);
                vertices[2 * i + 1] = vertices[2 * i];
            }
            // fix points to use with LineList
            for (var i = 0; i < steps; i++)
                vertices[2 * i + 1] = vertices[2 * i + 2];
            return vertices;
        }

        private static Vector3[] CreateConeCircle(float sectorAngle, int steps = 20)
        {
            var vertices = new Vector3[2 * (steps + 1)];
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                vertices[2 * i] = new Vector3(sin * MathF.Sin(sectorAngle), cos * MathF.Sin(sectorAngle), MathF.Cos(sectorAngle));
                vertices[2 * i + 1] = vertices[2 * i];
            }
            // fix points to use with LineList
            for (var i = 0; i < steps; i++)
            {
                vertices[2 * i + 1] = vertices[2 * i + 2];
            }
            return vertices;
        }

        public static VertexPositionColor[] AddCircle(Vector3 pos, float size, Color color)
        {
            const int Steps = 20;
            var vertices = new VertexPositionColor[2 * (Steps + 1)];

            // TODO: implement using matrix and CreateCircle
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                var x = pos.X + size * cos;
                var z = pos.Z + size * sin;
                vertices[2 * i] = new VertexPositionColor(new Vector3(x, pos.Y, z), color);
                vertices[2 * i + 1] = vertices[2 * i];
            }

            // fix points
            for (var i = 0; i < Steps; i++)
                vertices[2 * i + 1] = vertices[2 * i + 2];

            return vertices;
        }

        public static VertexPositionColor[] AddCorridorSplash(Vector3 startPos, Vector3 endPos, Matrix transformationM, Color color, int steps = 30)
        {
            var diffVector = endPos - startPos;
            var circle = CreateCircle(steps);
            Vector3.Transform(circle, ref transformationM, circle);
            var startCircleVertices = circle.Select(v => new VertexPositionColor(v, color)).ToArray();
            var endCircleVertices = circle.Select(v => new VertexPositionColor(v + diffVector, color)).ToArray();
            var connectionCircleVertices = new VertexPositionColor[circle.Length];
            var edgesVertices = new VertexPositionColor[2 * circle.Length];
            foreach (var i in Enumerable.Range(0, circle.Length / 2))
            {
                connectionCircleVertices[2 * i] = new VertexPositionColor(startCircleVertices[2 * i].Position, color);
                connectionCircleVertices[2 * i + 1] = new VertexPositionColor(endCircleVertices[2 * i].Position, color);
                edgesVertices[4 * i] = new VertexPositionColor(startPos, color);
                edgesVertices[4 * i + 1] = new VertexPositionColor(startCircleVertices[2 * i].Position, color);
                edgesVertices[4 * i + 2] = new VertexPositionColor(endPos, color);
                edgesVertices[4 * i + 3] = new VertexPositionColor(endCircleVertices[2 * i].Position, color);
            }

            var output = new List<VertexPositionColor>();
            output.AddRange(startCircleVertices);
            output.AddRange(endCircleVertices);
            output.AddRange(connectionCircleVertices);
            output.AddRange(edgesVertices);
            return output.ToArray();
        }

        public static VertexPositionColor[] AddConeSplash(Vector3 startPos, Vector3 endPos, Matrix transformationM, float coneAngleDegrees, Color color, int steps = 30, int angleSteps = 60)
        {
            var output = new List<VertexPositionColor>();
            var halfAngle = MathHelper.ToRadians(coneAngleDegrees / 2);

            var circleSteps = new List<float>();
            var angleStep = MathF.PI / angleSteps;
            for (var angle = 0f; angle < halfAngle; angle += angleStep)
            {
                circleSteps.Add(angle);
            }

            if (!MathUtil.CompareEqualFloats(coneAngleDegrees, 360.0f, 0.001f))
            {
                var lastCircleVectors = CreateConeCircle(halfAngle);
                Vector3.Transform(lastCircleVectors, ref transformationM, lastCircleVectors);
                var lastCircle = lastCircleVectors.Select(v => new VertexPositionColor(v, color)).ToArray();
                var rays = new VertexPositionColor[lastCircleVectors.Length];
                foreach (var j in Enumerable.Range(0, rays.Length / 2))
                {
                    rays[2 * j] = new VertexPositionColor(startPos, color);
                    rays[2 * j + 1] = new VertexPositionColor(lastCircle[2 * j].Position, color);
                }
                output.AddRange(lastCircle);
                output.AddRange(rays);
            }

            var coneCircleSize = 2 * (steps + 1);
            var circlesVectors = new Vector3[circleSteps.Count * coneCircleSize];
            foreach (var i in Enumerable.Range(0, circleSteps.Count))
            {
                var sectorAngle = circleSteps[i];
                var circleVectors = CreateConeCircle(sectorAngle);
                foreach (var j in Enumerable.Range(0, circleVectors.Length))
                {
                    circlesVectors[i * coneCircleSize + j] = circleVectors[j];
                }

            }
            Vector3.Transform(circlesVectors, ref transformationM, circlesVectors);
            var circles = circlesVectors.Select(v => new VertexPositionColor(v, color)).ToArray();
            output.AddRange(circles);
            return output.ToArray();
        }

        public static VertexPositionColor[] AddLine(Vector3 pos0, Vector3 pos1, Color colour)
        {
            var output = new VertexPositionColor[2];
            output[0] = new VertexPositionColor(pos0, colour);
            output[1] = new VertexPositionColor(pos1, colour);
            return output;
        }
    }

   /* public class LineMeshRender : IDisposable
    {
        private List<VertexPositionColor> _originalVertices = [];
        private readonly ResourceLibrary _resourceLibrary;

        public LineMeshRender(ResourceLibrary resourceLibrary)
        {
            _resourceLibrary = resourceLibrary;
        }

        public void Clear()
        {
            _originalVertices = [];
        }

        void CreateLineList(List<(Vector3, Vector3)> lines)
        {
            Clear();
            for (var i = 0; i < lines.Count; i++)
                AddLine(lines[i].Item1, lines[i].Item2);
        }

        public void AddCube(Matrix transform, Color color)
        {
            var offset = new Vector3(-0.5f, -0.5f, -0.5f);
            var a0 = Vector3.Transform(new Vector3(0, 0, 0) + offset, transform);
            var b0 = Vector3.Transform(new Vector3(1, 0, 0) + offset, transform);
            var c0 = Vector3.Transform(new Vector3(1, 1, 0) + offset, transform);
            var d0 = Vector3.Transform(new Vector3(0, 1, 0) + offset, transform);

            var a1 = Vector3.Transform(new Vector3(0, 0, 1) + offset, transform);
            var b1 = Vector3.Transform(new Vector3(1, 0, 1) + offset, transform);
            var c1 = Vector3.Transform(new Vector3(1, 1, 1) + offset, transform);
            var d1 = Vector3.Transform(new Vector3(0, 1, 1) + offset, transform);

            AddLine(a0, b0, color);
            AddLine(c0, d0, color);
            AddLine(b0, c0, color);
            AddLine(a0, d0, color);

            AddLine(a1, b1, color);
            AddLine(c1, d1, color);
            AddLine(b1, c1, color);
            AddLine(a1, d1, color);

            AddLine(a0, a1, color);
            AddLine(b0, b1, color);
            AddLine(c0, c1, color);
            AddLine(d0, d1, color);
        }

        public void AddLine(Vector3 pointA, Vector3 pointB)
        {
            _originalVertices.Add(new VertexPositionColor(pointA, Color.Black));
            _originalVertices.Add(new VertexPositionColor(pointB, Color.Black));
        }

        public void AddLine(Vector3 pointA, Vector3 pointB, Color color)
        {
            _originalVertices.Add(new VertexPositionColor(pointA, color));
            _originalVertices.Add(new VertexPositionColor(pointB, color));
        }

        public void CreateGrid()
        {
            const int LineCount = 10;
            const float Spacing = 1;
            const float Length = 10;
            const float Offset = LineCount * Spacing / 2;

            var list = new List<(Vector3, Vector3)>();
            for (var i = 0; i <= LineCount; i++)
            {
                var start = new Vector3(i * Spacing - Offset, 0, -Length * 0.5f);
                var stop = new Vector3(i * Spacing - Offset, 0, Length * 0.5f);
                list.Add((start, stop));
            }

            for (var i = 0; i <= LineCount; i++)
            {
                var start = new Vector3(-Length * 0.5f, 0, i * Spacing - Offset);
                var stop = new Vector3(Length * 0.5f, 0, i * Spacing - Offset);
                list.Add((start, stop));
            }
            CreateLineList(list);
        }

        public void AddBoundingBox(BoundingBox b)
        {
            var corners = b.GetCorners();
            AddLine(corners[0], corners[1]);
            AddLine(corners[2], corners[3]);
            AddLine(corners[0], corners[3]);
            AddLine(corners[1], corners[2]);

            AddLine(corners[4], corners[5]);
            AddLine(corners[6], corners[7]);
            AddLine(corners[4], corners[7]);
            AddLine(corners[5], corners[6]);

            AddLine(corners[0], corners[4]);
            AddLine(corners[1], corners[5]);
            AddLine(corners[2], corners[6]);
            AddLine(corners[3], corners[7]);
        }

        public void AddLocator(Vector3 pos, float size, Color color)
        {
            var vertices = new VertexPositionColor[6];
            var halfLength = size / 2;
            vertices[0] = new VertexPositionColor(pos + new Vector3(-halfLength, 0, 0), color);
            vertices[1] = new VertexPositionColor(pos + new Vector3(halfLength, 0, 0), color);
            vertices[2] = new VertexPositionColor(pos + new Vector3(0, -halfLength, 0), color);
            vertices[3] = new VertexPositionColor(pos + new Vector3(0, halfLength, 0), color);
            vertices[4] = new VertexPositionColor(pos + new Vector3(0, 0, -halfLength), color);
            vertices[5] = new VertexPositionColor(pos + new Vector3(0, 0, halfLength), color);
            _originalVertices.AddRange(vertices);
        }

        private IEnumerable<(int, float, float)> CircleAnglesGenerator(int steps = 20)
        {
            var stepSize = 2 * MathF.PI / steps;
            for (var i = 0; i < steps + 1; i++)
            {
                yield return (i, MathF.Cos(stepSize * i), MathF.Sin(stepSize * i));
            }
        }

        private Vector3[] CreateCircle(int steps = 20)
        {
            var vertices = new Vector3[2 * (steps + 1)];
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                vertices[2 * i] = new Vector3(cos, sin, 0);
                vertices[2 * i + 1] = vertices[2 * i];
            }
            // fix points to use with LineList
            for (var i = 0; i < steps; i++)
                vertices[2 * i + 1] = vertices[2 * i + 2];
            return vertices;
        }

        private Vector3[] CreateConeCircle(float sectorAngle, int steps = 20)
        {
            var vertices = new Vector3[2 * (steps + 1)];
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                vertices[2 * i] = new Vector3(sin * MathF.Sin(sectorAngle), cos * MathF.Sin(sectorAngle), MathF.Cos(sectorAngle));
                vertices[2 * i + 1] = vertices[2 * i];
            }
            // fix points to use with LineList
            for (var i = 0; i < steps; i++)
            {
                vertices[2 * i + 1] = vertices[2 * i + 2];
            }
            return vertices;
        }

        public void AddCircle(Vector3 pos, float size, Color color)
        {
            const int Steps = 20;
            var vertices = new VertexPositionColor[2 * (Steps + 1)];

            // TODO: implement using matrix and CreateCircle
            foreach (var (i, cos, sin) in CircleAnglesGenerator())
            {
                var x = pos.X + size * cos;
                var z = pos.Z + size * sin;
                vertices[2 * i] = new VertexPositionColor(new Vector3(x, pos.Y, z), color);
                vertices[2 * i + 1] = vertices[2 * i];
            }

            // fix points
            for (var i = 0; i < Steps; i++)
            {
                vertices[2 * i + 1] = vertices[2 * i + 2];
            }
            _originalVertices.AddRange(vertices);
        }

        public void AddCorridorSplash(Vector3 startPos, Vector3 endPos, Matrix transformationM, Color color, int steps = 30)
        {
            var diffVector = endPos - startPos;
            var circle = CreateCircle(steps);
            Vector3.Transform(circle, ref transformationM, circle);
            var startCircleVertices = circle.Select(v => new VertexPositionColor(v, color)).ToArray();
            var endCircleVertices = circle.Select(v => new VertexPositionColor(v + diffVector, color)).ToArray();
            var connectionCircleVertices = new VertexPositionColor[circle.Length];
            var edgesVertices = new VertexPositionColor[2 * circle.Length];
            foreach (var i in Enumerable.Range(0, circle.Length / 2))
            {
                connectionCircleVertices[2 * i] = new VertexPositionColor(startCircleVertices[2 * i].Position, color);
                connectionCircleVertices[2 * i + 1] = new VertexPositionColor(endCircleVertices[2 * i].Position, color);
                edgesVertices[4 * i] = new VertexPositionColor(startPos, color);
                edgesVertices[4 * i + 1] = new VertexPositionColor(startCircleVertices[2 * i].Position, color);
                edgesVertices[4 * i + 2] = new VertexPositionColor(endPos, color);
                edgesVertices[4 * i + 3] = new VertexPositionColor(endCircleVertices[2 * i].Position, color);
            }
            _originalVertices.AddRange(startCircleVertices);
            _originalVertices.AddRange(endCircleVertices);
            _originalVertices.AddRange(connectionCircleVertices);
            _originalVertices.AddRange(edgesVertices);
        }

        public void AddConeSplash(Vector3 startPos, Vector3 endPos, Matrix transformationM, float coneAngleDegrees, Color color, int steps = 30, int angleSteps = 60)
        {
            var halfAngle = MathHelper.ToRadians(coneAngleDegrees / 2);

            var circleSteps = new List<float>();
            var angleStep = MathF.PI / angleSteps;
            for (var angle = 0f; angle < halfAngle; angle += angleStep)
            {
                circleSteps.Add(angle);
            }

            if (!MathUtil.CompareEqualFloats(coneAngleDegrees, 360.0f, 0.001f))
            {
                var lastCircleVectors = CreateConeCircle(halfAngle);
                Vector3.Transform(lastCircleVectors, ref transformationM, lastCircleVectors);
                var lastCircle = lastCircleVectors.Select(v => new VertexPositionColor(v, color)).ToArray();
                var rays = new VertexPositionColor[lastCircleVectors.Length];
                foreach (var j in Enumerable.Range(0, rays.Length / 2))
                {
                    rays[2 * j] = new VertexPositionColor(startPos, color);
                    rays[2 * j + 1] = new VertexPositionColor(lastCircle[2 * j].Position, color);
                }
                _originalVertices.AddRange(lastCircle);
                _originalVertices.AddRange(rays);
            }

            var coneCircleSize = 2 * (steps + 1);
            var circlesVectors = new Vector3[circleSteps.Count * coneCircleSize];
            foreach (var i in Enumerable.Range(0, circleSteps.Count))
            {
                var sectorAngle = circleSteps[i];
                var circleVectors = CreateConeCircle(sectorAngle);
                foreach (var j in Enumerable.Range(0, circleVectors.Length))
                {
                    circlesVectors[i * coneCircleSize + j] = circleVectors[j];
                }

            }
            Vector3.Transform(circlesVectors, ref transformationM, circlesVectors);
            var circles = circlesVectors.Select(v => new VertexPositionColor(v, color)).ToArray();
            _originalVertices.AddRange(circles);
        }

        public void Render(GraphicsDevice device, CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            if (_originalVertices.Count != 0)
            {
                var shader = _resourceLibrary.GetStaticEffect(ShaderTypes.Line);

                shader.Parameters["View"].SetValue(commonShaderParameters.View);
                shader.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
                shader.Parameters["World"].SetValue(modelMatrix);

                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.LineList, _originalVertices.ToArray(), 0, _originalVertices.Count() / 2);
                }
            }
        }

        public void Dispose() => Clear();
    }*/
}
