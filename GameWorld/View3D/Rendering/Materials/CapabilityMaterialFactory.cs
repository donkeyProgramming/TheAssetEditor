﻿using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.WpfWindow.ResourceHandling;
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

            var material = CreateMaterial(preferredMaterial);
            foreach (var capability in material.Capabilities)
                capability.Initialize(wsModelMaterial, rmvMaterial);

            return material;
        }

        void UpdatedPreferedMaterialBasedOnWsMaterial(GameTypeEnum currentGame, WsModelMaterialFile wsModelMaterial, ref CapabilityMaterialsEnum preferredMaterial)
        {
            if ((currentGame == GameTypeEnum.Warhammer3 || currentGame == GameTypeEnum.ThreeKingdoms) == false)
                return;
                
            if (wsModelMaterial.ShaderPath.Contains("emissive", StringComparison.InvariantCultureIgnoreCase))
                preferredMaterial = CapabilityMaterialsEnum.MetalRoughPbr_Emissive;
        }

        void UpdatedPreferedMaterialBasedOnRmv(GameTypeEnum currentGame, IRmvMaterial material, ref CapabilityMaterialsEnum preferredMaterial)
        {
            // Decal disabled for now, as saving is not validated! 
            // if (material is WeightedMaterial weighterMaterial)
            // { 
            //     if (weighterMaterial.UseDecal || weighterMaterial.UseDirt)
            //         preferredMaterial = CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal;
            // }
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
            return [CapabilityMaterialsEnum.MetalRoughPbr_Default,
                    CapabilityMaterialsEnum.MetalRoughPbr_Emissive,

                    CapabilityMaterialsEnum.SpecGlossPbr_Default,
                    CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal];
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


