using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Services;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials
{
    public class CapabilityMaterialFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly ResourceLibrary _resourceLibrary;

        public CapabilityMaterialFactory(ApplicationSettingsService applicationSettingsService, ResourceLibrary resourceLibrary)
        {
            _applicationSettingsService = applicationSettingsService;
            _resourceLibrary = resourceLibrary;
        }

        public CapabilityMaterial Create(IRmvMaterial rmvMaterial, WsModelMaterialFile? wsModelMaterial = null)
        {
            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;

            var preferredMaterial = GetDefaultMaterial(currentGame);
            if (wsModelMaterial != null)
                UpdatedPreferedMaterialBasedOnWsMaterial(currentGame, wsModelMaterial, ref preferredMaterial);
            else
                UpdatedPreferedMaterialBasedOnRmv(currentGame, rmvMaterial, ref preferredMaterial);

            try
            {
                var material = CreateMaterial(preferredMaterial);
                foreach (var capability in material.Capabilities)
                    capability.Initialize(wsModelMaterial, rmvMaterial);
                return material;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize material '{preferredMaterial}' for game {currentGame}. RmrMaterial={rmvMaterial.MaterialId} wsModelMaterial={wsModelMaterial?.Name} ", ex);
            }
        }

        void UpdatedPreferedMaterialBasedOnWsMaterial(GameTypeEnum currentGame, WsModelMaterialFile wsModelMaterial, ref CapabilityMaterialsEnum preferredMaterial)
        {
            if ((currentGame == GameTypeEnum.Warhammer3 || currentGame == GameTypeEnum.ThreeKingdoms) == false)
                return;

            var isEmissive = wsModelMaterial.ShaderPath.Contains("emissive", StringComparison.InvariantCultureIgnoreCase);
            var isPropEmissive = wsModelMaterial.ShaderPath.Contains("prop_emissive", StringComparison.InvariantCultureIgnoreCase);
            if (isEmissive && !isPropEmissive)
                preferredMaterial = CapabilityMaterialsEnum.MetalRoughPbr_Emissive;
        }

        void UpdatedPreferedMaterialBasedOnRmv(GameTypeEnum currentGame, IRmvMaterial material, ref CapabilityMaterialsEnum preferredMaterial)
        {
            if (material is WeightedMaterial weighterMaterial)
            {
                var isDecal = RmvMaterialUtil.IsDecal(material);
                var isDirt = RmvMaterialUtil.IsDirt(material);

                if (isDecal || isDirt)
                    preferredMaterial = CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal;
            }
        }

        CapabilityMaterialsEnum GetDefaultMaterial(GameTypeEnum currentGame)
        {
            if (currentGame == GameTypeEnum.Warhammer3 || currentGame == GameTypeEnum.ThreeKingdoms)
                return CapabilityMaterialsEnum.MetalRoughPbr_Default;
            return CapabilityMaterialsEnum.SpecGlossPbr_Default;
        }

        public CapabilityMaterial CreateMaterial(CapabilityMaterialsEnum type)
        {
            return type switch
            {
                CapabilityMaterialsEnum.MetalRoughPbr_Default => new Shaders.MetalRough.DefaultMaterial(_resourceLibrary),
                CapabilityMaterialsEnum.MetalRoughPbr_Emissive => new Shaders.MetalRough.EmissiveMaterial(_resourceLibrary),
                CapabilityMaterialsEnum.SpecGlossPbr_Default => new Shaders.SpecGloss.DefaultMaterial(_resourceLibrary),
                CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal => new Shaders.SpecGloss.DecalAndDirtMaterial(_resourceLibrary),

                _ => throw new Exception($"Material of type {type} is not supported by {nameof(CapabilityMaterialFactory)}"),
            };
        }

        public List<CapabilityMaterialsEnum> GetPossibleMaterials()
        {
            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
            switch (currentGame)
            {
                case GameTypeEnum.Unknown:
                case GameTypeEnum.Arena:
                case GameTypeEnum.Attila:
                case GameTypeEnum.Empire:
                case GameTypeEnum.Napoleon:
                case GameTypeEnum.RomeRemastered:
                case GameTypeEnum.Rome_2:
                case GameTypeEnum.Shogun_2:
                case GameTypeEnum.Troy:
                case GameTypeEnum.Pharaoh:
                case GameTypeEnum.ThronesOfBritannia:
                case GameTypeEnum.Warhammer:
                case GameTypeEnum.Warhammer2:
                    return [CapabilityMaterialsEnum.SpecGlossPbr_Default, CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal];

                case GameTypeEnum.Warhammer3:
                case GameTypeEnum.ThreeKingdoms:
                    return [CapabilityMaterialsEnum.MetalRoughPbr_Default, CapabilityMaterialsEnum.MetalRoughPbr_Emissive];
                default:
                    throw new Exception($"Unkown game {currentGame} in {nameof(GetPossibleMaterials)}");
            }

        }

        public CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterialType)
        {
            var newMaterial = CreateMaterial(newMaterialType);

            for (var sourceCapIndex = 0; sourceCapIndex < source.Capabilities.Length; sourceCapIndex++)
            {
                for (var newCapIndex = 0; newCapIndex < newMaterial.Capabilities.Length; newCapIndex++)
                {
                    if (source.Capabilities[sourceCapIndex].GetType() == newMaterial.Capabilities[newCapIndex].GetType())
                    {
                        newMaterial.Capabilities[newCapIndex] = source.Capabilities[sourceCapIndex].Clone();
                    }
                }
            }

            return newMaterial;
        }
    }
}

