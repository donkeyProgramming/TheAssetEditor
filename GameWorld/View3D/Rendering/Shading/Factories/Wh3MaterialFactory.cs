using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public class Wh3MaterialFactory : IMaterialFactory
    {
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        public Wh3MaterialFactory(PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        public CapabilityMaterial Create(RmvModel model, string? wsModelMaterialPath)
        {
            var preferredMaterial = CapabilityMaterialsEnum.Normal;
            WsModelMaterialFile? wsModelMaterial = null;
            if (wsModelMaterialPath != null)
            {
                var materialPackFile = _packFileService.FindFile(wsModelMaterialPath);
                wsModelMaterial = new WsModelMaterialFile(materialPackFile);

                if(wsModelMaterial.ShaderPath.Contains("emissive", StringComparison.InvariantCultureIgnoreCase))
                    preferredMaterial = CapabilityMaterialsEnum.Emissive;
            }

            var material = CreateMaterial(preferredMaterial);
            foreach (var capability in material.Capabilities)
                capability.Initialize(wsModelMaterial, model);

            return material;
        }

        CapabilityMaterial CreateMaterial(CapabilityMaterialsEnum type)
        {
            return type switch
            {
                CapabilityMaterialsEnum.Normal => new DefaultMaterialWh3(_resourceLibrary),
                CapabilityMaterialsEnum.Emissive => new EmissiveMaterial(_resourceLibrary),
                _ => throw new Exception($"Material of type {type} is not supported by {nameof(Wh3MaterialFactory)}"),
            };
        }

        public List<CapabilityMaterialsEnum> GetPossibleMaterials() => [CapabilityMaterialsEnum.Normal, CapabilityMaterialsEnum.Emissive];
   
        public CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterial)
        {
            throw new NotImplementedException();
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new NotImplementedException();
        }


    }
}


