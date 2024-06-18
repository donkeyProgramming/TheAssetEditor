using System.Windows;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{
    public class Lod0ForAllLodGeneration : LodGeneratorBase, ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.Lod0ForAll;
        public string Name => "Lod0_ForAll";
        public string Description => "Copy lod 0 to all other LODs";
        public bool IsAvailable => true;

        public Lod0ForAllLodGeneration()
        {
        }

        public void Generate(MainEditableNode mainNode, LodGenerationSettings[] settings)
        {
            foreach (var setting in settings)
            {
                setting.LodRectionFactor = 1;
                setting.OptimizeAlpha = false;
                setting.OptimizeVertex = false;
            }

            CreateLodsForRootNode(mainNode, settings);
        }

        protected override void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio)
        {
            //var reducedMesh = DecimatorMeshOptimizer.GetReducedMeshCopy(rmv2MeshNode.Geometry, deductionRatio);
            //rmv2MeshNode.Geometry = reducedMesh;
        }
    }
}
