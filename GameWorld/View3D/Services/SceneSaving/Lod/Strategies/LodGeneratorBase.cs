using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Shared.Core.ErrorHandling;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{
    public abstract class LodGeneratorBase
    {
        protected abstract void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio);

        private void DeleteAllLods(List<Rmv2LodNode> lodRootNodes)
        {
            foreach (var lod in lodRootNodes)
            {
                var itemsToDelete = new List<ISceneNode>();
                foreach (var child in lod.Children)
                    itemsToDelete.Add(child);

                foreach (var child in itemsToDelete)
                    child.Parent.RemoveObject(child);
            }
        }

        public void CreateLodsForRootNode(Rmv2ModelNode rootNode, List<LodGenerationSettings> lodGenerationSettings)
        {
            var lods = rootNode.GetLodNodes();
            var generationSource = lods.First();
            var lodsToRemove = lods
                .Skip(1)
                .Take(rootNode.Children.Count - 1)
                .ToList();

            // Delete all the lods
            DeleteAllLods(lodsToRemove);

            var meshList = generationSource.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
            var generatedLod = CreateLods(meshList, lodGenerationSettings);

            for (var i = 0; i < lodsToRemove.Count; i++)
            {
                var newLodNode = (Rmv2LodNode)generationSource.CreateCopyInstance();
                generationSource.CopyInto(newLodNode);
                newLodNode.LodValue = i + 1;
                newLodNode.Name = "Lod " + newLodNode.LodValue;

                var newNodeMeshList = generationSource.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
                var generatedLod = CreateLods(meshList, lodGenerationSettings);

                foreach (var mesh in generatedLod[i])
                    lodsToRemove[i].AddObject(mesh);
            }
        }

        List<Rmv2MeshNode[]> CreateLods(List<Rmv2MeshNode> originalModel, List<LodGenerationSettings> settings)
        {
            var output = new List<Rmv2MeshNode[]>();
            for (var lodIndex = 1; lodIndex < settings.Count; lodIndex++)
            {
                var deductionRatio = settings[lodIndex].LodRectionFactor;
                var optimize = settings[lodIndex].OptimizeAlpha || settings[lodIndex].OptimizeVertex;

                // We want to work on a clone of all the meshes
                var originalMeshClone = originalModel.Select(x => SceneNodeHelper.CloneNode(x)).ToList();
                foreach (var mesh in originalMeshClone)
                    mesh.Name = mesh.Material.ModelName;

                if (optimize)
                {
                    foreach (var mesh in originalMeshClone)
                    {
                        if (mesh.Geometry.VertexFormat != UiVertexFormat.Static)
                            mesh.Geometry.ChangeVertexType(UiVertexFormat.Weighted, mesh.Geometry.ParentSkeletonName);

                        if (settings[lodIndex].OptimizeAlpha)
                            mesh.Material.AlphaMode = AlphaMode.Opaque;
                    }

                    // Combine if possible 
                    var errorList = new ErrorList();
                    var canCombine = ModelCombiner.HasPotentialCombineMeshes(originalMeshClone, out errorList);
                    if (canCombine)
                        originalMeshClone = ModelCombiner.CombineMeshes(originalMeshClone, addPrefix: false);
                }

                // Reduce the polygon count
                foreach (var mesh in originalMeshClone)
                {
                    if (mesh.ReduceMeshOnLodGeneration && settings[lodIndex].LodRectionFactor != 1)
                        ReduceMesh(mesh, deductionRatio);
                }

                output.Add(originalMeshClone.ToArray());
            }

            return output;
        }
    }
}
