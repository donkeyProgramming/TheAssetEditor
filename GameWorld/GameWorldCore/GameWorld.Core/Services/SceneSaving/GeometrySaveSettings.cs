using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Microsoft.Xna.Framework;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Services.SceneSaving
{
    public class GeometrySaveSettings
    {
        private readonly ApplicationSettingsService _applicationSettingsService;

        public bool IsUserInitialized { get; set; } = false;    // Initialized after the user has opened a settings window once
        public string OutputName { get; set; } = "";// Init on load
        public GeometryStrategy GeometryOutputType { get; set; } = GeometryStrategy.Rmv7;
        public MaterialStrategy MaterialOutputType { get; set; } = MaterialStrategy.WsModel_Warhammer3;
        public LodStrategy LodGenerationMethod { get; set; } = LodStrategy.AssetEditor;
        public List<LodGenerationSettings> LodSettingsPerLod { get; set; } = [];
        public bool OnlySaveVisible { get; set; } = true;
        public int NumberOfLodsToGenerate { get; set; } = 4;
        public List<RmvAttachmentPoint> AttachmentPoints { get; set; } = [];

        public GeometrySaveSettings(ApplicationSettingsService applicationSettingsService)
        {
            _applicationSettingsService = applicationSettingsService;

            GameTypeEnum[] rmv7Games = [
                GameTypeEnum.Warhammer3, GameTypeEnum.ThreeKingdoms,    // Actually rmv8, but not supported by tool for now
                GameTypeEnum.Pharaoh, GameTypeEnum.Troy, GameTypeEnum.Warhammer2, GameTypeEnum.Warhammer];

            if (rmv7Games.Contains(_applicationSettingsService.CurrentSettings.CurrentGame) == false)
                GeometryOutputType = GeometryStrategy.Rmv6;

            MaterialOutputType = _applicationSettingsService.CurrentSettings.CurrentGame switch
            {
                GameTypeEnum.Warhammer3 => MaterialStrategy.WsModel_Warhammer3,
                GameTypeEnum.Warhammer2 => MaterialStrategy.WsModel_Warhammer2,
                GameTypeEnum.Pharaoh => MaterialStrategy.WsModel_Pharaoh,
                _ => MaterialStrategy.None,
            };
        }

        public void RefreshLodSettings()
        {
            LodSettingsPerLod.Clear();
            for (var i = 0; i < NumberOfLodsToGenerate; i++)
            {
                var settings = GenerateLodSettingsForIndex(i, NumberOfLodsToGenerate, null);
                LodSettingsPerLod.Add(settings);
            }
        }

        public void InitializeLodSettings(RmvLodHeader[] lodHeaders)
        {
            LodSettingsPerLod.Clear();
            NumberOfLodsToGenerate = lodHeaders.Length;

            for (var i = 0; i < NumberOfLodsToGenerate; i++)
            {
                var settings = GenerateLodSettingsForIndex(i, NumberOfLodsToGenerate, lodHeaders);
                LodSettingsPerLod.Add(settings);
            }
        }

        LodGenerationSettings GenerateLodSettingsForIndex(int lodIndex, int lodCount, RmvLodHeader[]? lodHeaders)
        {
            int[] possibleCameraDistances = [20, 80, 100, 10000, 10000, 10000];
            byte[] possibleQualityValues = [2, 0, 0, 0, 0, 0, 0];

            float cameraDistance = possibleCameraDistances[lodIndex];
            var qualityLvl = possibleQualityValues[lodIndex];

            if (lodHeaders != null)
            {
                var numLodsInGeometry = lodHeaders.Length;
                if (lodIndex < numLodsInGeometry)
                {
                    var lodHeader = lodHeaders[lodIndex];
                    cameraDistance = lodHeader.LodCameraDistance;
                    qualityLvl = lodHeader.QualityLvl;
                }
            }

            var setting = new LodGenerationSettings()
            {
                CameraDistance = cameraDistance,
                QualityLvl = qualityLvl,
                LodRectionFactor = GetDefaultLodReductionValue(lodCount, lodIndex),
                OptimizeAlpha = lodIndex >= 2,
                OptimizeVertex = lodIndex >= 2,
            };
            return setting;
        }

        static float GetDefaultLodReductionValue(int numLods, int currentLodIndex)
        {
            var lerpValue = (1.0f / (numLods - 1)) * (numLods - 1 - currentLodIndex);
            if (float.IsNaN(lerpValue))
                lerpValue = 1;
            var deductionRatio = MathHelper.Lerp(0.25f, 1, lerpValue);
            return deductionRatio;
        }
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
