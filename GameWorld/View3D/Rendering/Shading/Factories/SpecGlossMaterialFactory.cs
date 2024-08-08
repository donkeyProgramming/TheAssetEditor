using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;
using SharpDX.DirectWrite;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public class SpecGlossMaterialFactory : IMaterialFactory
    {
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        public SpecGlossMaterialFactory(PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        public CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterial)
        {
            throw new System.NotImplementedException();
        }

        public CapabilityMaterial Create(RmvModel model, string wsModelMaterialPath)
        {
            WsModelMaterialFile? wsModelMaterial = null;
            if (wsModelMaterialPath != null)
            {
                var materialPackFile = _packFileService.FindFile(wsModelMaterialPath);
                if (materialPackFile != null)
                {
                    wsModelMaterial = new WsModelMaterialFile(materialPackFile);
                }
            }


            var material = CreateMaterial(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            foreach (var capability in material.Capabilities)
                capability.Initialize(wsModelMaterial, model);
            return material;
        }

        public CapabilityMaterial CreateMaterial(CapabilityMaterialsEnum type)
        {
            return type switch
            {
                CapabilityMaterialsEnum.SpecGlossPbr_Default => new DefaultSpecGlossPbrMaterial(_resourceLibrary),
                _ => throw new Exception($"Material of type {type} is not supported by {nameof(SpecGlossMaterialFactory)}"),
            };
        }

        public List<CapabilityMaterialsEnum> GetPossibleMaterials() => [CapabilityMaterialsEnum.SpecGlossPbr_Default];
    }
}


