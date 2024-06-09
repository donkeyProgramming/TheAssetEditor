using System;
using View3D.SceneNodes;
using View3D.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace View3D.Services.SceneSaving.Lod.Strategies
{
    public class DefaultLodGeneration : OptimizedLodGeneratorBase, ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.Default;
        public string Name => "Default";
        public string Description => "Use AssetEditor Algorithm";
        public bool IsAvailable => true;

        public void Generate(MainEditableNode mainNode, LodGenerationSettings[] settings)
        {
            CreateLodsForRootNode(mainNode, settings);
        }

        protected override void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio)
        {
            var originalMesh = rmv2MeshNode.Geometry;
            //var reducedMesh = MeshOptimizerService.CreatedReducedCopy(originalMesh, deductionRatio);
            var reducedMesh = DecimatorMeshOptimizer.GetReducedMeshCopy(originalMesh, deductionRatio);
            rmv2MeshNode.Geometry = reducedMesh;
        }
    }
}
