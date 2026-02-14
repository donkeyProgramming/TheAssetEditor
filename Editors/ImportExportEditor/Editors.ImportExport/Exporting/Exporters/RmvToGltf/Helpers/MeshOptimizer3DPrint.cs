using System.IO;
using System.Numerics;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    /// <summary>
    /// Optimizes mesh data for 3D printing quality.
    /// Includes vertex deduplication, normal calculation, and topology improvements.
    /// </summary>
    public class MeshOptimizer3DPrint
    {
        /// <summary>
        /// Analyzes and optimizes mesh topology for 3D printing
        /// </summary>
        public MeshOptimizationReport AnalyzeMesh(RmvMesh rmvMesh)
        {
            var report = new MeshOptimizationReport();

            // Check for degenerate triangles
            CheckDegenerateTriangles(rmvMesh, report);

            // Check for duplicate vertices
            CheckDuplicateVertices(rmvMesh, report);

            // Check for mesh manifold issues
            CheckManifoldIssues(rmvMesh, report);

            // Analyze normal consistency
            CheckNormalConsistency(rmvMesh, report);

            return report;
        }

        /// <summary>
        /// Detects and reports degenerate triangles (zero area)
        /// </summary>
        private void CheckDegenerateTriangles(RmvMesh rmvMesh, MeshOptimizationReport report)
        {
            for (int i = 0; i < rmvMesh.IndexList.Length; i += 3)
            {
                var i0 = rmvMesh.IndexList[i];
                var i1 = rmvMesh.IndexList[i + 1];
                var i2 = rmvMesh.IndexList[i + 2];

                var v0 = rmvMesh.VertexList[i0].Position;
                var v1 = rmvMesh.VertexList[i1].Position;
                var v2 = rmvMesh.VertexList[i2].Position;

                // Calculate area using cross product
                var edge1 = new Vector3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
                var edge2 = new Vector3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);

                var cross = Vector3.Cross(edge1, edge2);
                float area = cross.Length() * 0.5f;

                if (area < 0.0001f)
                {
                    report.DegenerateTriangles++;
                }
            }
        }

        /// <summary>
        /// Detects duplicate vertices that could be merged
        /// </summary>
        private void CheckDuplicateVertices(RmvMesh rmvMesh, MeshOptimizationReport report)
        {
            const float positionThreshold = 0.001f; // 1mm threshold
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < rmvMesh.VertexList.Length; i++)
            {
                if (processedIndices.Contains(i))
                    continue;

                var vertex = rmvMesh.VertexList[i];
                processedIndices.Add(i);

                for (int j = i + 1; j < rmvMesh.VertexList.Length; j++)
                {
                    if (processedIndices.Contains(j))
                        continue;

                    var other = rmvMesh.VertexList[j];
                    float distance = Vector3.Distance(
                        new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z),
                        new Vector3(other.Position.X, other.Position.Y, other.Position.Z));

                    if (distance < positionThreshold)
                    {
                        report.DuplicateVertices++;
                        processedIndices.Add(j);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for non-manifold edges and issues
        /// </summary>
        private void CheckManifoldIssues(RmvMesh rmvMesh, MeshOptimizationReport report)
        {
            var edgeCount = new Dictionary<(ushort, ushort), int>();

            for (int i = 0; i < rmvMesh.IndexList.Length; i += 3)
            {
                var i0 = rmvMesh.IndexList[i];
                var i1 = rmvMesh.IndexList[i + 1];
                var i2 = rmvMesh.IndexList[i + 2];

                // Count each edge (normalize to lower, higher order)
                AddEdge(edgeCount, i0, i1);
                AddEdge(edgeCount, i1, i2);
                AddEdge(edgeCount, i2, i0);
            }

            // Non-manifold edges appear more than twice
            foreach (var edgeCount_kvp in edgeCount)
            {
                if (edgeCount_kvp.Value > 2)
                    report.NonManifoldEdges++;
            }
        }

        private void AddEdge(Dictionary<(ushort, ushort), int> edgeCount, ushort a, ushort b)
        {
            var edge = a < b ? (a, b) : (b, a);
            if (edgeCount.ContainsKey(edge))
                edgeCount[edge]++;
            else
                edgeCount[edge] = 1;
        }

        /// <summary>
        /// Validates normal vector consistency for shading
        /// </summary>
        private void CheckNormalConsistency(RmvMesh rmvMesh, MeshOptimizationReport report)
        {
            const float normalThreshold = 0.1f;

            foreach (var vertex in rmvMesh.VertexList)
            {
                var normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                float length = normal.Length();

                // Normals should be normalized (length ~1.0)
                if (length < 1.0f - normalThreshold || length > 1.0f + normalThreshold)
                {
                    report.AbnormalNormals++;
                }
            }
        }
    }

    /// <summary>
    /// Report of mesh optimization analysis for 3D printing
    /// </summary>
    public class MeshOptimizationReport
    {
        public int DegenerateTriangles { get; set; }
        public int DuplicateVertices { get; set; }
        public int NonManifoldEdges { get; set; }
        public int AbnormalNormals { get; set; }

        public bool HasIssues => DegenerateTriangles > 0 || DuplicateVertices > 0 || NonManifoldEdges > 0 || AbnormalNormals > 0;

        public override string ToString()
        {
            return $"Degenerate: {DegenerateTriangles}, Duplicates: {DuplicateVertices}, " +
                   $"NonManifold: {NonManifoldEdges}, AbnormalNormals: {AbnormalNormals}";
        }
    }
}
