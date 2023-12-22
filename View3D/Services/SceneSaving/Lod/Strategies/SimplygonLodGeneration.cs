using View3D.SceneNodes;
using View3D.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace View3D.Services.SceneSaving.Lod.Strategies
{
    public class SimplygonLodGeneration : OptimizedLodGeneratorBase, ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.Simplygon;
        public string Name => "Simplygon";
        public string Description => "Use simplygon - requires external install";
        public bool IsAvailable => true;

        public SimplygonLodGeneration()
        {
     
        }

        public void Generate(MainEditableNode mainNode, LodGenerationSettings[] settings)
        {

        }

        protected override void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio)
        {
            var originalMesh = rmv2MeshNode.Geometry;
            //var reducedMesh = MeshOptimizerService.CreatedReducedCopy(originalMesh, deductionRatio);// Use simplygon!
            var reducedMesh = DecimatorMeshOptimizer.GetReducedMeshCopy(originalMesh, deductionRatio);
            rmv2MeshNode.Geometry = reducedMesh;
        }
    }
}
