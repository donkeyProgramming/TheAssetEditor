using View3D.Services.SceneSaving.Lod;
using View3D.Services.SceneSaving.Geometry;
using View3D.Services.SceneSaving.WsModel;

namespace View3D.Services.SceneSaving
{
    public class SaveSettings
    {
        public bool IsInitialized { get; set; } = false;
        public string OutputName { get; set; } = "";// Init on load
        public GeometryStrategy GeometryOutputType { get; set; } = GeometryStrategy.Rmv7;
        public MaterialStrategy MaterialOutputType { get; set; } = MaterialStrategy.WsModel_Warhammer3;
        public LodStrategy LodGenerationMethod { get; set; } = LodStrategy.Default;
        public LodGenerationSettings[] LodSettingsPerLod { get; set; }  // Init on load
        public bool OnlySaveVisible { get; set; } = true;
    }

    public class LodGenerationSettings
    {
        public float LodRectionFactor { get; set; }
        public bool OptimizeAlpha { get; set; }
        public bool OptimizeVertex { get; set; }
        public byte QualityLvl { get; set; }
        public float CameraDistance { get; set; }
    }
}
