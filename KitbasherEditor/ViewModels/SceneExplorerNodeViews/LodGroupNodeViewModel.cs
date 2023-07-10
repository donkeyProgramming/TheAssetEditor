using MonoGame.Framework.WpfInterop;
using System.Linq;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class LodGroupNodeViewModel : GroupNodeViewModel
    {
        private readonly Rmv2LodNode _node;
        private readonly IComponentManager _componentManager;

        public LodGroupNodeViewModel(Rmv2LodNode node, IComponentManager componentManager) : base(node)
        {
            _node = node;
            _componentManager = componentManager;
        }

        public float? CameraDistance
        {
            get => _node.CameraDistance;
            set
            {
                _node.CameraDistance = value;
                NotifyPropertyChanged();

                if (value.HasValue)
                {
                    var sceneManager = _componentManager.GetComponent<SceneManager>();
                    var root = sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                    var lodHeaders = root.Model.LodHeaders;
                    lodHeaders[_node.LodValue].LodCameraDistance = value.Value;
                }
            }
        }

        public byte QualityLvl
        {
            get
            {
                var sceneManager = _componentManager.GetComponent<SceneManager>();
                var root = sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                var lodHeaders = root.Model.LodHeaders;
                return lodHeaders[_node.LodValue].QualityLvl;
            }
            set
            {
                var sceneManager = _componentManager.GetComponent<SceneManager>();
                var root = sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
                var lodHeaders = root.Model.LodHeaders; ;
                lodHeaders[_node.LodValue].QualityLvl = value;
                NotifyPropertyChanged();
            }
        }

        public float LodReductionFactor
        {
            get => _node.LodReductionFactor;
            set
            {
                _node.LodReductionFactor = value;
                NotifyPropertyChanged();
            }
        }

        public int LodIndex { get => _node.LodValue; }
        public bool OptimizeLod_Alpha { get => _node.OptimizeLod_Alpha; set => _node.OptimizeLod_Alpha = value; }
        public bool OptimizeLod_Vertex { get => _node.OptimizeLod_Vertex; set => _node.OptimizeLod_Vertex = value; }
        public int PolygonCount { get => _node.GetAllModels(false).Sum(x => x.Geometry.VertexCount() / 3); }
        public int TextureCount { get => _node.GetAllModels(false).SelectMany(x => x.Material.GetAllTextures().Select(x => x.Path)).Distinct().Count(); }
        public int MeshCount { get => _node.GetAllModels(false).Count(); }
    }
}
