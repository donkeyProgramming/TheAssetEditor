using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public class WsModelGeneratorService
    {
        private readonly ILogger _logger = Logging.Create<WsModelGeneratorService>();
        private readonly PackFileService _packFileService;

        public WsModelGeneratorService(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public string? GenerateWsModel(string modelFilePath, WsModelGeneratorInput[][] meshInformation, IWsMaterialBuilder materialBuilder)
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return null;
                }

                var materialPaths = CreateMaterials(materialBuilder, meshInformation);
                var wsModelData = CreateWsModel(modelFilePath, meshInformation, materialPaths);

                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");
                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());
                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile, Encoding.UTF8.GetBytes(wsModelData));

                return wsModelPath;
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.Message);
                MessageBox.Show("Generation failed!");
                return null;
            }
        }

        string[][] CreateMaterials(IWsMaterialBuilder materialBuilder, WsModelGeneratorInput[][] meshInformation)
        {
            // Load all materials
            var repository = new WsMaterialRepository(_packFileService);

            // Generate Unieq mesh names - i dont think this is needed?
            var uniqueMeshNames = new string[meshInformation.Length][];
            for (var lodIndex = 0; lodIndex < meshInformation.Length; lodIndex++)
                uniqueMeshNames[lodIndex] = GenerateUniqueMeshNames(meshInformation[lodIndex]).ToArray();

            var materialPaths = new string[meshInformation.Length][];
            for (var lodIndex = 0; lodIndex < meshInformation.Length; lodIndex++)
            {
                materialPaths[lodIndex] = new string[meshInformation[lodIndex].Length];
                for (var meshIndex = 0; meshIndex < meshInformation[lodIndex].Length; meshIndex++)
                {
                    var currentMesh = meshInformation[lodIndex][meshIndex];
                    var uniqeMeshName = uniqueMeshNames[lodIndex][meshIndex];
                    var materialFile = materialBuilder.Create(uniqeMeshName, currentMesh.MeshVertexFormat, currentMesh.Material);

                    // Check if file is uniqe - if not use original
                    var newMaterialPath = " " + materialFile.FileName;
                    var materialPath = repository.GetExistingOrAddMaterial(materialFile.FileContent, newMaterialPath);


                    materialPaths[lodIndex][meshIndex] = materialPath;
                }
            }

            return materialPaths;
        }

        static string CreateWsModel(string modelFilePath, WsModelGeneratorInput[][] meshInformation, string[][] materialPaths)
        {
            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            for (var lodIndex = 0; lodIndex < meshInformation.Length; lodIndex++)
            {
                for (var meshIndex = 0; meshIndex < meshInformation[lodIndex].Length; meshIndex++)
                {
                    sb.Append($"\t\t\t<material lod_index=\"{lodIndex}\" part_index=\"{meshIndex}\">");
                    sb.Append(materialPaths[lodIndex][meshIndex]);
                    sb.Append("</material>\n");
                }

                sb.AppendLine();
            }

            sb.Append("\t</materials>\n");
            sb.Append("</model>\n");

            return sb.ToString();
        }

        static private List<string> GenerateUniqueMeshNames(WsModelGeneratorInput[] meshes)
        {
            var output = new List<string>();
            foreach (var mesh in meshes)
            {
                var fileName = mesh.MeshName;
                for (var index = 0; index < 1024; index++)
                {
                    var name = index == 0 ? fileName : string.Format("{0}_{1}", fileName, index);
                    if (output.Contains(name))
                        continue;

                    fileName = name;
                    break;
                }

                output.Add(fileName);
            }

            return output;
        }

    }


    public class WsMaterialRepository
    {
        private readonly Dictionary<string, string> _map;

        public WsMaterialRepository(PackFileService packFileService)
        {
            _map = LoadAllExistingMaterials(packFileService);
        }

        public string GetExistingOrAddMaterial(string wsMaterialContent, string WsMaterialPath)
        {
            var sanitizedWsMaterial = SanatizeMaterial(wsMaterialContent);
            var found = _map.TryGetValue(sanitizedWsMaterial, out var path);
            if (found == false)
            {
                _map[sanitizedWsMaterial] = WsMaterialPath;
                return WsMaterialPath;
            }
            return path!;
        }

        string SanatizeMaterial(string wsMaterialContent)
        {
            var start = wsMaterialContent.IndexOf("<name>");
            var end = wsMaterialContent.IndexOf("</name>", start);
            var contentWithoutName = wsMaterialContent.Remove(start, end).ToLower();

            return wsMaterialContent;
        }


        Dictionary<string, string> LoadAllExistingMaterials(PackFileService packFileService)
        {
            var materialList = new Dictionary<string, string>();

            var materialPacks = packFileService.FindAllWithExtentionIncludePaths(".material");
            materialPacks = materialPacks.Where(x => x.Pack.Name.Contains(".xml.material")).ToList();

            foreach (var (FileName, Pack) in materialPacks)
            {
                var bytes = Pack.DataSource.ReadData();
                var content = Encoding.UTF8.GetString(bytes);
                var sanitizedWsMaterial = SanatizeMaterial(content);


                materialList[sanitizedWsMaterial] = FileName;
            }

            return materialList;
        }
    }

    public static class WsModelGeneratorInputHelper
    {
        public static WsModelGeneratorInput[][] Create(MainEditableNode node)
        {
            var lodNodes = node.GetLodNodes();
            var output = new WsModelGeneratorInput[lodNodes.Count][];

            for (var lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = node.GetMeshesInLod(lodIndex, false);
                var meshIndex = 0;
                output[lodIndex] = meshes
                    .Select(x => new WsModelGeneratorInput(lodIndex, meshIndex++, meshes[meshIndex].Name, meshes[meshIndex].Geometry.VertexFormat, meshes[meshIndex].Effect))
                    .ToArray();
            }

            return output;
        }
    }

    public record WsModelGeneratorInput(
        int LodIndex,
        int MeshIndex, string MeshName,
        UiVertexFormat MeshVertexFormat,
        CapabilityMaterial Material);
}
