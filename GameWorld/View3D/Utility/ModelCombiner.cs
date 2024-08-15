using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Utility
{
    public class ModelCombiner
    {
        public static bool CanCombine(Rmv2MeshNode meshA, Rmv2MeshNode meshB, out string? errorMessage)
        {
            if (AreMaterialsEqual(meshA.Name, meshA.Effect, meshB.Effect, meshB.Name, out var textureErrorMsg) == false)
            { 
                errorMessage = "Material - " + textureErrorMsg;
                return false;
            }

            // Vertex type
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
            if (materialA.Type != materialB.Type)
            {
                errorMessage = $"{meshNameA} uses material {materialA.Type}, {meshNameB} uses material {materialB.Type}";
                return false; ;
            }

            var baseCapA = materialA.GetCapability<MaterialBaseCapability>();
            var baseCapB = materialB.GetCapability<MaterialBaseCapability>();
            if (baseCapA.UseAlpha != baseCapB.UseAlpha)
            {
                errorMessage = $"{meshNameA} uses Alpha {baseCapA.UseAlpha}, {meshNameB} uses Alpha {baseCapB.UseAlpha}";
                return false;
            }

            var specGlossA = materialA.TryGetCapability<SpecGlossCapability>();
            var specGlossB = materialB.TryGetCapability<SpecGlossCapability>();
            if (specGlossA != null && specGlossB != null)
            {
                if (SpecGlossCapability.AreEqual(specGlossA, specGlossB) == false)
                {
                    errorMessage = $"{meshNameA} uses different textures than {meshNameB}";
                    return false;
                }
            }

            var metalRoughA = materialA.TryGetCapability<MetalRoughCapability>();
            var metalRoughB = materialB.TryGetCapability<MetalRoughCapability>();
            if (metalRoughA != null && metalRoughB != null)
            {
                if (MetalRoughCapability.AreEqual(metalRoughA, metalRoughB) == false)
                {
                    errorMessage = $"{meshNameA} uses different textures than {meshNameB}";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

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

        public static List<List<Rmv2MeshNode>> SortMeshesIntoCombinableGroups(List<Rmv2MeshNode> meshList)
        {
            var groupedOutput = new List<List<Rmv2MeshNode>>();
            foreach (var currentMesh in meshList)
            {
                var foundMeshToCombineWith = false;
                foreach (var potentialCombineTargetGroup in groupedOutput)
                {
                    var canCombine = CanCombine(potentialCombineTargetGroup.First(), currentMesh, out _);
                    if (canCombine)
                    {
                        potentialCombineTargetGroup.Add(currentMesh);
                        foundMeshToCombineWith = true;
                    }
                }

                if (foundMeshToCombineWith == false)
                    groupedOutput.Add(new List<Rmv2MeshNode>() { currentMesh });
            }

            return groupedOutput;
        }

        public static List<Rmv2MeshNode> CombineMeshes(List<Rmv2MeshNode> geometriesToCombine, bool addPrefix = false)
        {
            var combinedMeshes = new List<Rmv2MeshNode>();
            var combineGroups = SortMeshesIntoCombinableGroups(geometriesToCombine);
            foreach (var currentGroup in combineGroups)
            {
                if (currentGroup.Count != 1)
                {
                    var combinedMesh = SceneNodeHelper.CloneNode(currentGroup.First());
                    combinedMesh.Name = currentGroup.First().Name;
                    if (addPrefix)
                        combinedMesh.Name += "_Combined";

                    var newModel = currentGroup.First().Geometry.Clone();
                    var typedGeo = currentGroup.Select(x => x.Geometry);
                    combinedMesh.Geometry = newModel;

                    var geoList = currentGroup.Skip(1).Select(x => x.Geometry).ToList();
                    newModel.Merge(geoList);

                    combinedMeshes.Add(combinedMesh);
                }
                else
                {
                    combinedMeshes.Add(currentGroup.First());
                }
            }

            return combinedMeshes;
        }
    }
}
