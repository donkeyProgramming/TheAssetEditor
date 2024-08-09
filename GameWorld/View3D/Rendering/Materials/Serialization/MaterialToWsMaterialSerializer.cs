using System.IO;
using System.Text;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToWsMaterialFactory
    {
        private readonly PackFileService _packFileServic;

        public MaterialToWsMaterialFactory(PackFileService packFileServic)
        {
            _packFileServic = packFileServic;
        }

        public IMaterialToWsMaterialSerializer CreateInstance(GameTypeEnum preferedGameHint) => new MaterialToWsMaterialSerializer(_packFileServic, preferedGameHint);
    }

    public interface IMaterialToWsMaterialSerializer
    {
        string ProsessMaterial(string modelFilePath, string meshName, UiVertexFormat meshVertexFormat, CapabilityMaterial material);
    }

    class MaterialToWsMaterialSerializer : IMaterialToWsMaterialSerializer
    {
        private readonly GameTypeEnum _preferedGameHint;
        private readonly PackFileService _packFileService;
        private readonly WsMaterialRepository _repository;

        public MaterialToWsMaterialSerializer(PackFileService packFileService, GameTypeEnum preferedGameHint)
        {
            _repository = new WsMaterialRepository(packFileService);
            _packFileService = packFileService;
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
            {
                var existingMaterialPackFile = _packFileService.FindFile(newMaterialPath, _packFileService.GetEditablePack());
                SaveHelper.Save(_packFileService, newMaterialPath, existingMaterialPackFile, Encoding.UTF8.GetBytes(fileContent));
            }

            return materialPath;
        }

    }
}
