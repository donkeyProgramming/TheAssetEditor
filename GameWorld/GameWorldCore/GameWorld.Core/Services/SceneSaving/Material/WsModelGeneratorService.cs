using System.IO;
using System.Text;
using System.Windows;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material
{
    public record WsMaterialResult(bool Result, string? Content, string? GeneratedFilePath);

    public class WsModelGeneratorService
    {
        private readonly ILogger _logger;
        private readonly IPackFileService _packFileService;
        private readonly IFileSaveService _packFileSaveService;

        public WsModelGeneratorService(IPackFileService packFileService, IFileSaveService packFileSaveService, IScopedLogger scopedLogger)
        {
            _logger = scopedLogger.ForContext<WsModelGeneratorService>();
            _packFileService = packFileService;
            _packFileSaveService = packFileSaveService;
        }

        // regenerateMaterials: write every material from the template, also those a save would otherwise keep as loaded
        public WsMaterialResult GenerateWsModel(IMaterialToWsMaterialSerializer wsMaterialGenerator, string modelFilePath, List<WsModelGeneratorInput> meshInformation, bool regenerateMaterials = false)
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return new WsMaterialResult(false, null, null); 
                }
               
                var wsModelData = CreateWsModel(wsMaterialGenerator, modelFilePath, meshInformation, regenerateMaterials);

                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");
                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());
                _packFileSaveService.Save(wsModelPath, Encoding.UTF8.GetBytes(wsModelData), false);
              
                return new WsMaterialResult(false, wsModelPath, wsModelData);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.Message);
                MessageBox.Show("Generation failed!");
                return new WsMaterialResult(false, null, null);
            }
        }

        static string CreateWsModel(IMaterialToWsMaterialSerializer wsMaterialGenerator, string modelFilePath, List<WsModelGeneratorInput> meshInformation, bool regenerateMaterials)
        {
            var meshesWithUniqeNames = EnsureUniqueMeshNames(meshInformation);
            var orderedMeshInfo = meshesWithUniqeNames
                .OrderBy(x => x.LodIndex)
                .ToList();

            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            // All meshes first, so no mesh's generated material can take a file a later mesh keeps
            var keptMaterialPaths = orderedMeshInfo
                .Select(x => regenerateMaterials ? null : wsMaterialGenerator.ReserveLoadedMaterial(x.MeshName, x.MeshVertexFormat, x.Material))
                .ToList();

            for (var i = 0; i < orderedMeshInfo.Count; i++)
            {
                var meshInfo = orderedMeshInfo[i];
                var materialPath = keptMaterialPaths[i] ?? wsMaterialGenerator.ProsessMaterial(modelFilePath, meshInfo.MeshName, meshInfo.MeshVertexFormat, meshInfo.Material);

                // Regenerated on purpose, so later saves compare against what this one wrote, not the file the model was
                // opened with: that one would still match and be kept again. Other saves keep the loaded file reserved.
                if (regenerateMaterials)
                    meshInfo.Material.RememberLoadedFrom(materialPath, meshInfo.MeshVertexFormat);

                sb.Append($"\t\t\t<material lod_index=\"{meshInfo.LodIndex}\" part_index=\"{meshInfo.MeshIndex}\">");
                sb.Append(materialPath);
                sb.Append("</material>\n");
            }

            sb.Append("\t</materials>\n");
            sb.Append("</model>\n");

            return sb.ToString();
        }

        static private List<WsModelGeneratorInput> EnsureUniqueMeshNames(IEnumerable<WsModelGeneratorInput> meshes)
        {
            var meshesWithUniqeNames = new List<WsModelGeneratorInput>();

            var tempNameList = new List<string>();
            foreach (var mesh in meshes)
            {
                var meshName = mesh.MeshName;
                for (var index = 0; index < 1024; index++)
                {
                    var name = index == 0 ? meshName : string.Format("{0}_{1}", meshName, index);
                    if (tempNameList.Contains(name))
                        continue;

                    meshName = name;
                    break;
                }

                meshesWithUniqeNames.Add(mesh with { MeshName = meshName });
                tempNameList.Add(meshName);
            }

            return meshesWithUniqeNames;
        }
    }

    public static class WsModelGeneratorInputHelper
    {
        public static List<WsModelGeneratorInput> Create(MainEditableNode node, bool onlyVisibleNodes)
        {
            var lodNodes = node.GetLodNodes();
            var output = new List<WsModelGeneratorInput>();

            for (var lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = node.GetMeshesInLod(lodIndex, onlyVisibleNodes);

                for (var meshPart = 0; meshPart < meshes.Count; meshPart++)
                {
                    var instance = new WsModelGeneratorInput(lodIndex, meshPart, meshes[meshPart].Name, meshes[meshPart].Geometry.VertexFormat, meshes[meshPart].Material);
                    output.Add(instance);
                }
            }

            return output;
        }
    }

    public record WsModelGeneratorInput(
        int LodIndex,
        int MeshIndex,
        string MeshName,
        UiVertexFormat MeshVertexFormat,
        CapabilityMaterial Material);
}
