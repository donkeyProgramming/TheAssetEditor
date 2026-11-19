using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Utility
{
    public class ModelCombiner
    {

        public static bool HasPotentialCombineMeshes(List<Rmv2MeshNode> meshList, out ErrorList out_errors)
        {
            out_errors = new ErrorList();

            var combineGroups = SortMeshesIntoCombinableGroups(meshList);
            var combineGroupLengths = combineGroups.Select(x => x.Count).Distinct();
            if (combineGroupLengths.Count() == 1 && combineGroupLengths.First() == 1)
            {
                var meshes = combineGroups.Select(x => x.First());
                foreach (var outerMesh in meshes)
                {
                    foreach (var innerMesh in meshes)
                    {
                        if (innerMesh == outerMesh)
                            continue;

                        if (CanCombine(innerMesh, outerMesh, out var errorStr) == false)
                            out_errors.Error("Error", errorStr);

                    }
                }

                return false;
            }

            return true;
        }
   
        // Merge a set of MeshNodes. 
        // A new set of merged nodes are created. The caller needs to handle the memory of the old nodes.
        public static List<Rmv2MeshNode> CombineMeshes(List<Rmv2MeshNode> geometriesToCombine, bool addPrefix = false)
        {
            var combinedMeshes = new List<Rmv2MeshNode>();
            var combineGroups = SortMeshesIntoCombinableGroups(geometriesToCombine);
            foreach (var currentGroup in combineGroups)
            {
                if (currentGroup.Count != 1)
                {
                    // Clone the first mesh
                    var combinedMesh = SceneNodeHelper.CloneNode(currentGroup.First());
                    combinedMesh.Name = currentGroup.First().Name;
                    if (addPrefix)
                        combinedMesh.Name += "_Combined";

                    // Merge the rest of the meshes into it
                    var geoList = currentGroup.Skip(1).Select(x => x.Geometry).ToList();
                    combinedMesh.Geometry.Merge(geoList);

                    combinedMeshes.Add(combinedMesh);
                }
                else
                {
                    combinedMeshes.Add(currentGroup.First());
                }
            }

            return combinedMeshes;
        }

        // One error per set of combinable meshes whose vertices together are more than one mesh can address
        public static ErrorList GetVertexLimitErrors(List<Rmv2MeshNode> meshList)
        {
            var errors = new ErrorList();
            foreach (var group in SortMeshesIntoCombinableGroups(meshList, int.MaxValue))
            {
                var vertexCount = group.Sum(x => x.Geometry.VertexCount());
                if (group.Count > 1 && vertexCount > RmvMesh.MaxVertexCount)
                    errors.Error("Vertex limit exceeded", $"Combining {string.Join(", ", group.Select(x => x.Name))} would make a mesh with {vertexCount} vertices, but a mesh can have at most {RmvMesh.MaxVertexCount}");
            }

            return errors;
        }

        // A mesh joins the first group it can combine with that still has room for its vertices
        static List<List<Rmv2MeshNode>> SortMeshesIntoCombinableGroups(List<Rmv2MeshNode> meshList, int maxVertexCount = RmvMesh.MaxVertexCount)
        {
            var groupedOutput = new List<List<Rmv2MeshNode>>();
            foreach (var currentMesh in meshList)
            {
                var group = groupedOutput.FirstOrDefault(x =>
                    CanCombine(x.First(), currentMesh, out _) &&
                    x.Sum(mesh => mesh.Geometry.VertexCount()) + currentMesh.Geometry.VertexCount() <= maxVertexCount);

                if (group != null)
                    group.Add(currentMesh);
                else
                    groupedOutput.Add(new List<Rmv2MeshNode>() { currentMesh });
            }

            return groupedOutput;
        }

        static bool CanCombine(Rmv2MeshNode meshA, Rmv2MeshNode meshB, out string? errorMessage)
        {
            if (AreMaterialsEqual(meshA.Name, meshA.Material, meshB.Material, meshB.Name, out var textureErrorMsg) == false)
            {
                errorMessage = "Material - " + textureErrorMsg;
                return false;
            }

            if (meshA.Geometry.VertexFormat != meshB.Geometry.VertexFormat)
            {
                errorMessage = "VertexType - " + $"{meshA.Name} has a different vertex type then {meshB.Name}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        static bool AreMaterialsEqual(string meshNameA, CapabilityMaterial materialA, CapabilityMaterial materialB, string meshNameB, out string? errorMessage)
        {
            var res = materialA.AreEqual(materialB);
            if (res.Result == false)
            {
                errorMessage = $"Comparing material for mesh {meshNameA} against {meshNameB} : {res.Message}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
