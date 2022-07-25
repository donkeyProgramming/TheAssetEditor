using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.FileTypes.RigidModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Services
{
    public class LodGenerationService
    {
        private readonly ObjectEditor _objectEditor;

        public class Settings
        { 
            public float LodRectionFactor { get; set; }
            public bool OptimizeAlpha { get; set; }
            public bool OptimizeVertex { get; set; }
        }

        public LodGenerationService(ObjectEditor objectEditor)
        {
            _objectEditor = objectEditor;
        }

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

        public void CreateLodsForRootNode(Rmv2ModelNode rootNode)
        {
            var lods = rootNode.GetLodNodes();
            var firtLod = lods.First();
            var lodRootNodes = lods
                .Skip(1)
                .Take(rootNode.Children.Count - 1)
                .ToList();

            var lodGenerationSettings = lodRootNodes
                .Select(x => new Settings() { LodRectionFactor = x.LodReductionFactor, OptimizeAlpha = x.OptimizeLod_Alpha, OptimizeVertex = x.OptimizeLod_Vertex })
                .ToArray();

            // Delete all the lods
            DeleteAllLods(lodRootNodes);

            var meshList = firtLod.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
            var generatedLod = CreateLods(meshList, lodGenerationSettings);

            for (int i = 0; i < lodRootNodes.Count; i++)
            {
                foreach (var mesh in generatedLod[i])
                    lodRootNodes[i].AddObject(mesh);
            }
        }

        public List<Rmv2MeshNode[]> CreateLods(List<Rmv2MeshNode> originalModel, Settings[] settings)
        {
            var output = new List<Rmv2MeshNode[]>();
            for (int lodIndex = 0; lodIndex < settings.Length; lodIndex++)
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
                        if(mesh.Geometry.VertexFormat != UiVertexFormat.Static)
                            mesh.Geometry.ChangeVertexType(UiVertexFormat.Weighted, mesh.Geometry.ParentSkeletonName);

                        if(settings[lodIndex].OptimizeAlpha)
                            mesh.Material.AlphaMode = AlphaMode.Opaque;
                    }

                    // Combine if possible 
                    var errorList = new ErrorListViewModel.ErrorList();
                    var canCombine = ModelCombiner.HasPotentialCombineMeshes(originalMeshClone, out errorList);
                    if (canCombine)
                        originalMeshClone = ModelCombiner.CombineMeshes(originalMeshClone, addPrefix:false);
                }

                // Reduce the polygon count
                foreach (var mesh in originalMeshClone)
                {
                    if (mesh.ReduceMeshOnLodGeneration && settings[lodIndex].LodRectionFactor != 1)
                        _objectEditor.ReduceMesh(mesh, deductionRatio, false);
                }

                output.Add(originalMeshClone.ToArray());
            }

            return output;
        }


    }
}
