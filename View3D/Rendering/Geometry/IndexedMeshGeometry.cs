using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Component;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public abstract class IndexedMeshGeometry : IGeometry
    {
        protected VertexBuffer _vertexBuffer;
        protected IndexBuffer _indexBuffer;

        protected ushort[] _indexList;

        public Vector3 Pivot { get; set; }

        public abstract Vector3 GetVertex(int index);

        public int VertexCount()
        {
            return _indexList.Count();
        }

        public void ApplyMesh(Effect effect, GraphicsDevice device)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffer.IndexCount);
            }
        }

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, List<int> faceSelection)
        {
            device.Indices = _indexBuffer;
            device.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var item in faceSelection)
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, item, 1);
            }

        }

        public virtual void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        public float? IntersectObject(Ray ray, Matrix modelMatrix)
        {
            var res = IntersectFace(ray, modelMatrix, out var _);
            return res;
        }

        public float? IntersectFace(Ray ray, Matrix modelMatrix, out int? face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(modelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _indexList.Length; i += 3)
            {
                var index0 = _indexList[i + 0];
                var index1 = _indexList[i + 1];
                var index2 = _indexList[i + 2];

                var vert0 = GetVertex(index0);
                var vert1 = GetVertex(index1);
                var vert2 = GetVertex(index2);

                var res = IntersectionMath.MollerTrumboreIntersection(ray, vert0, vert1, vert2, out var intersectionPoint);
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
                return null;

            face = faceIndex;
            return bestDistance;
        }

        public bool IntersectObject(BoundingFrustum boundingFrustum, Matrix modelMatrix)
        {
            for (int i = 0; i < VertexCount(); i++)
            {
                if (boundingFrustum.Contains(Vector3.Transform(GetVertex(i), modelMatrix)) != ContainmentType.Disjoint)
                    return true;
            }

            return false;
        }
    }
}
