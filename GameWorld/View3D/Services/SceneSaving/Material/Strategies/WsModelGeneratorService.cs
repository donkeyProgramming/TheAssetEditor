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

        public (bool Status, string? CreatedFilePath) GenerateWsModel(string modelFilePath, List<WsModelGeneratorInput> meshInformation, IWsMaterialBuilder materialBuilder)
        {
            try
            {
                if (_packFileService.GetEditablePack() == null)
                {
                    MessageBox.Show("No editable pack selected", "error");
                    return (false, null);
                }

                var materialPaths = CreateMaterials(modelFilePath, materialBuilder, meshInformation);
                var wsModelData = CreateWsModel(modelFilePath, materialPaths);

                var wsModelPath = Path.ChangeExtension(modelFilePath, ".wsmodel");
                var existingWsModelFile = _packFileService.FindFile(wsModelPath, _packFileService.GetEditablePack());
                SaveHelper.Save(_packFileService, wsModelPath, existingWsModelFile, Encoding.UTF8.GetBytes(wsModelData));

                return (true, wsModelPath);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error generating ws model - " + e.Message);
                MessageBox.Show("Generation failed!");
                return (false, null);
            }
        }

        List<WsModelRow> CreateMaterials(string modelFilePath, IWsMaterialBuilder materialBuilder, List<WsModelGeneratorInput> meshInformation)
        {
            // Load all materials
            var repository = new WsMaterialRepository(_packFileService);

            var output = new List<WsModelRow>();
            var uniqueMeshNames = GenerateUniqueMeshNames(meshInformation);

            for (var i = 0; i < meshInformation.Count; i++)
            {
                var currentMesh = meshInformation[i];
                var uniqeMeshName = uniqueMeshNames[i];
                var materialFile = materialBuilder.Create(uniqeMeshName, currentMesh.MeshVertexFormat, currentMesh.Material);

                // Check if file is uniqe - if not use original. We do this to avid an explotion of materials.
                // Kitbashed models sometimes have severl hundred meshes, we dont want that many materials if not needed
                var newMaterialPath = Path.GetDirectoryName(modelFilePath) + "/materials/" + materialFile.FileName;
                var materialPath = repository.GetExistingOrAddMaterial(materialFile.FileContent, newMaterialPath, out var isNew);
                if (isNew)
                {
                    var existingMaterialPackFile = _packFileService.FindFile(newMaterialPath, _packFileService.GetEditablePack());
                    SaveHelper.Save(_packFileService, newMaterialPath, existingMaterialPackFile, Encoding.UTF8.GetBytes(materialFile.FileContent));
                }

                output.Add(new WsModelRow(currentMesh.LodIndex, currentMesh.MeshIndex, materialPath));
            }

            return output;
        }

        static string CreateWsModel(string modelFilePath, List<WsModelRow> meshInformation)
        {
            var sb = new StringBuilder();

            sb.Append("<model version=\"1\">\n");
            sb.Append($"\t<geometry>{modelFilePath}</geometry>\n");
            sb.Append("\t\t<materials>\n");

            var orderedMeshInfo = meshInformation
                .OrderBy(x=>x.LodIndex)
                .ToList();

            foreach (var meshInfo in orderedMeshInfo)
            {
                sb.Append($"\t\t\t<material lod_index=\"{meshInfo.LodIndex}\" part_index=\"{meshInfo.MeshIndex}\">");
                sb.Append(meshInfo.MaterialFilePath);
                sb.Append("</material>\n");
            }

            sb.Append("\t</materials>\n");
            sb.Append("</model>\n");

            return sb.ToString();
        }

        static private List<string> GenerateUniqueMeshNames(IEnumerable<WsModelGeneratorInput> meshes)
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

        public string GetExistingOrAddMaterial(string wsMaterialContent, string wsMaterialPath, out bool isNew)
        {
            var sanitizedWsMaterial = SanatizeMaterial(wsMaterialContent);
            var found = _map.TryGetValue(sanitizedWsMaterial, out var path);
            if (found == false)
            {
                _map[sanitizedWsMaterial] = wsMaterialPath;
                isNew = true;
                return wsMaterialPath;
            }
            isNew = false;
            return path!;
        }

        string SanatizeMaterial(string wsMaterialContent)
        {
            var start = wsMaterialContent.IndexOf("<name>");
            var end = wsMaterialContent.IndexOf("</name>", start);
            var contentWithoutName = wsMaterialContent.Remove(start, end).ToLower();

            return contentWithoutName;
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
        public static List<WsModelGeneratorInput> Create(MainEditableNode node)
        {
            var lodNodes = node.GetLodNodes();
            var output = new List<WsModelGeneratorInput>();

            for (var lodIndex = 0; lodIndex < lodNodes.Count; lodIndex++)
            {
                var meshes = node.GetMeshesInLod(lodIndex, false);
                var meshIndex = 0;
                var rows = meshes
                    .Select(x => new WsModelGeneratorInput(lodIndex, meshIndex++, meshes[meshIndex].Name, meshes[meshIndex].Geometry.VertexFormat, meshes[meshIndex].Effect))
                    .ToList();
                output.AddRange(rows);
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

    public record WsModelRow(
        int LodIndex,
        int MeshIndex, 
        string MaterialFilePath);
}
