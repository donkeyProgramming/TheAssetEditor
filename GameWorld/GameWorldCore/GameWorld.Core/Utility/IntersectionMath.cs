using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Utility
{
    public static class IntersectionMath
    {
        public static float? IntersectObject(Ray ray, MeshObject geometry, Matrix matrix)
        {
            var res = IntersectFace(ray, geometry, matrix, out var _);
            return res;
        }

        public static float? IntersectVertex(Ray ray, MeshObject geometry, Vector3 cameraPos, Matrix matrix, out int selectedVertex)
        {
            var inverseTransform = Matrix.Invert(matrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);
            cameraPos = Vector3.Transform(cameraPos, inverseTransform);

            var vertexList = geometry.GetVertexList();
            var bestDistance = float.MaxValue;
            selectedVertex = -1;
            for (var i = 0; i < vertexList.Count; i++)
            {
                var distance = (cameraPos - vertexList[i]).Length();
                var distanceScale = 0.0025f * distance * 1.5f;

                var bb = new BoundingBox(new Vector3(distanceScale * -0.5f) + vertexList[i], new Vector3(distanceScale * 0.5f) + vertexList[i]);
                var res = bb.Intersects(ray); ;
                if (res != null)
                {
                    var dist = res.Value;
                    if (dist < bestDistance)
                    {
                        selectedVertex = i;
                        bestDistance = dist;
                    }
                }
            }

            if (selectedVertex == -1)
                return null;

            return bestDistance;
        }

        public static float? IntersectFace(Ray ray, MeshObject geometry, Matrix matrix, out int? face)
        {
            face = null;

            var inverseTransform = Matrix.Invert(matrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            var faceIndex = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < geometry.GetIndexCount(); i += 3)
            {
                var index0 = geometry.GetIndex(i + 0);
                var index1 = geometry.GetIndex(i + 1);
                var index2 = geometry.GetIndex(i + 2);

                var vert0 = geometry.GetVertexById(index0);
                var vert1 = geometry.GetVertexById(index1);
                var vert2 = geometry.GetVertexById(index2);

                var res = MollerTrumboreIntersection(ray, vert0, vert1, vert2, out var intersectionPoint);
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

        public static bool IntersectObject(BoundingFrustum boundingFrustum, MeshObject geometry, Matrix matrix)
        {
            for (var i = 0; i < geometry.VertexCount(); i++)
            {
                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertexById(i), matrix)) != ContainmentType.Disjoint)
                    return true;
            }

            return false;
        }

        public static bool IntersectFaces(BoundingFrustum boundingFrustum, MeshObject geometry, Matrix matrix, out List<int> faces)
        {
            faces = new List<int>();

            var indexList = geometry.GetIndexBuffer();
            var vertList = geometry.GetVertexList();

            var transformedVertList = new Vector3[vertList.Count];
            for (var i = 0; i < vertList.Count; i++)
                transformedVertList[i] = Vector3.Transform(vertList[i], matrix);

            for (var i = 0; i < indexList.Count; i += 3)
            {
                var index0 = indexList[i + 0];
                var index1 = indexList[i + 1];
                var index2 = indexList[i + 2];

                if (boundingFrustum.Contains(transformedVertList[index0]) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(transformedVertList[index1]) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(transformedVertList[index2]) != ContainmentType.Disjoint)
                    faces.Add(i);
            }

            if (faces.Count() == 0)
                faces = null;
            return faces != null;
        }

        public static bool IntersectVertices(BoundingFrustum boundingFrustum, MeshObject geometry, Matrix matrix, out List<int> vertices)
        {
            vertices = new List<int>();

            for (var i = 0; i < geometry.GetIndexCount(); i++)
            {
                var index = geometry.GetIndex(i);

                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertexById(index), matrix)) != ContainmentType.Disjoint)
                    vertices.Add(index);
            }
            vertices = vertices.Distinct().ToList();
            if (vertices.Count() == 0)
                vertices = null;
            return vertices != null;
        }

        public static ushort FindClosestVertexIndex(MeshObject mesh, Vector3 point, out float distance)
        {
            var closestDist = float.PositiveInfinity;
            var bestVertexIndex = -1;

            for (var i = 0; i < mesh.VertexArray.Length; i++)
            {
                var dist = (point - mesh.VertexArray[i].Position3()).LengthSquared();
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestVertexIndex = i;
                }
            }

            distance = closestDist;
            return (ushort)bestVertexIndex;
        }

        public static bool MollerTrumboreIntersection(Ray r, Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, out float? distance)
        {
            //Source : https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
            const float EPSILON = 0.0000001f;
            Vector3 edge1, edge2, h, s, q;
            float a, f, u, v;
            edge1 = vertex1 - vertex0;
            edge2 = vertex2 - vertex0;
            h = Vector3.Cross(r.Direction, edge2);
            a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON)
            {
                distance = null;
                return false;    // This ray is parallel to this triangle.
            }
            f = 1.0f / a;
            s = r.Position - vertex0;
            u = f * Vector3.Dot(s, h);
            if (u < 0.0 || u > 1.0)
            {
                distance = null;
                return false;
            }
            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(r.Direction, q);
            if (v < 0.0 || u + v > 1.0)
            {
                distance = null;
                return false;
            }
            // At this stage we can compute t to find out where the intersection point is on the line.
            var t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
            {
                distance = t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
            {
                distance = null;
                return false;
            }
        }

        public static bool IntersectBones(BoundingFrustum boundingFrustum, Rmv2MeshNode sceneNode, GameSkeleton skeleton, Matrix matrix, out List<int> bones)
        {
            bones = new List<int>();

            if (sceneNode.AnimationPlayer == null) return false;

            var animPlayer = sceneNode.AnimationPlayer;
            var currentFrame = animPlayer.GetCurrentAnimationFrame();

            if (currentFrame == null) return false;
            var totalBones = currentFrame.BoneTransforms.Count;

            for (var boneIdx = 0; boneIdx < totalBones; boneIdx++)
            {
                var bone = currentFrame.GetSkeletonAnimatedWorld(skeleton, boneIdx);
                bone.Decompose(out var _, out var _, out var trans);
                if (boundingFrustum.Contains(Vector3.Transform(trans, matrix)) != ContainmentType.Disjoint)
                    bones.Add(boneIdx);
            }

            bones = bones.Distinct().ToList();
            if (bones.Count() == 0)
                bones = null;
            return bones != null;
        }
    }
}
