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
            var preferredMaterial = CapabilityMaterialsEnum.SpecGlossPbr_Default;

            if (currentGame == GameTypeEnum.Warhammer3 || currentGame == GameTypeEnum.ThreeKingdoms)
            {
                preferredMaterial = CapabilityMaterialsEnum.MetalRoughPbr_Default;
                if (wsModelMaterial != null)
                {
                    if (wsModelMaterial.ShaderPath.Contains("emissive", StringComparison.InvariantCultureIgnoreCase))
                        preferredMaterial = CapabilityMaterialsEnum.MetalRoughPbr_Emissive;
                }
            }
            
            var material = CreateMaterial(preferredMaterial);
            foreach (var capability in material.Capabilities)
                capability.Initialize(wsModelMaterial, rmvMaterial);

            return material;
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

        public CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterial)
        {
            return CreateMaterial(newMaterial);
        }
    }
}


