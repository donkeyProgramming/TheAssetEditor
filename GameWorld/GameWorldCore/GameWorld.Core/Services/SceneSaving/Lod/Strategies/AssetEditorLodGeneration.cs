using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{
    public class AssetEditorLodGeneration : LodGeneratorBase, ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.AssetEditor;
        public string Name => "Default";
        public string Description => "Use AssetEditor Algorithm";
        public bool IsAvailable => true;

        public AssetEditorLodGeneration(IScopedLogger scopedLogger) : base(scopedLogger)
        {
        }

        public void Generate(MainEditableNode mainNode, List<LodGenerationSettings> settings)
        {
            CreateLodsForRootNode(mainNode, settings);
        }

        protected override void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio) => ReduceMeshes([rmv2MeshNode], deductionRatio);

        // The triangle budget is for the whole model, not each mesh
        protected override void ReduceMeshes(List<Rmv2MeshNode> meshes, float deductionRatio)
        {
            if (meshes.Count == 0)
                return;

            var reducedMeshes = DecimatorMeshOptimizer.GetReducedMeshCopies(meshes.Select(x => x.Geometry).ToList(), deductionRatio);
            for (var i = 0; i < meshes.Count; i++)
            {
                meshes[i].Geometry.RemoveGraphicsCardResources();
                meshes[i].Geometry = reducedMeshes[i];
            }
        }
    }
}
