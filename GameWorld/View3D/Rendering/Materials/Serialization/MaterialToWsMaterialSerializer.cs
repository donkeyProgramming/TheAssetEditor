﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToWsMaterialFactory
    {
        private readonly PackFileService _packFileServic;
        private readonly IPackFileSaveService _fileSaveService;

        public MaterialToWsMaterialFactory(PackFileService packFileServic, IPackFileSaveService fileSaveService)
        {
            _packFileServic = packFileServic;
            _fileSaveService = fileSaveService;
        }

        public IMaterialToWsMaterialSerializer CreateInstance(GameTypeEnum preferedGameHint)
        {
            var repository = new WsMaterialRepository(_packFileServic);
            var instance = new MaterialToWsMaterialSerializer(_fileSaveService, repository, preferedGameHint);
            return instance;
        }
    }

    public interface IMaterialToWsMaterialSerializer
    {
        string ProsessMaterial(string modelFilePath, string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material);
    }

    class MaterialToWsMaterialSerializer : IMaterialToWsMaterialSerializer
    {
        private readonly GameTypeEnum _preferedGameHint;
        private readonly IPackFileSaveService _fileSaveService;
        private readonly IWsMaterialRepository _repository;

        public MaterialToWsMaterialSerializer(IPackFileSaveService fileSaveService, IWsMaterialRepository wsMaterialRepository, GameTypeEnum preferedGameHint)
        {
            _repository = wsMaterialRepository;
            _fileSaveService = fileSaveService;
            _preferedGameHint = preferedGameHint;
        }

        public string ProsessMaterial(string modelFilePath, string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material)
        {
            var templateEditor = new WsMaterialTemplateEditor(material, _preferedGameHint);
            var fileName = templateEditor.AddTemplateHeader(meshName, meshVertexFormat, material);

            foreach (var cap in material.Capabilities)
                cap.SerializeToWsModel(templateEditor);

            var fileContent = templateEditor.GetCompletedMaterialString();

            // Check if file is uniqe - if not use original. We do this to avid an explotion of materials.
            // Kitbashed models sometimes have severl hundred meshes, we dont want that many materials if not needed
            var newMaterialPath = Path.GetDirectoryName(modelFilePath) + "/materials/" + fileName;
            var materialPath = _repository.GetExistingOrAddMaterial(fileContent, newMaterialPath, out var isNew);
            if (isNew)
                _fileSaveService.SaveFileWithoutPrompt(newMaterialPath, Encoding.UTF8.GetBytes(fileContent));

            return materialPath;
        }

    }
}


