using System.IO;
using System.Text;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToWsMaterialFactory
    {
        private readonly IPackFileService _packFileServic;
        private readonly IFileSaveService _fileSaveService;

        public MaterialToWsMaterialFactory(IPackFileService packFileServic, IFileSaveService fileSaveService)
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
        // Call for every mesh before any ProsessMaterial. Returns the file the material was loaded from when it still holds
        // the material, and keeps the rest of the save from overwriting that file or a loaded one AE did not write.
        string? ReserveLoadedMaterial(string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material);
        string ProsessMaterial(string modelFilePath, string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material);
    }

    class MaterialToWsMaterialSerializer : IMaterialToWsMaterialSerializer
    {
        private readonly GameTypeEnum _preferedGameHint;
        private readonly IFileSaveService _fileSaveService;
        private readonly IWsMaterialRepository _repository;
        private readonly HashSet<string> _materialPathsUsedBySave = [];
        private readonly ILogger _logger = Logging.Create<MaterialToWsMaterialSerializer>();

        public MaterialToWsMaterialSerializer(IFileSaveService fileSaveService, IWsMaterialRepository wsMaterialRepository, GameTypeEnum preferedGameHint)
        {
            _repository = wsMaterialRepository;
            _fileSaveService = fileSaveService;
            _preferedGameHint = preferedGameHint;
        }

        public string? ReserveLoadedMaterial(string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material)
        {
            var loadedFrom = material.LoadedFrom;
            if (loadedFrom == null || _repository.CanReferenceMaterial(loadedFrom.Path) == false)
                return null;

            // Compare what would be written, not the file: it can hold shaders and params the template has no place for
            string loadedContent;
            try
            {
                loadedContent = CreateMaterialFile(meshName, loadedFrom.VertexFormat, loadedFrom.Material).Content;
            }
            catch (Exception e)
            {
                // No template for the material as loaded, so nothing to compare: generate it like any other
                _logger.Here().Warning(e, "Unable to compare the material loaded from {MaterialPath}", loadedFrom.Path);
                return null;
            }

            var isUnchanged = loadedFrom.VertexFormat == meshVertexFormat && CreateMaterialFile(meshName, meshVertexFormat, material).Content == loadedContent;
            if (isUnchanged)
            {
                _materialPathsUsedBySave.Add(PathNormalization.NormalizeFileName(loadedFrom.Path));
                return loadedFrom.Path;
            }

            // Edited, so it gets a generated file. Never one written over a file AE could not have written.
            if (_repository.HoldsMaterial(loadedFrom.Path, loadedContent) == false)
                _materialPathsUsedBySave.Add(PathNormalization.NormalizeFileName(loadedFrom.Path));
            return null;
        }

        public string ProsessMaterial(string modelFilePath, string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material)
        {
            var (fileName, fileContent) = CreateMaterialFile(meshName, meshVertexFormat, material);

            // Check if file is uniqe - if not use original. We do this to avid an explotion of materials.
            // Kitbashed models sometimes have severl hundred meshes, we dont want that many materials if not needed
            if (_repository.TryGetExistingMaterial(fileContent, out var existingPath))
            {
                _materialPathsUsedBySave.Add(PathNormalization.NormalizeFileName(existingPath));
                return existingPath;
            }

            // The mesh-based name may belong to a different material another model still uses
            var materialDirectory = Path.Combine(Path.GetDirectoryName(modelFilePath), "materials");
            var ownerWsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");
            var materialPath = Path.Combine(materialDirectory, fileName);
            for (var suffix = 1; IsMaterialPathFree(materialPath, ownerWsModelPath) == false; suffix++)
            {
                (fileName, fileContent) = CreateMaterialFile($"{meshName}_{suffix}", meshVertexFormat, material);
                materialPath = Path.Combine(materialDirectory, fileName);
            }

            _repository.AddMaterial(fileContent, materialPath);
            _materialPathsUsedBySave.Add(PathNormalization.NormalizeFileName(materialPath));
            _fileSaveService.Save(materialPath, Encoding.UTF8.GetBytes(fileContent), false);

            return materialPath;
        }

        bool IsMaterialPathFree(string materialPath, string ownerWsModelPath)
        {
            if (_materialPathsUsedBySave.Contains(PathNormalization.NormalizeFileName(materialPath)))
                return false;
            return _repository.CanWriteMaterial(materialPath, ownerWsModelPath);
        }

        (string FileName, string Content) CreateMaterialFile(string materialName, UiVertexFormat meshVertexFormat, CapabilityMaterial material)
        {
            var templateEditor = new WsMaterialTemplateEditor(material, _preferedGameHint);
            var fileName = templateEditor.AddTemplateHeader(materialName, meshVertexFormat, material);

            foreach (var cap in material.Capabilities)
                cap.SerializeToWsModel(templateEditor);

            return (fileName, templateEditor.GetCompletedMaterialString());
        }

    }
}


