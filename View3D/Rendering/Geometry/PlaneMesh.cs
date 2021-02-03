using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public class PlaneMesh : IGeometry
    {
        private VertexBuffer _vertexBuffer;
        private VertexDeclaration _vertexDeclaration;
        VertexPositionNormalTexture[] _vertexData;

        public PlaneMesh(GraphicsDevice device)
        {
            _vertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );


            Vector3 topLeftFront = new Vector3(-1.0f, 1.0f, 1.0f);
            Vector3 bottomLeftFront = new Vector3(-1.0f, -1.0f, 1.0f);
            Vector3 topRightFront = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 bottomRightFront = new Vector3(1.0f, -1.0f, 1.0f);


            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);

            // Front face.
            _vertexData = new VertexPositionNormalTexture[6];
            _vertexData[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            _vertexData[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexData[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            _vertexData[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexData[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            _vertexData[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            _vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexData.Length, BufferUsage.None);
            _vertexBuffer.SetData(_vertexData);
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _vertexDeclaration?.Dispose();
        }

        public float? Intersect(Ray ray, Matrix modelMatrix)
        {
            var bb = new BoundingBox(new Vector3(-1), new Vector3(1));

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            return ray.Intersects(bb);
        }

        public bool IntersectFace(Ray ray, Matrix modelMatrix, out int? face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _vertexData.Length; i += 3)
            {
                var res = IntersectionMath.MollerTrumboreIntersection(ray, _vertexData[i].Position, _vertexData[i + 1].Position, _vertexData[i + 2].Position, out var intersectionPoint);
                if (res)
                {
                    var dist = intersectionPoint;
                    if (dist < bestDistance)
                    {
                        faceIndex = i;
                        bestDistance = dist.Value;
                    }
                }
            }

            if (faceIndex == -1)
                return false;

            face = faceIndex;
            return true;
        }

        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, _vertexBuffer.VertexCount / 3);
            }
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawPrimitives(PrimitiveType.TriangleList, item, 1);
            }
        }

        public Vector3 GetVertex(int index)
        {
            throw new NotImplementedException();
        }

        public int VertexCount()
        {
            throw new NotImplementedException();
        }
    }
}
