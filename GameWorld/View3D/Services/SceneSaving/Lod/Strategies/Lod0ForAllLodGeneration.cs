using System.Windows;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{
    public class Lod0ForAllLodGeneration : OptimizedLodGeneratorBase, ILodGenerationStrategy
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
            var res = MessageBox.Show("Are you sure to copy lod 0 to every lod slots? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

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
            var originalMesh = rmv2MeshNode.Geometry;
            var reducedMesh = DecimatorMeshOptimizer.GetReducedMeshCopy(originalMesh, deductionRatio);
            rmv2MeshNode.Geometry = reducedMesh;
        }
    }
}
