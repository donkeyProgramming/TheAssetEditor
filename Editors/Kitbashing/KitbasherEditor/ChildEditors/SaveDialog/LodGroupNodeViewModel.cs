using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using Shared.Core.Misc;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    public class LodGroupNodeViewModel : NotifyPropertyChangedImpl
    {
        private readonly Rmv2LodNode? _node;
        private readonly GeometrySaveSettings _saveSettings;

        public NotifyAttr<int> PolygonCount { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> TextureCount { get; set; } = new NotifyAttr<int>(0);
        public NotifyAttr<int> MeshCount { get; set; } = new NotifyAttr<int>(0);
        public int LodIndex { get; private set; }

        public LodGroupNodeViewModel(Rmv2LodNode? node, int lodIndex, GeometrySaveSettings saveSettings)
        {
            _node = node;
            _saveSettings = saveSettings;
            LodIndex = lodIndex;

            if (_node != null)
            {
                PolygonCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).Sum(x => x.Geometry.VertexCount() / 3);
                TextureCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).SelectMany(x => x.RmvMaterial.GetAllTextures().Select(x => x.Path)).Distinct().Count();
                MeshCount.Value = _node.GetAllModels(_saveSettings.OnlySaveVisible).Count();
            }
        }

        public float CameraDistance
        {
            get => _saveSettings.LodSettingsPerLod[LodIndex].CameraDistance;
            set
            {
                _saveSettings.LodSettingsPerLod[LodIndex].CameraDistance = value;
                NotifyPropertyChanged();
            }
        }

        public byte QualityLvl
        {
            get => _saveSettings.LodSettingsPerLod[LodIndex].QualityLvl;
            set
            {
                _saveSettings.LodSettingsPerLod[LodIndex].QualityLvl = value;
                NotifyPropertyChanged();
            }
        }

        public float LodReductionFactor
        {
            get => _saveSettings.LodSettingsPerLod[LodIndex].LodRectionFactor;
            set
            {
                _saveSettings.LodSettingsPerLod[LodIndex].LodRectionFactor = value;
                NotifyPropertyChanged();
            }
        }

    
        public bool OptimizeLod_Alpha
        {
            get => _saveSettings.LodSettingsPerLod[LodIndex].OptimizeAlpha;
            set
            {
                _saveSettings.LodSettingsPerLod[LodIndex].OptimizeAlpha = value;
                NotifyPropertyChanged();
            }
        }

        public bool OptimizeLod_Vertex 
        {
            get => _saveSettings.LodSettingsPerLod[LodIndex].OptimizeVertex;
            set
            {
                _saveSettings.LodSettingsPerLod[LodIndex].OptimizeVertex = value;
                NotifyPropertyChanged();
            }
        }
    }
}
