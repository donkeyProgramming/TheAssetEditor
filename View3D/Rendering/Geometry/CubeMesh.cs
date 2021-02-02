using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public class CubeMesh : IGeometry
    {

        private VertexBuffer _vertexBuffer;
        private VertexDeclaration _vertexDeclaration;
        public VertexBuffer VertexBuffer { get { return _vertexBuffer; } }

        VertexPositionNormalTexture[] _vertexData;

        public CubeMesh(GraphicsDevice device)
        {
            _vertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );
            _vertexBuffer = CreateCube(device, 1);
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _vertexDeclaration?.Dispose();
        }

        VertexBuffer CreateCube(GraphicsDevice device, float scale)
        {
            Vector3 topLeftFront = new Vector3(-1.0f, 1.0f, 1.0f) * scale;
            Vector3 bottomLeftFront = new Vector3(-1.0f, -1.0f, 1.0f) * scale;
            Vector3 topRightFront = new Vector3(1.0f, 1.0f, 1.0f) * scale;
            Vector3 bottomRightFront = new Vector3(1.0f, -1.0f, 1.0f) * scale;
            Vector3 topLeftBack = new Vector3(-1.0f, 1.0f, -1.0f) * scale;
            Vector3 topRightBack = new Vector3(1.0f, 1.0f, -1.0f) * scale;
            Vector3 bottomLeftBack = new Vector3(-1.0f, -1.0f, -1.0f) * scale;
            Vector3 bottomRightBack = new Vector3(1.0f, -1.0f, -1.0f) * scale;

            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            _vertexData = new VertexPositionNormalTexture[36];

            // Front face.
            _vertexData[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            _vertexData[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexData[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            _vertexData[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            _vertexData[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            _vertexData[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // Back face.
            _vertexData[6] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            _vertexData[7] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            _vertexData[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            _vertexData[9] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            _vertexData[10] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            _vertexData[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // Top face.
            _vertexData[12] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            _vertexData[13] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            _vertexData[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            _vertexData[15] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            _vertexData[16] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            _vertexData[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // Bottom face.
            _vertexData[18] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            _vertexData[19] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            _vertexData[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            _vertexData[21] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            _vertexData[22] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            _vertexData[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // Left face.
            _vertexData[24] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            _vertexData[25] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            _vertexData[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            _vertexData[27] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            _vertexData[28] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            _vertexData[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // Right face.
            _vertexData[30] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            _vertexData[31] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            _vertexData[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            _vertexData[33] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            _vertexData[34] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            _vertexData[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            var vertexBuffer = new VertexBuffer(device, _vertexDeclaration, _vertexData.Length, BufferUsage.None);
            vertexBuffer.SetData(_vertexData);
            return vertexBuffer;
        }

        public float? Intersect(Ray ray, Matrix modelMatrix)
        {
            var bb = new BoundingBox(new Vector3(-1), new Vector3(1));

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            return ray.Intersects(bb);
        }

        public bool IntersectFace(Ray ray, Matrix modelMatrix, out FaceSelection face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _vertexData.Length; i += 3)
            {
                var res = IntersectionMath.MollerTrumboreIntersection(ray, _vertexData[i].Position, _vertexData[i+1].Position, _vertexData[i+2].Position, out var intersectionPoint);
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

            face = new FaceSelection(faceIndex);
            return true;
        }

        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            device.SetVertexBuffer(VertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
            }
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, FaceSelection faceSelection)
        {
            device.SetVertexBuffer(VertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach(var item in faceSelection.SelectedFaces)
                    device.DrawPrimitives(PrimitiveType.TriangleList, item, 1);
            }
        }
    }

}
