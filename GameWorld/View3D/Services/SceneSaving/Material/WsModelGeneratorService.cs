using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material
{
    public record WsMaterialResult(bool Result, string? Content, string? GeneratedFilePath);

    public class WsModelGeneratorService
    {
        private readonly ILogger _logger = Logging.Create<WsModelGeneratorService>();
        private readonly IPackFileService _packFileService;
        private readonly IFileSaveService _packFileSaveService;

        public WsModelGeneratorService(IPackFileService packFileService, IFileSaveService packFileSaveService)
        {
            _packFileService = packFileService;
            _packFileSaveService = packFileSaveService;
        }

        public WsMaterialResult GenerateWsModel(IMaterialToWsMaterialSerializer wsMaterialGenerator, string modelFilePath, List<WsModelGeneratorInput> meshInformation)
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return new WsMaterialResult(false, null, null); 
                }
               
                var wsModelData = CreateWsModel(wsMaterialGenerator, modelFilePath, meshInformation);

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

        static string CreateWsModel(IMaterialToWsMaterialSerializer wsMaterialGenerator, string modelFilePath, List<WsModelGeneratorInput> meshInformation)
        {
            var meshesWithUniqeNames = EnsureUniqueMeshNames(meshInformation);
            var orderedMeshInfo = meshesWithUniqeNames
                .OrderBy(x => x.LodIndex)
                .ToList();

            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            foreach (var meshInfo in orderedMeshInfo)
            {
                var materialPath = wsMaterialGenerator.ProsessMaterial(modelFilePath, meshInfo.MeshName, meshInfo.MeshVertexFormat, meshInfo.Material);
             
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
