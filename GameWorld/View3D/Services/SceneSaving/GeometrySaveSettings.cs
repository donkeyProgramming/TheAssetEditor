using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Material;
using System.Collections.Generic;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Services.SceneSaving
{
    public class GeometrySaveSettings
    {
        public bool IsUserInitialized { get; set; } = false;    // Initialized after the user has opened a settings window once
        public string OutputName { get; set; } = "";// Init on load
        public GeometryStrategy GeometryOutputType { get; set; } = GeometryStrategy.Rmv7;
        public MaterialStrategy MaterialOutputType { get; set; } = MaterialStrategy.WsModel_Warhammer3;
        public LodStrategy LodGenerationMethod { get; set; } = LodStrategy.AssetEditor;
        public List<LodGenerationSettings> LodSettingsPerLod { get; set; } = [];
        public bool OnlySaveVisible { get; set; } = true;
        public int NumberOfLodsToGenerate { get; set; } = 4;


        public void RefreshLodSettings()
        {
            LodSettingsPerLod.Clear();
            for (var i = 0; i < NumberOfLodsToGenerate; i++)
            {
                var settings = GenerateLodSettingsForIndex(i, NumberOfLodsToGenerate, null);
                LodSettingsPerLod.Add(settings);
            }
        }

        public void InitializeFromModel(Rmv2ModelNode modelNode)
        {
            LodSettingsPerLod.Clear();

            NumberOfLodsToGenerate = modelNode.Model.LodHeaders.Length;

            for (var i = 0; i < NumberOfLodsToGenerate; i++)
            {
                var settings = GenerateLodSettingsForIndex(i, NumberOfLodsToGenerate, modelNode);
                LodSettingsPerLod.Add(settings);
            }
        }

        LodGenerationSettings GenerateLodSettingsForIndex(int lodIndex, int lodCount, Rmv2ModelNode? node)
        {
            int[] possibleCameraDistances = [20, 80, 100, 10000, 10000, 10000];
            byte[] possibleQualityValues = [2, 0, 0, 0, 0, 0, 0];

            float cameraDistance = possibleCameraDistances[lodIndex];
            var qualityLvl = possibleQualityValues[lodIndex];

            if (node != null)
            {
                var numLodsInGeometry = node.Model.LodHeaders.Length;
                if (lodIndex < numLodsInGeometry)
                { 
                    var lodHeader = node.Model.LodHeaders[lodIndex];
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
