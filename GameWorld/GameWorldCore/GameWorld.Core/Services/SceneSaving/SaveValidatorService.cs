using System.Linq;
using CommonControls.BaseDialogs.ErrorListDialog;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving
{
    public class SaveValidatorService
    {
        static ErrorList Validate(MainEditableNode mainNode)
        {
            var errorList = new ErrorList();

            var skeleton = mainNode.SkeletonNode.Skeleton;
            var meshes = mainNode.GetMeshNodes(0);

            // Different skeletons
            if (skeleton != null)
            {
                var activeSkeletonName = skeleton.SkeletonName;
                var skeletonNames = meshes.Select(x => x.Geometry.SkeletonName).Distinct().ToList();

                if (skeletonNames.Count != 1)
                    errorList.Error("Skeleton", "Model contains meshes with multiple skeleton references. They will not animate well in game");

                skeletonNames.Remove(activeSkeletonName);
                if (skeletonNames.Count != 0)
                    errorList.Error("Skeleton", "Model contains meshes that have not been re-rigged. They will not behave well in game");
            }

            // Mismatch between static and animated vertex
            var vertexTypes = meshes.Select(x => x.Geometry.VertexFormat).Distinct().ToList();
            if (vertexTypes.Contains(UiVertexFormat.Static) && skeleton != null)
                errorList.Error("Vertex", "Model has a skeleton, but contains meshes with non-animated vertexes. Rig them or they will not behave as expected in game");

            if ((vertexTypes.Contains(UiVertexFormat.Weighted) || vertexTypes.Contains(UiVertexFormat.Cinematic)) && skeleton == null)
                errorList.Error("Vertex", "Model does not have a skeleton, but has animated vertex data.");

            // Large model count
            if (meshes.Count > 50)
                errorList.Warning("Mesh Count", "Model contains a large amount of meshes, might cause performance issues");

            if (ModelCombiner.HasPotentialCombineMeshes(meshes, out _))
                errorList.Warning("Mesh", "Model contains multiple meshes that can be merged. Consider merging them for performance reasons");

            // Different pivots
            var pivots = meshes.Select(x => x.PivotPoint).Distinct().ToList();
            if (pivots.Count != 1)
                errorList.Warning("Pivot Point", "Model contains multiple different pivot points, this is almost always not intended");

            // Animation and Pivotpoint
            if (pivots.Count == 1 && skeleton != null)
            {
                if ((pivots.First().X == 0 && pivots.First().Y == 0 && pivots.First().Z == 0) == false)
                    errorList.Warning("Pivot Point", "Model contains a non zero pivot point and animation, this is almost always not intended");
            }

            return errorList;
        }

        public void DisplayValidateDialog(MainEditableNode mainEditableNode)
        {
            var errorList = Validate(mainEditableNode);
            if (errorList.HasData)
                ErrorListWindow.ShowDialog("Potential problems", errorList);
        }
    }
}
