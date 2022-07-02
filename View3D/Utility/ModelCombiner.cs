using System.Collections.Generic;
using System.Linq;
using View3D.SceneNodes;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace View3D.Utility
{
    public class ModelCombiner
    {
        public bool CanCombine(List<Rmv2MeshNode> items, out ErrorList errors)
        {
            errors = new ErrorList();
            foreach (var outerLoopItem in items)
            {
                foreach (var innerLoopItem in items)
                {
                    if (outerLoopItem == innerLoopItem)
                        continue;

                    var name0 = outerLoopItem.Name;
                    var name1 = innerLoopItem.Name;

                    // Textures
                    if (!ValidateTextures(outerLoopItem, name0, innerLoopItem, name1, out string textureErrorMsg))
                        errors.Error("Texture", textureErrorMsg);

                    // Vertex type
                    if (outerLoopItem.Geometry.VertexFormat != innerLoopItem.Geometry.VertexFormat)
                        errors.Error("VertexType", $"{name0} has a different vertex type then {name1}");

                    // Alpha mode
                    if (outerLoopItem.Material.AlphaMode != innerLoopItem.Material.AlphaMode)
                        errors.Error("AlphaSettings mode", $"{name0} has a different AlphaSettings mode then {name1}");
                }
            }

            return errors.Errors.Count == 0;
        }

        public static bool CanCombine(Rmv2MeshNode meshA, Rmv2MeshNode meshB, out string errorMessage)
        {
            // Textures
            if (!ValidateTextures(meshA, meshA.Name, meshB, meshB.Name, out string textureErrorMsg))
            {
                errorMessage = "Texture - " + textureErrorMsg;
                return false;
            }

            // Vertex type
            if (meshA.Geometry.VertexFormat != meshB.Geometry.VertexFormat)
            {
                errorMessage = "VertexType - " + $"{meshA.Name} has a different vertex type then {meshB.Name}";
                return false;
            }

            // Alpha mode
            if (meshA.Material.AlphaMode != meshB.Material.AlphaMode)
            {
                errorMessage = "Alpha Mode - " + $"{meshA.Name} has a different AlphaSettings then {meshB.Name}";
                return false;
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
                bool foundMeshToCombineWith = false;
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

        public static List<Rmv2MeshNode> CombineMeshes(List<Rmv2MeshNode> geometriesToCombine)
        {
            var combinedMeshes = new List<Rmv2MeshNode>();
            var combineGroups = SortMeshesIntoCombinableGroups(geometriesToCombine);
            foreach (var currentGroup in combineGroups)
            {
                if (currentGroup.Count != 1)
                {
                    var combinedMesh = SceneNodeHelper.CloneNode(currentGroup.First());
                    combinedMesh.Name = currentGroup.First().Name + "_Combined";

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

        private static bool ValidateTextures(Rmv2MeshNode item0, string item0Name, Rmv2MeshNode item1, string item1Name, out string textureErrorMsg)
        {
            var textureList0 = item0.GetTextures();
            var textureList1 = item1.GetTextures();
            if (textureList0.Count != textureList1.Count())
            {
                textureErrorMsg = $"{item0Name} has a different number of textures then {item1Name}";
                return false;
            }

            foreach (var item in textureList0)
            {
                if (textureList1.ContainsKey(item.Key) && textureList1[item.Key] == textureList0[item.Key])
                    continue;

                textureErrorMsg = $"{item1Name} does not share the same {item.Key} texture";
                return false;
            }

            textureErrorMsg = null;
            return true;
        }






    }
}
