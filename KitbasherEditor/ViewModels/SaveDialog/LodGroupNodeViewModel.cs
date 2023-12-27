using System.Linq;
using CommonControls.Common;
using View3D.SceneNodes;
using View3D.Services.SceneSaving;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    public class LodGroupNodeViewModel : NotifyPropertyChangedImpl
    {
        private readonly Rmv2LodNode _node;
        private readonly SaveSettings _saveSettings;
        private readonly int _lodIndex;

        public LodGroupNodeViewModel(Rmv2LodNode node,  SaveSettings saveSettings)
        {
            _node = node;
            _saveSettings = saveSettings;
            _lodIndex = _node.LodValue;

            Compute();
        }

        void Compute()
        {
            PolygonCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).Sum(x => x.Geometry.VertexCount() / 3);
            TextureCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).SelectMany(x => x.Material.GetAllTextures().Select(x => x.Path)).Distinct().Count();
            MeshCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).Count();
        }

        public float CameraDistance
        {
            get => _saveSettings.LodSettingsPerLod[_lodIndex].CameraDistance;
            set
            {
                _saveSettings.LodSettingsPerLod[_lodIndex].CameraDistance = value;
                NotifyPropertyChanged();
            }
        }

        public byte QualityLvl
        {
            get => _saveSettings.LodSettingsPerLod[_lodIndex].QualityLvl;
            set
            {
                _saveSettings.LodSettingsPerLod[_lodIndex].QualityLvl = value;
                NotifyPropertyChanged();
            }
        }

        public float LodReductionFactor
        {
            get => _saveSettings.LodSettingsPerLod[_lodIndex].LodRectionFactor;
            set
            {
                _saveSettings.LodSettingsPerLod[_lodIndex].LodRectionFactor = value;
                Compute();
                NotifyPropertyChanged();
            }
        }

        public int LodIndex { get => _lodIndex; }
        public bool OptimizeLod_Alpha
        {
            get => _saveSettings.LodSettingsPerLod[_lodIndex].OptimizeAlpha;
            set
            {
                _saveSettings.LodSettingsPerLod[_lodIndex].OptimizeAlpha = value;
                Compute();
                NotifyPropertyChanged();
            }
        }

        public bool OptimizeLod_Vertex 
        {
            get => _saveSettings.LodSettingsPerLod[_lodIndex].OptimizeVertex;
            set
            {
                _saveSettings.LodSettingsPerLod[_lodIndex].OptimizeVertex = value;
                Compute();
                NotifyPropertyChanged();
            }
        }

        public NotifyAttr<int> PolygonCount { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> TextureCount { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> MeshCount { get; set; } = new NotifyAttr<int>(0);
    }
}
