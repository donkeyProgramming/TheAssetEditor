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
        public static float? IntersectObject(Ray ray, RenderItem item)
        {
            var res = IntersectFace(ray, item, out var _);
            return res;
        }

        public static float? IntersectFace(Ray ray, RenderItem item, out int? face)
        {
            face = null;

            Matrix inverseTransform = Matrix.Invert(item.ModelMatrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            int faceIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < item.Geometry.GetIndexCount(); i += 3)
            {
                var index0 = item.Geometry.GetIndex(i + 0);
                var index1 = item.Geometry.GetIndex(i + 1);
                var index2 = item.Geometry.GetIndex(i + 2);

                var vert0 = item.Geometry.GetVertex(index0);
                var vert1 = item.Geometry.GetVertex(index1);
                var vert2 = item.Geometry.GetVertex(index2);

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

        public static bool IntersectObject(BoundingFrustum boundingFrustum, RenderItem item)
        {
            for (int i = 0; i < item.Geometry.VertexCount(); i++)
            {
                if (boundingFrustum.Contains(Vector3.Transform(item.Geometry.GetVertex(i), item.ModelMatrix)) != ContainmentType.Disjoint)
                    return true;
            }

            return false;
        }

        public static bool IntersectFaces(BoundingFrustum boundingFrustum, RenderItem item, out List<int> faces)
        {
            faces = new List<int>();

            for (int i = 0; i < item.Geometry.GetIndexCount(); i += 3)
            {
                var index0 = item.Geometry.GetIndex(i + 0);
                var index1 = item.Geometry.GetIndex(i + 1);
                var index2 = item.Geometry.GetIndex(i + 2);

                if (boundingFrustum.Contains(Vector3.Transform(item.Geometry.GetVertex(index0), item.ModelMatrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(Vector3.Transform(item.Geometry.GetVertex(index1), item.ModelMatrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(Vector3.Transform(item.Geometry.GetVertex(index2), item.ModelMatrix)) != ContainmentType.Disjoint)
                    faces.Add(i);
            }

            if (faces.Count() == 0)
                faces = null;
            return faces != null;
        }

        public static bool IntersectVertices(BoundingFrustum boundingFrustum, RenderItem item, out List<int> vertices)
        {
            vertices = new List<int>();

            for (int i = 0; i < item.Geometry.GetIndexCount(); i++)
            {
                var index = item.Geometry.GetIndex(i);
                
                if (boundingFrustum.Contains(Vector3.Transform(item.Geometry.GetVertex(index), item.ModelMatrix)) != ContainmentType.Disjoint)
                    vertices.Add(index);
            }
            vertices = vertices.Distinct().ToList();
            if (vertices.Count() == 0)
                vertices = null;
            return vertices != null;
        }
    }
}
