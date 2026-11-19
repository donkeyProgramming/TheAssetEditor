#region License
/*
MIT License

Copyright(c) 2017-2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

#region Original License
/////////////////////////////////////////////
//
// Mesh Simplification Tutorial
//
// (C) by Sven Forstmann in 2014
//
// License : MIT
// http://opensource.org/licenses/MIT
//
//https://github.com/sp4cerat/Fast-Quadric-Mesh-Simplification
#endregion

using MeshDecimator.Collections;
using MeshDecimator.Math;
using System;
using System.Collections.Generic;

namespace MeshDecimator.Algorithms
{
    /// <summary>
    /// The fast quadric mesh simplification algorithm.
    /// </summary>
    public sealed class FastQuadricMeshSimplification : DecimationAlgorithm
    {
        #region Consts
        private const double DoubleEpsilon = 1.0E-3;
        // Blender: boundary edge constraint plane weight (bmesh_decimate_collapse.cc line 54)
        private const double BoundaryPreserveWeight = 100.0;
        // Blender: threshold below which QEM cost is considered noise in flat regions
        private const double TopologyFallbackEps = 1e-12;
        // Scale for topology fallback cost in flat regions: edge_length² * this value.
        private const double TopologyFallbackScale = 1e-2;
        #endregion

        #region Classes
        #region Triangle
        private struct Triangle
        {
            #region Fields
            public int v0;
            public int v1;
            public int v2;
            public int subMeshIndex;

            public int va0;
            public int va1;
            public int va2;

            public double err0;
            public double err1;
            public double err2;
            public double err3;

            public bool deleted;
            public bool dirty;
            public Vector3d n;
            #endregion

            #region Properties
            public int this[int index]
            {
                get
                {
                    return (index == 0 ? v0 : (index == 1 ? v1 : v2));
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            v0 = value;
                            break;
                        case 1:
                            v1 = value;
                            break;
                        case 2:
                            v2 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
            #endregion

            #region Constructor
            public Triangle(int v0, int v1, int v2, int subMeshIndex)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
                this.subMeshIndex = subMeshIndex;

                this.va0 = v0;
                this.va1 = v1;
                this.va2 = v2;

                err0 = err1 = err2 = err3 = 0;
                deleted = dirty = false;
                n = new Vector3d();
            }
            #endregion

            #region Public Methods
            public void GetAttributeIndices(int[] attributeIndices)
            {
                attributeIndices[0] = va0;
                attributeIndices[1] = va1;
                attributeIndices[2] = va2;
            }

            public void SetAttributeIndex(int index, int value)
            {
                switch (index)
                {
                    case 0:
                        va0 = value;
                        break;
                    case 1:
                        va1 = value;
                        break;
                    case 2:
                        va2 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public void GetErrors(double[] err)
            {
                err[0] = err0;
                err[1] = err1;
                err[2] = err2;
            }
            #endregion
        }
        #endregion

        #region Vertex
        private struct Vertex
        {
            public Vector3d p;
            public int tstart;
            public int tcount;
            public SymmetricMatrix q;
            public bool border;
            public bool seam;
            public bool foldover;

            public Vertex(Vector3d p)
            {
                this.p = p;
                this.tstart = 0;
                this.tcount = 0;
                this.q = new SymmetricMatrix();
                this.border = true;
                this.seam = false;
                this.foldover = false;
            }
        }
        #endregion

        #region Ref
        private struct Ref
        {
            public int tid;
            public int tvertex;

            public void Set(int tid, int tvertex)
            {
                this.tid = tid;
                this.tvertex = tvertex;
            }
        }
        #endregion

        #region Border Vertex
        private struct BorderVertex
        {
            public int index;
            public int hash;

            public BorderVertex(int index, int hash)
            {
                this.index = index;
                this.hash = hash;
            }
        }
        #endregion

        #region Border Vertex Comparer
        private class BorderVertexComparer : IComparer<BorderVertex>
        {
            public static readonly BorderVertexComparer instance = new BorderVertexComparer();

            public int Compare(BorderVertex x, BorderVertex y)
            {
                return x.hash.CompareTo(y.hash);
            }
        }
        #endregion

        #region Edge Entry
        // Heap entry for Blender-style min-heap edge collapse (bmesh_decimate_collapse.cc).
        // Uses version-based stale detection: when a vertex is modified during collapse,
        // its version increments, invalidating all heap entries referencing the old version.
        // This avoids floating-point equality issues that plagued cost-based stale detection.
        private struct EdgeEntry
        {
            public int v0; // always < v1
            public int v1;
            public long version0; // vertexVersion[v0] when this entry was created
            public long version1; // vertexVersion[v1] when this entry was created

            public EdgeEntry(int v0, int v1, long version0, long version1)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.version0 = version0;
                this.version1 = version1;
            }
        }
        #endregion
        #endregion

        #region Fields
        private bool preserveSeams = false;
        private bool preserveFoldovers = false;
        private bool enableSmartLink = true;
        private int maxIterationCount = 100;
        private double agressiveness = 7.0;
        private double vertexLinkDistanceSqr = double.Epsilon;

        private int subMeshCount = 0;
        private ResizableArray<Triangle> triangles = null;
        private ResizableArray<Vertex> vertices = null;
        private ResizableArray<Ref> refs = null;

        private ResizableArray<Vector3> vertNormals = null;
        private ResizableArray<Vector4> vertTangents = null;
        private UVChannels<Vector2> vertUV2D = null;
        private UVChannels<Vector3> vertUV3D = null;
        private UVChannels<Vector4> vertUV4D = null;
        private ResizableArray<Vector4> vertColors = null;
        private ResizableArray<BoneWeight> vertBoneWeights = null;

        private int remainingVertices = 0;

        // Version counter per vertex for heap stale detection (Blender-style).
        // Incremented when a vertex is modified by edge collapse.
        // Heap entries store the version at creation time; stale entries have mismatched versions.
        private long[] vertexVersion;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if seams should be preserved.
        /// Default value: false
        /// </summary>
        public bool PreserveSeams
        {
            get { return preserveSeams; }
            set { preserveSeams = value; }
        }

        /// <summary>
        /// Gets or sets if foldovers should be preserved.
        /// Default value: false
        /// </summary>
        public bool PreserveFoldovers
        {
            get { return preserveFoldovers; }
            set { preserveFoldovers = value; }
        }

        /// <summary>
        /// Gets or sets if a feature for smarter vertex linking should be enabled, reducing artifacts in the
        /// decimated result at the cost of a slightly more expensive initialization by treating vertices at
        /// the same position as the same vertex while separating the attributes.
        /// Default value: true
        /// </summary>
        public bool EnableSmartLink
        {
            get { return enableSmartLink; }
            set { enableSmartLink = value; }
        }

        /// <summary>
        /// Gets or sets the maximum iteration count. Higher number is more expensive but can bring you closer to your target quality.
        /// Sometimes a lower maximum count might be desired in order to lower the performance cost.
        /// Default value: 100
        /// </summary>
        public int MaxIterationCount
        {
            get { return maxIterationCount; }
            set { maxIterationCount = value; }
        }

        /// <summary>
        /// Gets or sets the agressiveness of this algorithm. Higher number equals higher quality, but more expensive to run.
        /// Default value: 7.0
        /// </summary>
        public double Agressiveness
        {
            get { return agressiveness; }
            set { agressiveness = value; }
        }

        /// <summary>
        /// Gets or sets the maximum squared distance between two vertices in order to link them.
        /// Note that this value is only used if EnableSmartLink is true.
        /// Default value: double.Epsilon
        /// </summary>
        public double VertexLinkDistanceSqr
        {
            get { return vertexLinkDistanceSqr; }
            set { vertexLinkDistanceSqr = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new fast quadric mesh simplification algorithm.
        /// </summary>
        public FastQuadricMeshSimplification()
        {
            triangles = new ResizableArray<Triangle>(0);
            vertices = new ResizableArray<Vertex>(0);
            refs = new ResizableArray<Ref>(0);
        }
        #endregion

        #region Private Methods
        #region Initialize Vertex Attribute
        private ResizableArray<T> InitializeVertexAttribute<T>(T[] attributeValues, string attributeName)
        {
            if (attributeValues != null && attributeValues.Length == vertices.Length)
            {
                var newArray = new ResizableArray<T>(attributeValues.Length, attributeValues.Length);
                var newArrayData = newArray.Data;
                Array.Copy(attributeValues, 0, newArrayData, 0, attributeValues.Length);
                return newArray;
            }
            else if (attributeValues != null && attributeValues.Length > 0)
            {
                Logging.LogError("Failed to set vertex attribute '{0}' with {1} length of array, when {2} was needed.", attributeName, attributeValues.Length, vertices.Length);
            }
            return null;
        }
        #endregion

        #region Calculate Error
        private double VertexError(ref SymmetricMatrix q, double x, double y, double z)
        {
            return q.m0 * x * x + 2 * q.m1 * x * y + 2 * q.m2 * x * z + 2 * q.m3 * x + q.m4 * y * y
                + 2 * q.m5 * y * z + 2 * q.m6 * y + q.m7 * z * z + 2 * q.m8 * z + q.m9;
        }

        private double CalculateError(ref Vertex vert0, ref Vertex vert1, out Vector3d result, out int resultIndex)
        {
            // compute interpolated vertex
            SymmetricMatrix q = (vert0.q + vert1.q);
            bool border = (vert0.border & vert1.border);
            double error = 0.0;
            double det = q.Determinant1();
            if (det != 0.0 && !border)
            {
                // q_delta is invertible
                result = new Vector3d(
                    -1.0 / det * q.Determinant2(),  // vx = A41/det(q_delta)
                    1.0 / det * q.Determinant3(),   // vy = A42/det(q_delta)
                    -1.0 / det * q.Determinant4()); // vz = A43/det(q_delta)
                error = VertexError(ref q, result.x, result.y, result.z);
                resultIndex = 2;
            }
            else
            {
                // det = 0 -> try to find best result
                Vector3d p1 = vert0.p;
                Vector3d p2 = vert1.p;
                Vector3d p3 = (p1 + p2) * 0.5f;
                double error1 = VertexError(ref q, p1.x, p1.y, p1.z);
                double error2 = VertexError(ref q, p2.x, p2.y, p2.z);
                double error3 = VertexError(ref q, p3.x, p3.y, p3.z);
                error = MathHelper.Min(error1, error2, error3);
                if (error == error3)
                {
                    result = p3;
                    resultIndex = 2;
                }
                else if (error == error2)
                {
                    result = p2;
                    resultIndex = 1;
                }
                else if (error == error1)
                {
                    result = p1;
                    resultIndex = 0;
                }
                else
                {
                    result = p3;
                    resultIndex = 2;
                }
            }
            return error;
        }
        #endregion

        #region Flipped
        /// <summary>
        /// Check if a triangle flips when this edge is removed.
        /// Uses Blender's area-weighted flip detection (bmesh_decimate_collapse.cc line 190):
        /// Compares unnormalized cross products with relative threshold.
        /// Large triangles get stricter protection (structurally important),
        /// small triangles get more flexibility. When collapse changes triangle area
        /// significantly, the check becomes stricter — protecting thin features.
        /// </summary>
        private bool Flipped(ref Vector3d p, int i0, int i1, ref Vertex v0, bool[] deleted)
        {
            int tcount = v0.tcount;
            var refs = this.refs.Data;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;
            Vector3d v0pos = v0.p;

            for (int k = 0; k < tcount; k++)
            {
                Ref r = refs[v0.tstart + k];
                if (triangles[r.tid].deleted)
                    continue;

                int s = r.tvertex;
                int id1 = triangles[r.tid][(s + 1) % 3];
                int id2 = triangles[r.tid][(s + 2) % 3];
                if (id1 == i1 || id2 == i1)
                {
                    deleted[k] = true;
                    continue;
                }

                // BEFORE-collapse normal (unnormalized cross product)
                Vector3d d1_before = vertices[id1].p - v0pos;
                Vector3d d2_before = vertices[id2].p - v0pos;
                Vector3d cross_before;
                Vector3d.Cross(ref d1_before, ref d2_before, out cross_before);
                if (cross_before.MagnitudeSqr < 1e-20)
                    return true;

                // AFTER-collapse normal (unnormalized cross product)
                Vector3d d1_after = vertices[id1].p - p;
                Vector3d d2_after = vertices[id2].p - p;
                Vector3d cross_after;
                Vector3d.Cross(ref d1_after, ref d2_after, out cross_after);

                deleted[k] = false;

                // Reject degenerate triangles (near-zero area)
                double optimMagSq = cross_after.MagnitudeSqr;
                if (optimMagSq < 1e-20)
                    return true;

                // Blender area-weighted flip detection:
                // dot(cross_before, cross_after) <= (|cross_before|² + |cross_after|²) * 0.01
                // For equal-area triangles: allows ~89° rotation
                // For 10x area change: becomes ~84° (stricter, protects thin features)
                double existMagSq = cross_before.MagnitudeSqr;
                double dotBA = (cross_before.x * cross_after.x + cross_before.y * cross_after.y + cross_before.z * cross_after.z);

                if (dotBA <= (existMagSq + optimMagSq) * 0.01)
                    return true;
            }

            return false;
        }
        #endregion

        #region Update Triangles
        /// <summary>
        /// Update triangle connections after an edge is collapsed.
        /// Simplified for min-heap approach: no error computation (costs managed by heap).
        /// </summary>
        private void UpdateTriangles(int i0, int ia0, ref Vertex v, bool[] deleted, ref int deletedTriangles)
        {
            int tcount = v.tcount;
            var triangles = this.triangles.Data;
            for (int k = 0; k < tcount; k++)
            {
                Ref r = refs[v.tstart + k];
                int tid = r.tid;
                Triangle t = triangles[tid];
                if (t.deleted)
                    continue;

                if (deleted[k])
                {
                    triangles[tid].deleted = true;
                    ++deletedTriangles;
                    continue;
                }

                t[r.tvertex] = i0;
                if (ia0 != -1)
                {
                    t.SetAttributeIndex(r.tvertex, ia0);
                }

                t.dirty = true;
                triangles[tid] = t;
                refs.Add(r);
            }
        }
        #endregion

        #region Move/Merge Vertex Attributes
        private void MoveVertexAttributes(int i0, int i1)
        {
            if (vertNormals != null)
            {
                vertNormals[i0] = vertNormals[i1];
            }
            if (vertTangents != null)
            {
                vertTangents[i0] = vertTangents[i1];
            }
            if (vertUV2D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV2D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertUV3D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV3D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertUV4D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV4D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertColors != null)
            {
                vertColors[i0] = vertColors[i1];
            }
            if (vertBoneWeights != null)
            {
                vertBoneWeights[i0] = vertBoneWeights[i1];
            }
        }

        /// <summary>
        /// Calculate interpolation factor: where the optimal point falls on the edge [0,1].
        /// 0 = p0, 1 = p1. Used for weighted attribute blending (Blender USE_VERT_NORMAL_INTERP).
        /// </summary>
        private double CalculateInterpolationFactor(ref Vector3d optimal, ref Vector3d p0, ref Vector3d p1)
        {
            Vector3d edge = p1 - p0;
            double edgeLenSqr = edge.MagnitudeSqr;
            if (edgeLenSqr < 1e-20)
                return 0.5;
            Vector3d diff = optimal - p0;
            double t = Vector3d.Dot(ref diff, ref edge) / edgeLenSqr;
            return System.Math.Max(0.0, System.Math.Min(1.0, t));
        }

        private void MergeVertexAttributes(int i0, int i1, double t)
        {
            // Blender USE_VERT_NORMAL_INTERP: interpolate by edge-parametric factor t
            // instead of simple 0.5 average, for smoother normals
            if (vertNormals != null)
            {
                var n0 = vertNormals[i0];
                var n1 = vertNormals[i1];
                var merged = new Vector3(
                    (float)(n0.x * (1 - t) + n1.x * t),
                    (float)(n0.y * (1 - t) + n1.y * t),
                    (float)(n0.z * (1 - t) + n1.z * t));
                float len = (float)System.Math.Sqrt(merged.x * merged.x + merged.y * merged.y + merged.z * merged.z);
                if (len > 0) merged = new Vector3(merged.x / len, merged.y / len, merged.z / len);
                vertNormals[i0] = merged;
            }
            if (vertTangents != null)
            {
                var t0 = vertTangents[i0];
                var t1 = vertTangents[i1];
                vertTangents[i0] = new Vector4(
                    (float)(t0.x * (1 - t) + t1.x * t),
                    (float)(t0.y * (1 - t) + t1.y * t),
                    (float)(t0.z * (1 - t) + t1.z * t),
                    (float)(t0.w * (1 - t) + t1.w * t));
            }
            if (vertUV2D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV2D[i];
                    if (vertUV != null)
                    {
                        var uv0 = vertUV[i0];
                        var uv1 = vertUV[i1];
                        vertUV[i0] = new Vector2(
                            (float)(uv0.x * (1 - t) + uv1.x * t),
                            (float)(uv0.y * (1 - t) + uv1.y * t));
                    }
                }
            }
            if (vertUV3D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV3D[i];
                    if (vertUV != null)
                    {
                        var uv0 = vertUV[i0];
                        var uv1 = vertUV[i1];
                        vertUV[i0] = new Vector3(
                            (float)(uv0.x * (1 - t) + uv1.x * t),
                            (float)(uv0.y * (1 - t) + uv1.y * t),
                            (float)(uv0.z * (1 - t) + uv1.z * t));
                    }
                }
            }
            if (vertUV4D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    var vertUV = vertUV4D[i];
                    if (vertUV != null)
                    {
                        var uv0 = vertUV[i0];
                        var uv1 = vertUV[i1];
                        vertUV[i0] = new Vector4(
                            (float)(uv0.x * (1 - t) + uv1.x * t),
                            (float)(uv0.y * (1 - t) + uv1.y * t),
                            (float)(uv0.z * (1 - t) + uv1.z * t),
                            (float)(uv0.w * (1 - t) + uv1.w * t));
                    }
                }
            }
            if (vertColors != null)
            {
                var c0 = vertColors[i0];
                var c1 = vertColors[i1];
                vertColors[i0] = new Vector4(
                    (float)(c0.x * (1 - t) + c1.x * t),
                    (float)(c0.y * (1 - t) + c1.y * t),
                    (float)(c0.z * (1 - t) + c1.z * t),
                    (float)(c0.w * (1 - t) + c1.w * t));
            }

            // TODO: Do we have to blend bone weights at all or can we just keep them as it is in this scenario?
        }
        #endregion

        #region Are UVs The Same
        private bool AreUVsTheSame(int channel, int indexA, int indexB)
        {
            if (vertUV2D != null)
            {
                var vertUV = vertUV2D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            if (vertUV3D != null)
            {
                var vertUV = vertUV3D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            if (vertUV4D != null)
            {
                var vertUV = vertUV4D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            return false;
        }
        #endregion

        #region Compute Edge Cost
        /// <summary>
        /// Compute edge collapse cost using QEM (Garland-Heckbert).
        /// Uses Blender's topology fallback for flat regions (bmesh_decimate_collapse.cc:287-309).
        /// </summary>
        private double ComputeEdgeCost(int va, int vb)
        {
            var vertices = this.vertices.Data;
            Vector3d dummy;
            int dummy2;
            double cost = System.Math.Abs(CalculateError(ref vertices[va], ref vertices[vb], out dummy, out dummy2));

            // Topology fallback for flat regions (Blender USE_TOPOLOGY_FALLBACK):
            // When QEM cost is near zero (flat surface), use edge length² as tiebreaker
            // so shorter edges collapse first → even distribution.
            if (cost < TopologyFallbackEps)
            {
                double lenSqr = (vertices[va].p - vertices[vb].p).MagnitudeSqr;
                cost = lenSqr * TopologyFallbackScale;
            }

            return cost;
        }
        #endregion

        #region Push Edge Cost
        /// <summary>
        /// Compute cost for edge (a,b) and push to heap with current vertex versions.
        /// Does not check border/seam constraints — those are checked at collapse time.
        /// </summary>
        private void PushEdgeCost(PriorityQueue<EdgeEntry, double> heap, long[] version, int a, int b)
        {
            if (a == b) return;
            int va = System.Math.Min(a, b);
            int vb = System.Math.Max(a, b);

            var vertices = this.vertices.Data;
            if (vertices[va].tcount == 0 || vertices[vb].tcount == 0) return;

            double cost = ComputeEdgeCost(va, vb);
            var entry = new EdgeEntry(va, vb, version[va], version[vb]);
            heap.Enqueue(entry, cost);
        }
        #endregion

        #region Build Edge Costs
        /// <summary>
        /// Build initial edge costs for all edges in the mesh.
        /// Iterates over all triangles and pushes each edge to the heap.
        /// Duplicate edges (manifold edges shared by 2 triangles) may be pushed twice,
        /// but version-based stale detection handles this correctly.
        /// </summary>
        private void BuildEdgeCosts(PriorityQueue<EdgeEntry, double> heap, long[] version)
        {
            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            for (int i = 0; i < triangleCount; i++)
            {
                var t = triangles[i];
                if (t.deleted) continue;

                PushEdgeCost(heap, version, t.v0, t.v1);
                PushEdgeCost(heap, version, t.v1, t.v2);
                PushEdgeCost(heap, version, t.v2, t.v0);
            }
        }
        #endregion

        #region Update Neighbor Costs
        /// <summary>
        /// After collapsing an edge, recompute and push costs for all edges
        /// touching the kept vertex. This is Blender's approach:
        /// after bm_edge_collapse, update costs for all edges in v_other's disk cycle.
        /// </summary>
        private void UpdateNeighborCosts(int v0, PriorityQueue<EdgeEntry, double> heap, long[] version)
        {
            var vertData = this.vertices.Data;
            var refsData = this.refs.Data;
            var triData = this.triangles.Data;

            int tstart = vertData[v0].tstart;
            int tcount = vertData[v0].tcount;

            for (int k = 0; k < tcount; k++)
            {
                Ref r = refsData[tstart + k];
                var t = triData[r.tid];
                if (t.deleted) continue;

                PushEdgeCost(heap, version, t.v0, t.v1);
                PushEdgeCost(heap, version, t.v1, t.v2);
                PushEdgeCost(heap, version, t.v2, t.v0);
            }
        }
        #endregion

        #region Is Degenerate Topology
        /// <summary>
        /// Check if collapsing edge (i0,i1) would create duplicate faces.
        /// Blender bmesh_decimate_collapse.cc:857-937: tag-based overlap detection.
        /// For each non-shared triangle of i0, check if i1 has a matching triangle
        /// with the same "other" two vertices → would become identical after collapse.
        /// </summary>
        private bool IsDegenerateTopology(int i0, int i1)
        {
            var refsData = this.refs.Data;
            var trisData = this.triangles.Data;
            var vertsData = this.vertices.Data;

            int tcount0 = vertsData[i0].tcount;
            int tstart0 = vertsData[i0].tstart;

            for (int a = 0; a < tcount0; a++)
            {
                Ref ra = refsData[tstart0 + a];
                var ta = trisData[ra.tid];
                if (ta.deleted) continue;
                if (ta.v0 == i1 || ta.v1 == i1 || ta.v2 == i1) continue;
                int sa = ra.tvertex;
                int na0 = ta[(sa + 1) % 3];
                int na1 = ta[(sa + 2) % 3];

                int tcount1 = vertsData[i1].tcount;
                int tstart1 = vertsData[i1].tstart;

                for (int b = 0; b < tcount1; b++)
                {
                    Ref rb = refsData[tstart1 + b];
                    var tb = trisData[rb.tid];
                    if (tb.deleted) continue;
                    if (tb.v0 == i0 || tb.v1 == i0 || tb.v2 == i0) continue;
                    int sb = rb.tvertex;
                    int nb0 = tb[(sb + 1) % 3];
                    int nb1 = tb[(sb + 2) % 3];

                    if ((na0 == nb0 && na1 == nb1) || (na0 == nb1 && na1 == nb0))
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region Get Vertex Attribute Index
        private static int GetVertexAttrIndex(ref Triangle t, int vertexPos)
        {
            return vertexPos == 0 ? t.va0 : (vertexPos == 1 ? t.va1 : t.va2);
        }
        #endregion

        #region Init Borders And Smart Link
        /// <summary>
        /// One-time border detection and smart link.
        /// Extracted from the original UpdateMesh iteration==0 block.
        /// </summary>
        private void InitBordersAndSmartLink(int vertexCount, int triangleCount)
        {
            var refs = this.refs.Data;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;

            var vcount = new List<int>(8);
            var vids = new List<int>(8);
            int vsize = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].border = false;
                vertices[i].seam = false;
                vertices[i].foldover = false;
            }

            int ofs;
            int id;
            int borderVertexCount = 0;
            double borderMinX = double.MaxValue;
            double borderMaxX = double.MinValue;
            for (int i = 0; i < vertexCount; i++)
            {
                int tstart = vertices[i].tstart;
                int tcount = vertices[i].tcount;
                vcount.Clear();
                vids.Clear();
                vsize = 0;

                for (int j = 0; j < tcount; j++)
                {
                    int tid = refs[tstart + j].tid;
                    for (int k = 0; k < 3; k++)
                    {
                        ofs = 0;
                        id = triangles[tid][k];
                        while (ofs < vsize)
                        {
                            if (vids[ofs] == id)
                                break;
                            ++ofs;
                        }

                        if (ofs == vsize)
                        {
                            vcount.Add(1);
                            vids.Add(id);
                            ++vsize;
                        }
                        else
                        {
                            ++vcount[ofs];
                        }
                    }
                }

                for (int j = 0; j < vsize; j++)
                {
                    if (vcount[j] == 1)
                    {
                        id = vids[j];
                        vertices[id].border = true;
                        ++borderVertexCount;

                        if (enableSmartLink)
                        {
                            if (vertices[id].p.x < borderMinX)
                                borderMinX = vertices[id].p.x;
                            if (vertices[id].p.x > borderMaxX)
                                borderMaxX = vertices[id].p.x;
                        }
                    }
                }
            }

            if (enableSmartLink)
            {
                var borderVertices = new BorderVertex[borderVertexCount];
                int borderIndexCount = 0;
                double borderAreaWidth = borderMaxX - borderMinX;
                for (int i = 0; i < vertexCount; i++)
                {
                    if (vertices[i].border)
                    {
                        int vertexHash = (int)(((((vertices[i].p.x - borderMinX) / borderAreaWidth) * 2.0) - 1.0) * int.MaxValue);
                        borderVertices[borderIndexCount] = new BorderVertex(i, vertexHash);
                        ++borderIndexCount;
                    }
                }

                Array.Sort(borderVertices, 0, borderIndexCount, BorderVertexComparer.instance);

                double vertexLinkDistance = System.Math.Sqrt(vertexLinkDistanceSqr);
                int hashMaxDistance = System.Math.Max((int)((vertexLinkDistance / borderAreaWidth) * int.MaxValue), 1);

                for (int i = 0; i < borderIndexCount; i++)
                {
                    int myIndex = borderVertices[i].index;
                    if (myIndex == -1)
                        continue;

                    var myPoint = vertices[myIndex].p;
                    for (int j = i + 1; j < borderIndexCount; j++)
                    {
                        int otherIndex = borderVertices[j].index;
                        if (otherIndex == -1)
                            continue;
                        else if ((borderVertices[j].hash - borderVertices[i].hash) > hashMaxDistance)
                            break;

                        var otherPoint = vertices[otherIndex].p;
                        var sqrX = ((myPoint.x - otherPoint.x) * (myPoint.x - otherPoint.x));
                        var sqrY = ((myPoint.y - otherPoint.y) * (myPoint.y - otherPoint.y));
                        var sqrZ = ((myPoint.z - otherPoint.z) * (myPoint.z - otherPoint.z));
                        var sqrMagnitude = sqrX + sqrY + sqrZ;

                        if (sqrMagnitude <= vertexLinkDistanceSqr)
                        {
                            borderVertices[j].index = -1;
                            vertices[myIndex].border = false;
                            vertices[otherIndex].border = false;

                            if (AreUVsTheSame(0, myIndex, otherIndex))
                            {
                                vertices[myIndex].foldover = true;
                                vertices[otherIndex].foldover = true;
                            }
                            else
                            {
                                vertices[myIndex].seam = true;
                                vertices[otherIndex].seam = true;
                            }

                            int otherTriangleCount = vertices[otherIndex].tcount;
                            int otherTriangleStart = vertices[otherIndex].tstart;
                            for (int k = 0; k < otherTriangleCount; k++)
                            {
                                var r = refs[otherTriangleStart + k];
                                triangles[r.tid][r.tvertex] = myIndex;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Init Quadrics And Boundary Constraints
        /// <summary>
        /// Build quadrics from face planes and add boundary constraint planes.
        /// Blender bmesh_decimate_collapse.cc:75-128: build quadrics once, add boundary
        /// constraints with BOUNDARY_PRESERVE_WEIGHT.
        /// </summary>
        private void InitQuadricsAndBoundaryConstraints(int vertexCount, int triangleCount)
        {
            var vertices = this.vertices.Data;
            var triangles = this.triangles.Data;
            var refsData = this.refs.Data;

            // Reset all quadrics
            for (int i = 0; i < vertexCount; i++)
                vertices[i].q = new SymmetricMatrix();

            // Build quadrics from face planes
            int v0, v1, v2;
            Vector3d n, p0, p1, p2, p10, p20;
            SymmetricMatrix sm;
            for (int i = 0; i < triangleCount; i++)
            {
                v0 = triangles[i].v0;
                v1 = triangles[i].v1;
                v2 = triangles[i].v2;

                p0 = vertices[v0].p;
                p1 = vertices[v1].p;
                p2 = vertices[v2].p;
                p10 = p1 - p0;
                p20 = p2 - p0;
                Vector3d.Cross(ref p10, ref p20, out n);
                n.Normalize();
                triangles[i].n = n;

                sm = new SymmetricMatrix(n.x, n.y, n.z, -Vector3d.Dot(ref n, ref p0));
                vertices[v0].q += sm;
                vertices[v1].q += sm;
                vertices[v2].q += sm;
            }

            // Boundary edge constraints (Blender bmesh_decimate_collapse.cc:101-127):
            // For each boundary edge, add a perpendicular constraint plane with high weight.
            for (int i = 0; i < triangleCount; i++)
            {
                var tri = triangles[i];
                int[] edgePairs = { tri.v0, tri.v1, tri.v1, tri.v2, tri.v2, tri.v0 };
                for (int e = 0; e < 6; e += 2)
                {
                    int va = edgePairs[e];
                    int vb = edgePairs[e + 1];
                    if (va > vb) continue;
                    if (!vertices[va].border || !vertices[vb].border) continue;

                    // Check if this is a boundary edge (shared by exactly one triangle)
                    int sharedCount = 0;
                    int tstartA = vertices[va].tstart;
                    int tcountA = vertices[va].tcount;
                    for (int j = 0; j < tcountA; j++)
                    {
                        int tid = refsData[tstartA + j].tid;
                        var t = triangles[tid];
                        if (t.v0 == vb || t.v1 == vb || t.v2 == vb)
                            sharedCount++;
                    }
                    if (sharedCount != 1) continue;

                    // Compute perpendicular constraint plane
                    Vector3d edgeVec = vertices[vb].p - vertices[va].p;
                    Vector3d faceNormal = tri.n;
                    Vector3d constraintNormal;
                    Vector3d.Cross(ref edgeVec, ref faceNormal, out constraintNormal);
                    double constraintMagSqr = constraintNormal.MagnitudeSqr;
                    if (constraintMagSqr > 1e-20)
                    {
                        constraintNormal = constraintNormal * (1.0 / System.Math.Sqrt(constraintMagSqr));
                        double d = -Vector3d.Dot(ref constraintNormal, ref vertices[va].p);
                        SymmetricMatrix constraint = new SymmetricMatrix(
                            constraintNormal.x, constraintNormal.y, constraintNormal.z, d);
                        constraint = constraint * BoundaryPreserveWeight;
                        vertices[va].q += constraint;
                        vertices[vb].q += constraint;
                    }
                }
            }
        }
        #endregion

        #region Update References
        private void UpdateReferences()
        {
            int triangleCount = this.triangles.Length;
            int vertexCount = this.vertices.Length;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;

            // Init Reference ID list
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tstart = 0;
                vertices[i].tcount = 0;
            }

            for (int i = 0; i < triangleCount; i++)
            {
                ++vertices[triangles[i].v0].tcount;
                ++vertices[triangles[i].v1].tcount;
                ++vertices[triangles[i].v2].tcount;
            }

            int tstart = 0;
            remainingVertices = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tstart = tstart;
                if (vertices[i].tcount > 0)
                {
                    tstart += vertices[i].tcount;
                    vertices[i].tcount = 0;
                    ++remainingVertices;
                }
            }

            // Write References
            this.refs.Resize(tstart);
            var refs = this.refs.Data;
            for (int i = 0; i < triangleCount; i++)
            {
                int v0 = triangles[i].v0;
                int v1 = triangles[i].v1;
                int v2 = triangles[i].v2;
                int start0 = vertices[v0].tstart;
                int count0 = vertices[v0].tcount;
                int start1 = vertices[v1].tstart;
                int count1 = vertices[v1].tcount;
                int start2 = vertices[v2].tstart;
                int count2 = vertices[v2].tcount;

                refs[start0 + count0].Set(i, 0);
                refs[start1 + count1].Set(i, 1);
                refs[start2 + count2].Set(i, 2);

                ++vertices[v0].tcount;
                ++vertices[v1].tcount;
                ++vertices[v2].tcount;
            }
        }
        #endregion

        #region Compact Mesh
        /// <summary>
        /// Finally compact mesh before exiting.
        /// </summary>
        private void CompactMesh()
        {
            int dst = 0;
            var vertices = this.vertices.Data;
            int vertexCount = this.vertices.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tcount = 0;
            }

            var vertNormals = (this.vertNormals != null ? this.vertNormals.Data : null);
            var vertTangents = (this.vertTangents != null ? this.vertTangents.Data : null);
            var vertUV2D = (this.vertUV2D != null ? this.vertUV2D.Data : null);
            var vertUV3D = (this.vertUV3D != null ? this.vertUV3D.Data : null);
            var vertUV4D = (this.vertUV4D != null ? this.vertUV4D.Data : null);
            var vertColors = (this.vertColors != null ? this.vertColors.Data : null);
            var vertBoneWeights = (this.vertBoneWeights != null ? this.vertBoneWeights.Data : null);

            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                if (!triangle.deleted)
                {
                    if (triangle.va0 != triangle.v0)
                    {
                        int iDest = triangle.va0;
                        int iSrc = triangle.v0;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v0 = triangle.va0;
                    }
                    if (triangle.va1 != triangle.v1)
                    {
                        int iDest = triangle.va1;
                        int iSrc = triangle.v1;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v1 = triangle.va1;
                    }
                    if (triangle.va2 != triangle.v2)
                    {
                        int iDest = triangle.va2;
                        int iSrc = triangle.v2;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v2 = triangle.va2;
                    }

                    triangles[dst++] = triangle;

                    vertices[triangle.v0].tcount = 1;
                    vertices[triangle.v1].tcount = 1;
                    vertices[triangle.v2].tcount = 1;
                }
            }

            triangleCount = dst;
            this.triangles.Resize(triangleCount);
            triangles = this.triangles.Data;

            dst = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                var vert = vertices[i];
                if (vert.tcount > 0)
                {
                    vert.tstart = dst;
                    vertices[i] = vert;

                    if (dst != i)
                    {
                        vertices[dst].p = vert.p;
                        if (vertNormals != null) vertNormals[dst] = vertNormals[i];
                        if (vertTangents != null) vertTangents[dst] = vertTangents[i];
                        if (vertUV2D != null)
                        {
                            for (int j = 0; j < Mesh.UVChannelCount; j++)
                            {
                                var vertUV = vertUV2D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertUV3D != null)
                        {
                            for (int j = 0; j < Mesh.UVChannelCount; j++)
                            {
                                var vertUV = vertUV3D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertUV4D != null)
                        {
                            for (int j = 0; j < Mesh.UVChannelCount; j++)
                            {
                                var vertUV = vertUV4D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertColors != null) vertColors[dst] = vertColors[i];
                        if (vertBoneWeights != null) vertBoneWeights[dst] = vertBoneWeights[i];
                    }
                    ++dst;
                }
            }

            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                triangle.v0 = vertices[triangle.v0].tstart;
                triangle.v1 = vertices[triangle.v1].tstart;
                triangle.v2 = vertices[triangle.v2].tstart;
                triangles[i] = triangle;
            }

            vertexCount = dst;
            this.vertices.Resize(vertexCount);
            if (vertNormals != null) this.vertNormals.Resize(vertexCount, true);
            if (vertTangents != null) this.vertTangents.Resize(vertexCount, true);
            if (vertUV2D != null) this.vertUV2D.Resize(vertexCount, true);
            if (vertUV3D != null) this.vertUV3D.Resize(vertexCount, true);
            if (vertUV4D != null) this.vertUV4D.Resize(vertexCount, true);
            if (vertColors != null) this.vertColors.Resize(vertexCount, true);
            if (vertBoneWeights != null) this.vertBoneWeights.Resize(vertexCount, true);
        }
        #endregion
        #endregion

        #region Public Methods
        #region Initialize
        /// <summary>
        /// Initializes the algorithm with the original mesh.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public override void Initialize(Mesh mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            int meshSubMeshCount = mesh.SubMeshCount;
            int meshTriangleCount = mesh.TriangleCount;
            var meshVertices = mesh.Vertices;
            var meshNormals = mesh.Normals;
            var meshTangents = mesh.Tangents;
            var meshColors = mesh.Colors;
            var meshBoneWeights = mesh.BoneWeights;
            subMeshCount = meshSubMeshCount;

            vertices.Resize(meshVertices.Length);
            var vertArr = vertices.Data;
            for (int i = 0; i < meshVertices.Length; i++)
            {
                vertArr[i] = new Vertex(meshVertices[i]);
            }

            triangles.Resize(meshTriangleCount);
            var trisArr = triangles.Data;
            int triangleIndex = 0;
            for (int subMeshIndex = 0; subMeshIndex < meshSubMeshCount; subMeshIndex++)
            {
                int[] subMeshIndices = mesh.GetIndices(subMeshIndex);
                int subMeshTriangleCount = subMeshIndices.Length / 3;
                for (int i = 0; i < subMeshTriangleCount; i++)
                {
                    int offset = i * 3;
                    int v0 = subMeshIndices[offset];
                    int v1 = subMeshIndices[offset + 1];
                    int v2 = subMeshIndices[offset + 2];
                    trisArr[triangleIndex++] = new Triangle(v0, v1, v2, subMeshIndex);
                }
            }

            vertNormals = InitializeVertexAttribute(meshNormals, "normals");
            vertTangents = InitializeVertexAttribute(meshTangents, "tangents");
            vertColors = InitializeVertexAttribute(meshColors, "colors");
            vertBoneWeights = InitializeVertexAttribute(meshBoneWeights, "boneWeights");

            for (int i = 0; i < Mesh.UVChannelCount; i++)
            {
                int uvDim = mesh.GetUVDimension(i);
                string uvAttributeName = string.Format("uv{0}", i);
                if (uvDim == 2)
                {
                    if (vertUV2D == null)
                        vertUV2D = new UVChannels<Vector2>();

                    var uvs = mesh.GetUVs2D(i);
                    vertUV2D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
                else if (uvDim == 3)
                {
                    if (vertUV3D == null)
                        vertUV3D = new UVChannels<Vector3>();

                    var uvs = mesh.GetUVs3D(i);
                    vertUV3D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
                else if (uvDim == 4)
                {
                    if (vertUV4D == null)
                        vertUV4D = new UVChannels<Vector4>();

                    var uvs = mesh.GetUVs4D(i);
                    vertUV4D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
            }
        }
        #endregion

        #region Decimate Mesh
        /// <summary>
        /// Decimates the mesh using Blender-style min-heap edge collapse.
        /// Single pass: build heap once, greedily collapse cheapest edge,
        /// update only affected neighbors (bmesh_decimate_collapse.cc:1357-1383).
        /// Quadrics are accumulated during collapse, not rebuilt from scratch.
        /// </summary>
        public override void DecimateMesh(int targetTrisCount)
        {
            if (targetTrisCount < 0)
                throw new ArgumentOutOfRangeException("targetTrisCount");

            int vertexCount = this.vertices.Length;
            var vertices = this.vertices.Data;
            int triangleCount = this.triangles.Length;
            int startTrisCount = triangleCount;

            int maxVertexCount = base.MaxVertexCount;

            // Phase 1: Build reference list
            UpdateReferences();

            // Phase 2: Detect borders + smart link (one-time, like Blender initialization)
            InitBordersAndSmartLink(vertexCount, triangleCount);
            vertices = this.vertices.Data; // refresh after modification

            // Phase 3: Build quadrics + boundary constraints (one-time)
            InitQuadricsAndBoundaryConstraints(vertexCount, triangleCount);
            vertices = this.vertices.Data;

            // Phase 4: Rebuild references after smart link modified triangle vertices
            UpdateReferences();
            vertices = this.vertices.Data;
            triangleCount = this.triangles.Length;

            // Phase 5: Initialize version counters and build heap
            vertexVersion = new long[vertexCount];
            var heap = new PriorityQueue<EdgeEntry, double>();
            BuildEdgeCosts(heap, vertexVersion);

            // Phase 6: Iterative edge collapse (Blender BM_mesh_decimate_collapse main loop)
            int deletedTris = 0;
            var deleted0 = new bool[64];
            var deleted1 = new bool[64];
            int collapseCount = 0;

            // Diagnostic counters
            int diagTotal = 0, diagStale = 0, diagDead = 0;
            int diagBorderMismatch = 0;
            int diagPreserveBorder = 0, diagPreserveSeam = 0, diagPreserveFoldover = 0;
            int diagFlip0 = 0, diagFlip1 = 0, diagTopology = 0, diagSuccess = 0;

            while ((startTrisCount - deletedTris) > targetTrisCount && heap.Count > 0)
            {
                var entry = heap.Dequeue();
                diagTotal++;

                // Stale check: vertex versions must match entry's versions
                if (vertexVersion[entry.v0] != entry.version0 || vertexVersion[entry.v1] != entry.version1)
                {
                    diagStale++;
                    continue;
                }

                // Both vertices must still be alive
                vertices = this.vertices.Data;
                if (vertices[entry.v0].tcount == 0 || vertices[entry.v1].tcount == 0)
                {
                    diagDead++;
                    continue;
                }

                int i0 = entry.v0;
                int i1 = entry.v1;

                // Constraint checks (same as Forstmann, checked at collapse time)
                if (vertices[i0].border != vertices[i1].border) { diagBorderMismatch++; continue; }
                if (base.PreserveBorders && vertices[i0].border) { diagPreserveBorder++; continue; }
                if (preserveSeams && vertices[i0].seam) { diagPreserveSeam++; continue; }
                if (preserveFoldovers && vertices[i0].foldover) { diagPreserveFoldover++; continue; }

                // Compute optimal collapse position
                Vector3d p;
                int pIndex;
                CalculateError(ref vertices[i0], ref vertices[i1], out p, out pIndex);

                // Resize deleted arrays if needed
                int tcount0 = vertices[i0].tcount;
                int tcount1 = vertices[i1].tcount;
                if (tcount0 > deleted0.Length) deleted0 = new bool[tcount0];
                if (tcount1 > deleted1.Length) deleted1 = new bool[tcount1];

                // Flip check (Blender bm_edge_collapse_is_degenerate_flip)
                if (Flipped(ref p, i0, i1, ref vertices[i0], deleted0))
                {
                    diagFlip0++;
                    continue;
                }
                if (Flipped(ref p, i1, i0, ref vertices[i1], deleted1))
                {
                    diagFlip1++;
                    continue;
                }

                // Degenerate topology check (Blender bm_edge_collapse_is_degenerate_topology)
                if (IsDegenerateTopology(i0, i1))
                {
                    diagTopology++;
                    continue;
                }

                // Find attribute indices from a triangle containing edge (i0, i1)
                int ia0 = i0, ia1 = i1;
                {
                    var refsData = this.refs.Data;
                    var triData = this.triangles.Data;
                    int ts0 = vertices[i0].tstart;
                    int tc0 = vertices[i0].tcount;
                    bool found = false;
                    for (int k = 0; k < tc0 && !found; k++)
                    {
                        Ref r = refsData[ts0 + k];
                        var t = triData[r.tid];
                        if (t.deleted) continue;
                        int s = r.tvertex;
                        int next = (s + 1) % 3;
                        int prev = (s + 2) % 3;
                        if (t[next] == i1)
                        {
                            ia0 = GetVertexAttrIndex(ref t, s);
                            ia1 = GetVertexAttrIndex(ref t, next);
                            found = true;
                        }
                        else if (t[prev] == i1)
                        {
                            ia0 = GetVertexAttrIndex(ref t, s);
                            ia1 = GetVertexAttrIndex(ref t, prev);
                            found = true;
                        }
                    }
                }

                // Compute interpolation factor (Blender USE_VERT_NORMAL_INTERP)
                double interpFactor = 0.5;
                if (pIndex == 2)
                {
                    interpFactor = CalculateInterpolationFactor(ref p, ref vertices[i0].p, ref vertices[i1].p);
                }

                // === COLLAPSE: merge v1 into v0 ===
                vertices[i0].p = p;
                vertices[i0].q += vertices[i1].q; // Accumulate quadric (Blender approach)

                // Update vertex attributes
                if (pIndex == 1)
                {
                    MoveVertexAttributes(ia0, ia1);
                }
                else if (pIndex == 2)
                {
                    MergeVertexAttributes(ia0, ia1, interpFactor);
                }

                int ia0Param = ia0;
                if (vertices[i0].seam)
                    ia0Param = -1;

                int tstart = refs.Length;
                UpdateTriangles(i0, ia0Param, ref vertices[i0], deleted0, ref deletedTris);
                UpdateTriangles(i0, ia0Param, ref vertices[i1], deleted1, ref deletedTris);

                int newTcount = refs.Length - tstart;
                if (newTcount <= vertices[i0].tcount)
                {
                    if (newTcount > 0)
                    {
                        var refsArr = refs.Data;
                        Array.Copy(refsArr, tstart, refsArr, vertices[i0].tstart, newTcount);
                    }
                }
                else
                {
                    vertices[i0].tstart = tstart;
                }

                vertices[i0].tcount = newTcount;
                --remainingVertices;

                // Increment versions for both vertices (invalidates all old heap entries)
                vertexVersion[i0]++;
                vertexVersion[i1]++;

                // Update costs for neighbor edges touching the kept vertex
                UpdateNeighborCosts(i0, heap, vertexVersion);

                // Check vertex count limit
                if (maxVertexCount > 0 && remainingVertices < maxVertexCount)
                    break;

                collapseCount++;
                diagSuccess++;
                if (collapseCount % 500 == 0)
                {
                    ReportStatus(collapseCount / 500, startTrisCount, startTrisCount - deletedTris, targetTrisCount);
                }
            }

            // Diagnostic output
            System.Console.WriteLine(
                $"[LOD Diag] target={targetTrisCount} start={startTrisCount} final={startTrisCount - deletedTris} success={diagSuccess} heapRemain={heap.Count}\n" +
                $"  stale={diagStale} dead={diagDead} borderMismatch={diagBorderMismatch}\n" +
                $"  preserveBorder={diagPreserveBorder} preserveSeam={diagPreserveSeam} preserveFoldover={diagPreserveFoldover}\n" +
                $"  flip0={diagFlip0} flip1={diagFlip1} topology={diagTopology} totalPopped={diagTotal}");

            CompactMesh();
        }
        #endregion

        #region Decimate Mesh Lossless
        /// <summary>
        /// Decimates the mesh without losing any quality.
        /// Uses min-heap: collapse all edges with cost below DoubleEpsilon.
        /// </summary>
        public override void DecimateMeshLossless()
        {
            int vertexCount = this.vertices.Length;
            var vertices = this.vertices.Data;
            int triangleCount = this.triangles.Length;
            int startTrisCount = triangleCount;

            UpdateReferences();
            InitBordersAndSmartLink(vertexCount, triangleCount);
            vertices = this.vertices.Data;

            InitQuadricsAndBoundaryConstraints(vertexCount, triangleCount);
            vertices = this.vertices.Data;

            UpdateReferences();
            vertices = this.vertices.Data;
            triangleCount = this.triangles.Length;

            vertexVersion = new long[vertexCount];
            var heap = new PriorityQueue<EdgeEntry, double>();
            BuildEdgeCosts(heap, vertexVersion);

            int deletedTris = 0;
            var deleted0 = new bool[64];
            var deleted1 = new bool[64];

            ReportStatus(0, startTrisCount, startTrisCount, -1);

            while (heap.Count > 0)
            {
                // Peek at cheapest — if above threshold, stop
                if (heap.TryPeek(out _, out double topCost) && topCost > DoubleEpsilon)
                    break;

                var entry = heap.Dequeue();

                if (vertexVersion[entry.v0] != entry.version0 || vertexVersion[entry.v1] != entry.version1)
                    continue;

                vertices = this.vertices.Data;
                if (vertices[entry.v0].tcount == 0 || vertices[entry.v1].tcount == 0)
                    continue;

                int i0 = entry.v0;
                int i1 = entry.v1;

                if (vertices[i0].border != vertices[i1].border) continue;
                if (vertices[i0].seam != vertices[i1].seam) continue;
                if (vertices[i0].foldover != vertices[i1].foldover) continue;
                if (base.PreserveBorders && vertices[i0].border) continue;
                if (preserveSeams && vertices[i0].seam) continue;
                if (preserveFoldovers && vertices[i0].foldover) continue;

                Vector3d p;
                int pIndex;
                CalculateError(ref vertices[i0], ref vertices[i1], out p, out pIndex);

                int tcount0 = vertices[i0].tcount;
                int tcount1 = vertices[i1].tcount;
                if (tcount0 > deleted0.Length) deleted0 = new bool[tcount0];
                if (tcount1 > deleted1.Length) deleted1 = new bool[tcount1];

                if (Flipped(ref p, i0, i1, ref vertices[i0], deleted0))
                    continue;
                if (Flipped(ref p, i1, i0, ref vertices[i1], deleted1))
                    continue;

                if (IsDegenerateTopology(i0, i1))
                    continue;

                int ia0 = i0, ia1 = i1;
                {
                    var refsData = this.refs.Data;
                    var triData = this.triangles.Data;
                    int ts0 = vertices[i0].tstart;
                    int tc0 = vertices[i0].tcount;
                    bool found = false;
                    for (int k = 0; k < tc0 && !found; k++)
                    {
                        Ref r = refsData[ts0 + k];
                        var t = triData[r.tid];
                        if (t.deleted) continue;
                        int s = r.tvertex;
                        int next = (s + 1) % 3;
                        int prev = (s + 2) % 3;
                        if (t[next] == i1)
                        {
                            ia0 = GetVertexAttrIndex(ref t, s);
                            ia1 = GetVertexAttrIndex(ref t, next);
                            found = true;
                        }
                        else if (t[prev] == i1)
                        {
                            ia0 = GetVertexAttrIndex(ref t, s);
                            ia1 = GetVertexAttrIndex(ref t, prev);
                            found = true;
                        }
                    }
                }

                double interpFactor = 0.5;
                if (pIndex == 2)
                    interpFactor = CalculateInterpolationFactor(ref p, ref vertices[i0].p, ref vertices[i1].p);

                vertices[i0].p = p;
                vertices[i0].q += vertices[i1].q;

                if (pIndex == 1)
                    MoveVertexAttributes(ia0, ia1);
                else if (pIndex == 2)
                    MergeVertexAttributes(ia0, ia1, interpFactor);

                int ia0Param = ia0;
                if (vertices[i0].seam)
                    ia0Param = -1;

                int tstart = refs.Length;
                UpdateTriangles(i0, ia0Param, ref vertices[i0], deleted0, ref deletedTris);
                UpdateTriangles(i0, ia0Param, ref vertices[i1], deleted1, ref deletedTris);

                int newTcount = refs.Length - tstart;
                if (newTcount <= vertices[i0].tcount)
                {
                    if (newTcount > 0)
                    {
                        var refsArr = refs.Data;
                        Array.Copy(refsArr, tstart, refsArr, vertices[i0].tstart, newTcount);
                    }
                }
                else
                {
                    vertices[i0].tstart = tstart;
                }

                vertices[i0].tcount = newTcount;
                --remainingVertices;

                vertexVersion[i0]++;
                vertexVersion[i1]++;

                UpdateNeighborCosts(i0, heap, vertexVersion);

                if (Verbose)
                    Logging.LogVerbose("Lossless collapse {0} - triangles {1}", deletedTris, startTrisCount - deletedTris);
            }

            CompactMesh();
        }
        #endregion

        #region To Mesh
        /// <summary>
        /// Returns the resulting mesh.
        /// </summary>
        /// <returns>The resulting mesh.</returns>
        public override Mesh ToMesh()
        {
            int vertexCount = this.vertices.Length;
            int triangleCount = this.triangles.Length;
            var vertices = new Vector3d[vertexCount];
            var indices = new int[subMeshCount][];

            var vertArr = this.vertices.Data;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = vertArr[i].p;
            }

            // First get the sub-mesh offsets
            var triArr = this.triangles.Data;
            int[] subMeshOffsets = new int[subMeshCount];
            int lastSubMeshOffset = -1;
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triArr[i];
                if (triangle.subMeshIndex != lastSubMeshOffset)
                {
                    for (int j = lastSubMeshOffset + 1; j < triangle.subMeshIndex; j++)
                    {
                        subMeshOffsets[j] = i;
                    }
                    subMeshOffsets[triangle.subMeshIndex] = i;
                    lastSubMeshOffset = triangle.subMeshIndex;
                }
            }
            for (int i = lastSubMeshOffset + 1; i < subMeshCount; i++)
            {
                subMeshOffsets[i] = triangleCount;
            }

            // Then setup the sub-meshes
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                int startOffset = subMeshOffsets[subMeshIndex];
                if (startOffset < triangleCount)
                {
                    int endOffset = ((subMeshIndex + 1) < subMeshCount ? subMeshOffsets[subMeshIndex + 1] : triangleCount);
                    int subMeshTriangleCount = endOffset - startOffset;
                    if (subMeshTriangleCount < 0) subMeshTriangleCount = 0;
                    int[] subMeshIndices = new int[subMeshTriangleCount * 3];

                    for (int triangleIndex = startOffset; triangleIndex < endOffset; triangleIndex++)
                    {
                        var triangle = triArr[triangleIndex];
                        int offset = (triangleIndex - startOffset) * 3;
                        subMeshIndices[offset] = triangle.v0;
                        subMeshIndices[offset + 1] = triangle.v1;
                        subMeshIndices[offset + 2] = triangle.v2;
                    }

                    indices[subMeshIndex] = subMeshIndices;
                }
                else
                {
                    // This mesh doesn't have any triangles left
                    indices[subMeshIndex] = new int[0];
                }
            }

            Mesh newMesh = new Mesh(vertices, indices);

            if (vertNormals != null)
            {
                newMesh.Normals = vertNormals.Data;
            }
            if (vertTangents != null)
            {
                newMesh.Tangents = vertTangents.Data;
            }
            if (vertColors != null)
            {
                newMesh.Colors = vertColors.Data;
            }
            if (vertBoneWeights != null)
            {
                newMesh.BoneWeights = vertBoneWeights.Data;
            }

            if (vertUV2D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    if (vertUV2D[i] != null)
                    {
                        var uvSet = vertUV2D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            if (vertUV3D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    if (vertUV3D[i] != null)
                    {
                        var uvSet = vertUV3D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            if (vertUV4D != null)
            {
                for (int i = 0; i < Mesh.UVChannelCount; i++)
                {
                    if (vertUV4D[i] != null)
                    {
                        var uvSet = vertUV4D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            return newMesh;
        }
        #endregion
        #endregion
    }
}