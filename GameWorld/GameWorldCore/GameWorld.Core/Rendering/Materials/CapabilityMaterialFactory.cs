using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials
{

    public enum MaterialVersion
    {
        SpecGloss,

        MetalRough,
    }

    //  RegisterShader(PreferedShaderGroup.Wh3)
    //      .Shader("path", shaderEnum)
    //      .AddCapability<TCap, TWsModelSerializer, TRmvSerializer>();

    public class CapabilityMaterialDatabase
    {
        public CapabilityMaterialDatabase()
        {

        }



        public void Create()
        {
            //RegisterShader(MaterialVersion.MetalRough)
            //  .Shader("Path", Pbr_MetalRough)
            //  .AddCapability<MetalRoughCapability, MetalRoughCapabilityWsModelSerializer, MetalRoughCapabilityMaterialSerizer>();
            //  .AddCapability<AnimationCapability...>();
            //  .AddCapability<BloodCapability...>();
            //  .AddCapability<EmissiveCapability...>();
            //  .AddCapability<TintCapability...>();
            //  .AddToDb(this);
        }

    }

    public interface ICapabilityMaterialFactory
    {
        public CapabilityMaterial Create(IRmvMaterial rmvMaterial, WsModelMaterialFile? wsModelMaterial = null);
        // GetPossiblematerials
        //IsmaterialSupportedFromWsModelName
        public CapabilityMaterial GetDefault();
    }


    public class CapabilityMaterialFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IScopedResourceLibrary _resourceLibrary;

        public CapabilityMaterialFactory(ApplicationSettingsService applicationSettingsService, IScopedResourceLibrary resourceLibrary)
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
                material.Initialize(wsModelMaterial, rmvMaterial);
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
                var isSkin = RmvMaterialUtil.IsSkin(material);

                if (isDecal || isDirt || isSkin)
                    preferredMaterial = CapabilityMaterialsEnum.SpecGlossPbr_Advanced;
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
                CapabilityMaterialsEnum.SpecGlossPbr_Advanced => new Shaders.SpecGloss.AdvancedRmvMaterial(_resourceLibrary),

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
                case GameTypeEnum.Rome2:
                case GameTypeEnum.Shogun2:
                case GameTypeEnum.Troy:
                case GameTypeEnum.Pharaoh:
                case GameTypeEnum.ThronesOfBritannia:
                case GameTypeEnum.Warhammer:
                case GameTypeEnum.Warhammer2:
                    return [CapabilityMaterialsEnum.SpecGlossPbr_Default, CapabilityMaterialsEnum.SpecGlossPbr_Advanced];

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

