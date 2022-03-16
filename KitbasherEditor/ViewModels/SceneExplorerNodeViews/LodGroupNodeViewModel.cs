using CommonControls.Common;
using CommonControls.MathViews;
using MonoGame.Framework.WpfInterop;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class LodGroupNodeViewModel : GroupNodeViewModel
    {
        private new readonly Rmv2LodNode _node;
        private readonly IComponentManager _componentManager;

        public LodGroupNodeViewModel(Rmv2LodNode node, IComponentManager componentManager) : base(node)
        {
            _node = node;
            _componentManager = componentManager;
            LodReductionFactor = new DoubleViewModel(_node.LodReductionFactor);
            LodReductionFactor.OnValueChanged += (value) => _node.LodReductionFactor = (float)value;
        }



        public float? CameraDistance
        {
            get => _node.CameraDistance;
            set {
                _node.CameraDistance = value;
                NotifyPropertyChanged();

                if (value.HasValue)
                {
                    var lodHeaders = _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Model.LodHeaders;
                    lodHeaders[_node.LodValue].LodCameraDistance = value.Value;
                }
            }
        }

        public byte QualityLvl
        {
            get
            {
                var lodHeaders = _componentManager.GetComponent<IEditableMeshResolver>().GeEditableMeshRootNode().Model.LodHeaders;
                return lodHeaders[_node.LodValue].QualityLvl;
            }
           
        }

        public DoubleViewModel LodReductionFactor { get; set; } = new DoubleViewModel(1);

    }
}
