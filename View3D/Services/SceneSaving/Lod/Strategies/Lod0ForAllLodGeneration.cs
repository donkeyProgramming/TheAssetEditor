using System.Windows;
using View3D.SceneNodes;

namespace View3D.Services.SceneSaving.Lod.Strategies
{
    public class Lod0ForAllLodGeneration : ILodGenerationStrategy
    {

        public LodStrategy StrategyId => LodStrategy.Lod0ForAll;
        public string Name => "Lod0";
        public string Description => "Copy lod 0 to all other lods";
        public bool IsAvailable => true;

        public Lod0ForAllLodGeneration()
        {

        }

        public void Generate(MainEditableNode mainNode, LodGenerationSettings[] settings)
        {
            //var res = MessageBox.Show("Are you sure to copy lod 0 to every lod slots? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (res != MessageBoxResult.Yes) return;
            //
            //mainNode.GetLodNodes().ForEach(x =>
            //{
            //    x.LodReductionFactor = 1;
            //    x.OptimizeLod_Alpha = false;
            //    x.OptimizeLod_Vertex = false;
            //});
            //
            //_lodGenerationService.CreateLodsForRootNode(mainNode);
        }

    }
}
