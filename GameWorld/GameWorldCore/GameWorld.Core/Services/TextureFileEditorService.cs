/*using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;*/

namespace GameWorld.Core.Services
{/*
    public class TextureFileEditorService
    {
        ILogger _logger = Logging.Create<TextureFileEditorService>();
        public string FilePreFix { get; set; } = "cust_";
        public string ProjectPath { get; set; } = "";
        readonly string ProjectFileName = "TextureProject.Json";
        public class Texture
        {
            public TextureType Type { get; set; }
            public string GamePath { get; set; }
            public string SystemPath { get; set; }
        }

        public class TextureItem
        {
            public bool PartOfProject { get; set; }
            public string SystemFilePath { get; set; }
            public bool IsFoundInProjectFolder { get; set; }

            public string PackFilePath { get; set; }
            public string UpdatedPackFilePath { get; set; }

            public TextureType Type { get; set; }
            public bool HasErrors { get; set; }
            public string ErrorString { get; set; } = "";
        }

        //public List<Texture> GetCurrentTextures() => _textures;
        public List<TextureType> ValidTextureTypes { get => new List<TextureType>() { TextureType.BaseColour, TextureType.Normal, TextureType.Mask, TextureType.MaterialMap }; }
        public List<TextureItem> TextureList { get; set; } = new List<TextureItem>();

        List<Texture> _textures { get; set; } = new List<Texture>();
        PackFileService _pfs;
        MainEditableNode _node;


        public TextureFileEditorService(MainEditableNode node, PackFileService pfs)
        {
            _pfs = pfs;
            _node = node;
        }


        public void UpdateStatus()
        {
            return;
            //TextureList.Clear();
            //
            //// Check if there is a project file there
            //var loadResult = LoadProject(ProjectPath + "\\" + ProjectFileName, false);
            //if (loadResult == true)
            //    return;
            //
            //ProjectPath = DetermineDefaultProjectName(_node);
            //var allTexutes = _node.GetMeshesInLod(0, false)
            //       .SelectMany(x => x.Material.GetAllTextures())
            //       .DistinctBy(x => x.Path.ToLower())
            //       .ToList();
            //
            //var cfgTextureItems = new List<Texture>();
            //foreach (var texture in allTexutes)
            //{
            //    cfgTextureItems.Add(new Texture()
            //    {
            //        Type = texture.TexureType,
            //        SystemPath = DetermineTextureName(ProjectPath, texture.Path),
            //        GamePath = texture.Path
            //    });
            //}
            //
            //BuildStatusFromCfg(cfgTextureItems);
        }

        void BuildStatusFromCfg(List<Texture> cfgItems)
        {
            var directoryContent = new string[0];
            if (Directory.Exists(ProjectPath))
                directoryContent = Directory.GetFiles(ProjectPath);

            foreach (var texture in cfgItems)
            {
                var fileSystemName = texture.SystemPath;
                var updateTexturePath = DetermineUpdatedTextureName(fileSystemName);
                var isValidPath = _pfs.FindFile(texture.GamePath) != null;
                var isValidTextureType = ValidTextureTypes.Contains(texture.Type);
                var isFoundInProjectFolder = directoryContent.Contains(fileSystemName, StringComparer.InvariantCultureIgnoreCase);

                var errorStr = "";
                if (!isValidPath)
                    errorStr += "Unable to find texture.";

                if (!isValidTextureType)
                    errorStr += "Texture type is not supported.";

                var textureItem = new TextureItem()
                {
                    Type = texture.Type,
                    SystemFilePath = texture.SystemPath,
                    PackFilePath = texture.GamePath,

                    //UpdatedPackFilePath = updateTexturePath,

                    IsFoundInProjectFolder = isFoundInProjectFolder,
                    PartOfProject = string.IsNullOrWhiteSpace(errorStr),
                    HasErrors = !string.IsNullOrWhiteSpace(errorStr),
                    ErrorString = errorStr
                };
                TextureList.Add(textureItem);
            }

            TextureList = TextureList.OrderBy(x => x.HasErrors).ToList();
        }


        bool EnsureCleanProject(string projectPath)
        {
            if (Directory.Exists(projectPath))
            {
                var numFiles = Directory.GetFiles(projectPath).Length;
                if (numFiles != 0)
                {
                    if (MessageBox.Show($"Project folder {projectPath} already exists. Do you want to delete it?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _logger.Here().Information($"Deleting project at {projectPath}");
                            Directory.Delete(projectPath, true);
                        }
                        catch (Exception e)
                        {
                            _logger.Here().Error($"Unable to delete folder {e.Message}");
                            MessageBox.Show("Unable to delete folder - Can not createa a new project");
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            DirectoryHelper.EnsureCreated(projectPath);
            _logger.Here().Information($"New project at {projectPath}");
            return true;
        }

        public void CreateProject()
        {
            _logger.Here().Information($"Creating project at {ProjectPath}");

            // Ensure folder is created and empty! Clean up the project if there is already one there. Return if the user dont want to delete it
            if (EnsureCleanProject(ProjectPath) == false)
                return;

            var projectCfg = new List<Texture>();
            foreach (var texture in TextureList)
            {
                if (texture.PartOfProject)
                {
                    var fileSystemName = texture.SystemFilePath;
                    var updateTexturePath = DetermineUpdatedTextureName(fileSystemName);

                    projectCfg.Add(new Texture()
                    {
                        Type = texture.Type,
                        GamePath = updateTexturePath,
                        SystemPath = texture.SystemFilePath,
                    });

                    var folderPath = Path.GetDirectoryName(texture.SystemFilePath);
                    DirectoryHelper.EnsureCreated(folderPath);
                    ExportTextureToFile(texture);
                    AddTextureToPackFile(texture);

                    // Upate reference in meshes
                    // Determine which models use this texture and update them
                    var allMeshes = _node.GetMeshesInLod(0, false);
                    foreach (var model in allMeshes)
                    {
                        // To array to create a copy of the list, so we dont get an exception while changing the list while itterating
                        var modelTextures = model.Material.GetAllTextures().ToArray();
                        foreach (var modelTexture in modelTextures)
                        {
                            var equalType = modelTexture.TexureType == texture.Type;
                            var equalPath = string.Compare(modelTexture.Path, texture.PackFilePath, StringComparison.InvariantCultureIgnoreCase) == 0;

                            if (equalType && equalPath)
                                model.UpdateTexture(updateTexturePath, modelTexture.TexureType, true);
                        }
                    }
                }
            }

            var projectCfgStr = JsonSerializer.Serialize(projectCfg, GetJsonConverterOptions());
            File.WriteAllText(ProjectPath + "\\" + ProjectFileName, projectCfgStr);
            SaveUvMaps(ProjectPath);
            UpdateStatus();
            OpenProjectFolder();
            _logger.Here().Information($"Texture project created");
        }

        public void RefreshProject()
        {
            // Are you sure
            // Read cfg and update paths (game path key)
            // add missing textures to cfg and export them
            // Remove textures no longer part of project
        }


        public void UpdateProject()
        {
            // Update settings
            // Update project files
            // Export missing files - textures and uvs
        }

        public void SetProjectDirectory(string newPath)
        {
            ProjectPath = newPath;
        }


        public bool LoadProject(string projectFilePath, bool updateTextures)
        {
            if (File.Exists(projectFilePath) == false)
                return false;

            try
            {
                var bytes = File.ReadAllBytes(projectFilePath);
                var projectCfg = JsonSerializer.Deserialize<List<Texture>>(bytes, GetJsonConverterOptions());
                BuildStatusFromCfg(projectCfg);
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Unable to load Project at {projectFilePath} error: {e.Message}");
                MessageBox.Show($"Unable to load project - {e.Message}");
                return false;
            }

            if (updateTextures)
                RefreshTextures();

            return true;
        }

        public void RefreshTextures()
        {
            foreach (var texture in TextureList)
            {
                if (texture.PartOfProject)
                {
                    AddTextureToPackFile(texture);

                    // Determine which models use this texture and update them
                    var allMeshes = _node.GetMeshesInLod(0, false);
                    foreach (var model in allMeshes)
                    {
                        // To array to create a copy of the list, so we dont get an exception while changing the list while itterating
                        var modelTextures = model.Material.GetAllTextures().ToArray();
                        foreach (var modelTexture in modelTextures)
                        {
                            var equalType = modelTexture.TexureType == texture.Type;
                            var equalPath = string.Compare(modelTexture.Path, texture.PackFilePath, StringComparison.InvariantCultureIgnoreCase) == 0;
                            if (equalType && equalPath)
                                model.UpdateTexture(texture.PackFilePath, modelTexture.TexureType, true);
                        }
                    }
                }
            }

        }

        public void OpenProjectFolder()
        {
            if (Directory.Exists(ProjectPath))
                Process.Start("explorer.exe", ProjectPath);
            else
                MessageBox.Show("Project folder not found");
        }


        JsonSerializerOptions GetJsonConverterOptions()
        {
            var jsonOptions = new JsonSerializerOptions() { WriteIndented = true };
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
            return jsonOptions;
        }

        private bool ExportTextureToFile(TextureItem texture)
        {
            var packFile = _pfs.FindFile(texture.PackFilePath);
            return TextureConverter.SaveAsPNG(packFile, texture.SystemFilePath);
        }


        private PackFile AddTextureToPackFile(TextureItem texture)
        {
            var filePath = texture.SystemFilePath;
            filePath = filePath.Replace(DirectoryHelper.Temp, "variantmeshes\\wh_variantmodels", StringComparison.InvariantCultureIgnoreCase);
            var packFile = TextureConverter.LoadTexture(_pfs, filePath, texture.SystemFilePath, texture.Type);
            return packFile;
        }

        //---------------------------

        string DetermineTextureName(string outputFolder, string texturePath)
        {
            var prefix = FilePreFix;
            var textureName = Path.GetFileName(texturePath);
            if (textureName.Contains(prefix, StringComparison.InvariantCultureIgnoreCase))
                prefix = "";

            var path = outputFolder + "\\" + prefix + textureName;
            path = Path.ChangeExtension(path, "png");
            return path;
        }

        string DetermineUpdatedTextureName(string fileSystemPath)
        {
            var filePath = fileSystemPath;
            filePath = filePath.Replace(DirectoryHelper.Temp, "variantmeshes\\wh_variantmodels", StringComparison.InvariantCultureIgnoreCase);
            filePath = Path.ChangeExtension(filePath, "dds");
            return filePath;
        }

        string DetermineDefaultProjectName(MainEditableNode node)
        {
            throw new NotImplementedException();
            //if (string.IsNullOrWhiteSpace(ProjectPath) == false)
            //    return ProjectPath;
            //
            //var path = _pfs.GetFullPath(node.MainPackFile);
            //var dirName = Path.GetDirectoryName(path);
            //dirName = dirName.Replace(@"variantmeshes\wh_variantmodels\", "", StringComparison.InvariantCultureIgnoreCase);
            //var projectDir = DirectoryHelper.Temp + "\\" + dirName;
            //return projectDir;
        }




        //---------------------------


        public void SaveUvMaps(string projectPath)
        {
            var meshes = _node.GetMeshesInLod(0, false);
            foreach (var mesh in meshes)
                ExportUvMap(projectPath + "\\UvMaps\\", mesh);
        }

        private void ExportUvMap(string outputDirectory, Rmv2MeshNode mesh)
        {
            var blackPen = new Pen(Color.Red, 1);
            using var image = new Bitmap(1024, 1024);
            using var graphics = Graphics.FromImage(image);

            for (var i = 0; i < mesh.Geometry.GetIndexCount(); i += 3)
            {
                var idx0 = mesh.Geometry.IndexArray[i + 0];
                var idx1 = mesh.Geometry.IndexArray[i + 1];
                var idx2 = mesh.Geometry.IndexArray[i + 2];

                var uv0 = mesh.Geometry.VertexArray[idx0].TextureCoordinate * 1024;
                var uv1 = mesh.Geometry.VertexArray[idx1].TextureCoordinate * 1024;
                var uv2 = mesh.Geometry.VertexArray[idx2].TextureCoordinate * 1024;

                graphics.DrawLine(blackPen, uv0.X, uv0.Y, uv1.X, uv1.Y);
                graphics.DrawLine(blackPen, uv1.X, uv1.Y, uv2.X, uv2.Y);
                graphics.DrawLine(blackPen, uv2.X, uv2.Y, uv0.X, uv0.Y);
            }

            var imagePathWithoutExtention = outputDirectory + "\\Uv_map_" + mesh.Name;
            for (var index = 0; index < 1024; index++)
            {
                var name = index == 0 ? imagePathWithoutExtention : string.Format("{0} _{1}", imagePathWithoutExtention, index);
                name += ".png";
                if (File.Exists(name))
                    continue;

                imagePathWithoutExtention = name;
                break;
            }

            DirectoryHelper.EnsureCreated(outputDirectory);
            image.Save(imagePathWithoutExtention);
        }
    }*/
}
