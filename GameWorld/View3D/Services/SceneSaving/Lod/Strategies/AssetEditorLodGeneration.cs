using System.Collections.Generic;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving.Lod.MeshDecimatorIntegration;

namespace GameWorld.Core.Services.SceneSaving.Lod.Strategies
{
    public class AssetEditorLodGeneration : LodGeneratorBase, ILodGenerationStrategy
    {
        public LodStrategy StrategyId => LodStrategy.AssetEditor;
        public string Name => "Default";
        public string Description => "Use AssetEditor Algorithm";
        public bool IsAvailable => true;

        public void Generate(MainEditableNode mainNode, List<LodGenerationSettings> settings)
        {
            CreateLodsForRootNode(mainNode, settings);
        }

        protected override void ReduceMesh(Rmv2MeshNode rmv2MeshNode, float deductionRatio)
        {
            var reducedMesh = DecimatorMeshOptimizer.GetReducedMeshCopy(rmv2MeshNode.Geometry, deductionRatio);
            rmv2MeshNode.Geometry = reducedMesh;
        }
    }
}
