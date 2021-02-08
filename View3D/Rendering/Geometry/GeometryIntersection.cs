using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Utility;

namespace View3D.Rendering.Geometry
{
    public static class GeometryIntersection
    {
        public static float? IntersectObject(Ray ray, IGeometry geometry, Matrix matrix)
        {
            var res = IntersectFace(ray, geometry, matrix, out var _);
            return res;
        }

        public static float? IntersectFace(Ray ray, IGeometry geometry, Matrix matrix, out int? face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(matrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < geometry.GetIndexCount(); i += 3)
            {
                var index0 = geometry.GetIndex(i + 0);
                var index1 = geometry.GetIndex(i + 1);
                var index2 = geometry.GetIndex(i + 2);

                var vert0 = geometry.GetVertex(index0);
                var vert1 = geometry.GetVertex(index1);
                var vert2 = geometry.GetVertex(index2);

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

        public static bool IntersectObject(BoundingFrustum boundingFrustum, IGeometry geometry, Matrix matrix)
        {
            for (int i = 0; i < geometry.VertexCount(); i++)
            {
                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertex(i), matrix)) != ContainmentType.Disjoint)
                    return true;
            }

            return false;
        }

        public static bool IntersectFaces(BoundingFrustum boundingFrustum, IGeometry geometry, Matrix matrix, out List<int> faces)
        {
            faces = new List<int>();

            for (int i = 0; i < geometry.GetIndexCount(); i += 3)
            {
                var index0 = geometry.GetIndex(i + 0);
                var index1 = geometry.GetIndex(i + 1);
                var index2 = geometry.GetIndex(i + 2);

                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertex(index0), matrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertex(index1), matrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertex(index2), matrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
            }

            if (faces.Count() == 0)
                faces = null;
            return faces != null;
        }

        public static bool IntersectVertices(BoundingFrustum boundingFrustum, IGeometry geometry, Matrix matrix, out List<int> vertices)
        {
            vertices = new List<int>();

            for (int i = 0; i < geometry.GetIndexCount(); i++)
            {
                var index = geometry.GetIndex(i);
                
                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertex(index), matrix)) != ContainmentType.Disjoint)
                    vertices.Add(index);
            }
            vertices = vertices.Distinct().ToList();
            if (vertices.Count() == 0)
                vertices = null;
            return vertices != null;
        }
    }
}
