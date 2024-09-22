using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Rendering.Materials.Capabilities;
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

                var numLods = lodRootNodes.Count;
                for (var i = 0; i < numLods; i++)
                    lodRootNodes[i].Parent.RemoveObject(lodRootNodes[i]);
            }
        }

        private void DeleteAllMeshes(Rmv2LodNode meshNode)
        {
            foreach (var lod in meshNode.GetAllModels(false))
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

            for (var i = 1; i < lodGenerationSettings.Count; i++)
            {
                var newLodNode = (Rmv2LodNode)generationSource.CreateCopyInstance();
                generationSource.CopyInto(newLodNode);
                newLodNode.LodValue = i;
                newLodNode.Name = "Lod " + newLodNode.LodValue;
                rootNode.AddObject(newLodNode);

                var meshList = generationSource.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
                var generatedLod = CreateLods(meshList, lodGenerationSettings[i]);

                // Delete all models
                DeleteAllMeshes(newLodNode);

                // Add new meshes
                foreach (var mesh in generatedLod)
                    newLodNode.AddObject(mesh);
            }
        }

        List<Rmv2MeshNode> CreateLods(List<Rmv2MeshNode> originalModel, LodGenerationSettings settings)
        {
            var deductionRatio = settings.LodRectionFactor;
            var optimize = settings.OptimizeAlpha || settings.OptimizeVertex;

            // We want to work on a clone of all the meshes
            var originalMeshClone = originalModel.Select(x => SceneNodeHelper.CloneNode(x)).ToList();

            if (optimize)
            {
                foreach (var mesh in originalMeshClone)
                {
                    if (mesh.Geometry.VertexFormat != UiVertexFormat.Static)
                        mesh.Geometry.ChangeVertexType(UiVertexFormat.Weighted);

                    if (settings.OptimizeAlpha)
                        mesh.Material.GetCapability<MaterialBaseCapability>().UseAlpha = false;
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
                mesh.Name = mesh.Name.Replace(" - Clone", "");
                if (mesh.ReduceMeshOnLodGeneration && settings.LodRectionFactor != 1)
                    ReduceMesh(mesh, deductionRatio);
            }

            return originalMeshClone;
        }
    }
}
