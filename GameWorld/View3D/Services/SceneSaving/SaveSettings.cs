using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Material;
using System.Collections.Generic;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Services.SceneSaving
{
    public class SaveSettings
    {
        public bool IsUserInitialized { get; set; } = false;    // Initialized after the user has opened a settings window once
        public string OutputName { get; set; } = "";// Init on load
        public GeometryStrategy GeometryOutputType { get; set; } = GeometryStrategy.Rmv7;
        public MaterialStrategy MaterialOutputType { get; set; } = MaterialStrategy.WsModel_Warhammer3;
        public LodStrategy LodGenerationMethod { get; set; } = LodStrategy.AssetEditor;
        public List<LodGenerationSettings> LodSettingsPerLod { get; set; }  // Init on load
        public bool OnlySaveVisible { get; set; } = true;

        public void InitializeFromModel(Rmv2ModelNode modelNode)
        {
            int[] possibleCameraDistances = [20, 80, 100, 10000, 10000, 10000];
            byte[] possibleQualityValues = [2, 0, 0, 0, 0, 0, 0];

            GeometryOutputType = GeometryStrategy.Rmv7;  
        
            var numLodsInGeometry = modelNode.Model.LodHeaders.Length;
            var numLodNodes = modelNode.Children.Count;
            var lodValues = new List<LodGenerationSettings>();
        
            for (var i = 0; i < numLodNodes; i++)
            {
                float cameraDistance = possibleCameraDistances[i];
                byte qualityLvl = possibleQualityValues[i];

                if (i < numLodsInGeometry)
                {
                    var lodHeader = modelNode.Model.LodHeaders[i];
                    cameraDistance = lodHeader.LodCameraDistance;
                    qualityLvl = lodHeader.QualityLvl;
                }
                  
                var setting = new LodGenerationSettings()
                {
                    CameraDistance = cameraDistance,
                    QualityLvl = qualityLvl,
                    LodRectionFactor = GetDefaultLodReductionValue(numLodNodes, i),
                    OptimizeAlpha = i >= 2,
                    OptimizeVertex = i >= 2,
                };
                lodValues.Add(setting);
            }
        
            LodSettingsPerLod = lodValues;
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
