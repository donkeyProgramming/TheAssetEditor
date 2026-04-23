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
            var inverseTransform = Matrix.Invert(matrix);
            var localRay = new Ray(
                Vector3.Transform(ray.Position, inverseTransform),
                Vector3.TransformNormal(ray.Direction, inverseTransform));
            if (localRay.Intersects(geometry.BoundingBox) == null)
                return null;

            var res = IntersectFace(ray, geometry, matrix, out var _);
            return res;
        }

        public static float? IntersectVertex(Vector2 mouseScreenPos, MeshObject geometry, Matrix modelMatrix,
            Matrix viewProjection, float viewportWidth, float viewportHeight, out int selectedVertex)
        {
            selectedVertex = -1;
            var bestDist = float.MaxValue;

            const float pixelThreshold = 25.0f;

            for (var i = 0; i < geometry.VertexArray.Length; i++)
            {
                var worldPos = Vector3.Transform(geometry.GetVertexById(i), modelMatrix);
                var clipPos = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjection);

                if (clipPos.W <= 0.0f)
                    continue;

                var invW = 1.0f / clipPos.W;
                var screenX = (clipPos.X * invW + 1.0f) * 0.5f * viewportWidth;
                var screenY = (1.0f - clipPos.Y * invW) * 0.5f * viewportHeight;

                var dist = MathF.Abs(screenX - mouseScreenPos.X) + MathF.Abs(screenY - mouseScreenPos.Y);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    selectedVertex = i;
                }
            }

            if (selectedVertex == -1 || bestDist > pixelThreshold)
            {
                selectedVertex = -1;
                return null;
            }

            return bestDist;
        }

        public static float? IntersectFace(Ray ray, MeshObject geometry, Matrix matrix, out int? face)
        {
            face = null;

            var inverseTransform = Matrix.Invert(matrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);

            if (ray.Intersects(geometry.BoundingBox) == null)
                return null;

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
            var transformedBox = TransformBoundingBox(geometry.BoundingBox, matrix);
            if (boundingFrustum.Contains(transformedBox) == ContainmentType.Disjoint)
                return false;

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

            var transformedBox = TransformBoundingBox(geometry.BoundingBox, matrix);
            if (boundingFrustum.Contains(transformedBox) == ContainmentType.Disjoint)
                return false;

            var vertCount = geometry.VertexArray.Length;
            var transformedVerts = new Vector3[vertCount];
            for (var i = 0; i < vertCount; i++)
                transformedVerts[i] = Vector3.Transform(geometry.GetVertexById(i), matrix);

            for (var i = 0; i < geometry.IndexArray.Length; i += 3)
            {
                var index0 = geometry.IndexArray[i + 0];
                var index1 = geometry.IndexArray[i + 1];
                var index2 = geometry.IndexArray[i + 2];

                if (boundingFrustum.Contains(transformedVerts[index0]) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(transformedVerts[index1]) != ContainmentType.Disjoint)
                    faces.Add(i);
                else if (boundingFrustum.Contains(transformedVerts[index2]) != ContainmentType.Disjoint)
                    faces.Add(i);
            }

            if (faces.Count == 0)
                faces = null;
            return faces != null;
        }

        public static bool IntersectVertices(BoundingFrustum boundingFrustum, MeshObject geometry, Matrix matrix, out List<int> vertices)
        {
            vertices = new List<int>();

            for (var i = 0; i < geometry.IndexArray.Length; i++)
            {
                var index = geometry.IndexArray[i];
                if (boundingFrustum.Contains(Vector3.Transform(geometry.GetVertexById(index), matrix)) != ContainmentType.Disjoint)
                    vertices.Add(index);
            }

            if (vertices.Count == 0)
                vertices = null;
            else
                vertices = vertices.Distinct().ToList();
            return vertices != null;
        }

        public static float? IntersectEdge(Ray ray, MeshObject geometry, Vector3 cameraPos, Matrix matrix, out (int v0, int v1) selectedEdge)
        {
            selectedEdge = (-1, -1);
            var inverseTransform = Matrix.Invert(matrix);
            ray.Position = Vector3.Transform(ray.Position, inverseTransform);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverseTransform);
            cameraPos = Vector3.Transform(cameraPos, inverseTransform);

            var bestDistance = float.MaxValue;
            var edgeThreshold = 0.0025f;

            var processedEdges = new HashSet<(int, int)>();
            var indexBuffer = geometry.IndexArray;

            for (var i = 0; i < indexBuffer.Length; i += 3)
            {
                var i0 = indexBuffer[i];
                var i1 = indexBuffer[i + 1];
                var i2 = indexBuffer[i + 2];

                var edges = new[] { (Math.Min(i0, i1), Math.Max(i0, i1)), (Math.Min(i1, i2), Math.Max(i1, i2)), (Math.Min(i0, i2), Math.Max(i0, i2)) };

                foreach (var edge in edges)
                {
                    if (processedEdges.Contains(edge))
                        continue;
                    processedEdges.Add(edge);

                    var p0 = geometry.GetVertexById(edge.Item1);
                    var p1 = geometry.GetVertexById(edge.Item2);

                    var midPoint = (p0 + p1) * 0.5f;
                    var distToCamera = (cameraPos - midPoint).Length();
                    var scaledThreshold = edgeThreshold * distToCamera * 1.5f;

                    var dist = RayToLineSegmentDistance(ray, p0, p1);
                    if (dist < scaledThreshold && dist < bestDistance)
                    {
                        bestDistance = dist;
                        selectedEdge = edge;
                    }
                }
            }

            if (selectedEdge.Item1 == -1)
                return null;

            return bestDistance;
        }

        public static bool IntersectEdges(BoundingFrustum boundingFrustum, MeshObject geometry, Matrix matrix, out List<(int v0, int v1)> edges)
        {
            edges = new List<(int, int)>();
            var processedEdges = new HashSet<(int, int)>();
            var indexBuffer = geometry.IndexArray;

            var vertCount = geometry.VertexArray.Length;
            var transformedVerts = new Vector3[vertCount];
            for (var i = 0; i < vertCount; i++)
                transformedVerts[i] = Vector3.Transform(geometry.GetVertexById(i), matrix);

            for (var i = 0; i < indexBuffer.Length; i += 3)
            {
                var i0 = indexBuffer[i];
                var i1 = indexBuffer[i + 1];
                var i2 = indexBuffer[i + 2];

                var edgeList = new[] { (Math.Min(i0, i1), Math.Max(i0, i1)), (Math.Min(i1, i2), Math.Max(i1, i2)), (Math.Min(i0, i2), Math.Max(i0, i2)) };

                foreach (var edge in edgeList)
                {
                    if (processedEdges.Contains(edge))
                        continue;
                    processedEdges.Add(edge);

                    if (boundingFrustum.Contains(transformedVerts[edge.Item1]) != ContainmentType.Disjoint &&
                        boundingFrustum.Contains(transformedVerts[edge.Item2]) != ContainmentType.Disjoint)
                    {
                        edges.Add(edge);
                    }
                }
            }

            if (edges.Count == 0)
                return false;
            return true;
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
                return false;
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
            var t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON)
            {
                distance = t;
                return true;
            }
            else
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

        public static BoundingBox TransformBoundingBox(BoundingBox box, Matrix matrix)
        {
            var corners = box.GetCorners();
            Vector3.Transform(corners, ref matrix, corners);
            return BoundingBox.CreateFromPoints(corners);
        }

        static float RayToLineSegmentDistance(Ray ray, Vector3 segStart, Vector3 segEnd)
        {
            var rayDir = ray.Direction;
            var segDir = segEnd - segStart;
            var segLength = segDir.Length();

            if (segLength < 0.0001f)
            {
                var toPoint = segStart - ray.Position;
                var projection = Vector3.Dot(toPoint, rayDir);
                var closestOnRay = ray.Position + rayDir * projection;
                return (closestOnRay - segStart).Length();
            }

            segDir /= segLength;

            var w0 = ray.Position - segStart;
            var a = Vector3.Dot(rayDir, rayDir);
            var b = Vector3.Dot(rayDir, segDir);
            var c = Vector3.Dot(segDir, segDir);
            var d = Vector3.Dot(rayDir, w0);
            var e = Vector3.Dot(segDir, w0);

            var denom = a * c - b * b;

            float s, t;
            if (denom < 0.0001f)
            {
                s = 0f;
                t = d / b;
            }
            else
            {
                s = (b * e - c * d) / denom;
                t = (a * e - b * d) / denom;
            }

            t = MathHelper.Clamp(t, 0f, segLength);

            var rayPt = ray.Position + rayDir * s;
            var segPt = segStart + segDir * t;

            return (rayPt - segPt).Length();
        }
    }
}
