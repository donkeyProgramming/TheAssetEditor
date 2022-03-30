using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using MoreLinq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Services;
using View3D.Utility;
using MessageBox = System.Windows.MessageBox;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class TextureFileEditorServiceViewModel
    {
        ILogger _logger = Logging.Create<TextureFileEditorServiceViewModel>();

        public class TextureItem
        {
            public bool PartOfProject { get; set; }
            public string SystemFilePath { get; set; }
            public bool IsFoundInProjectFolder { get; set; }
     
            public string PackFilePath { get; set; }
            public string UpdatedPackFilePath { get; set; }

            public TexureType Type { get; set; }
            public bool HasErrors { get; set; }
            public string ErrorString { get; set; } = "";
        }

        MainEditableNode _mainNode;
        TextureFileEditorService _textureService;
        PackFileService _pfs;

        public ICommand CreateProjectCommand { get; set; }
        public ICommand AddMissingTextureCommand { get; set; }
        public ICommand RefreshTexturesCommand { get; set; }
        public ICommand OpenFolderCommand { get; set; }
        public ICommand BrowseCommand { get; set; }
 

        public NotifyAttr<bool> IsRunning { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> FilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> FilePrefix { get; set; } = new NotifyAttr<string>("");

        public ObservableCollection<TextureItem> TextureList { get; set; } = new ObservableCollection<TextureItem>();

        public TextureFileEditorServiceViewModel(MainEditableNode mainNode, PackFileService pfs)
        {
            CreateProjectCommand = new RelayCommand(CreateProject);
            AddMissingTextureCommand = new RelayCommand(AddMissingTextures);
            RefreshTexturesCommand = new RelayCommand(RefreshTextures);

            OpenFolderCommand = new RelayCommand(OpenFolder);
            BrowseCommand = new RelayCommand(Browse);

            _pfs = pfs;
            _mainNode = mainNode;
            _textureService = _mainNode.TextureFileEditorService;

            UpdateViewData();
        }

        void UpdateViewData()
        {
            var projectFolder = _textureService.ProjectPath;
            if (string.IsNullOrEmpty(projectFolder))
                projectFolder = DetermineDefaultProjectName(_mainNode);

            string[] directoryContent = new string[0];
            if(Directory.Exists(projectFolder))
                directoryContent  = Directory.GetFiles(projectFolder);

            var allTexutes = _mainNode.GetMeshesInLod(0, false)
                   .SelectMany(x => x.Material.GetAllTextures())
                   .DistinctBy(x => x.Path.ToLower())
                   .ToList();

            var textureList = new List<TextureItem>();
            foreach (var texture in allTexutes)
            {
                var fileSystemName = DetermineTextureName(projectFolder, texture.Path);
                var updateTexturePath = DetermineUpdatedTextureName(fileSystemName);
                var isValidPath = _pfs.FindFile(texture.Path) != null;
                var isValidTextureType = _textureService.ValidTextureTypes.Contains(texture.TexureType);
                var isFoundInProjectFolder = directoryContent.Contains(fileSystemName, StringComparer.InvariantCultureIgnoreCase);

                var errorStr = "";
                if (!isValidPath)
                    errorStr += "Unable to find texture. ";

                if (!isValidTextureType)
                    errorStr += "Texture type is not supported. ";

                var textureItem = new TextureItem()
                {
                    IsFoundInProjectFolder = isFoundInProjectFolder,
                    PackFilePath = texture.Path,
                    SystemFilePath = DetermineTextureName(projectFolder, texture.Path),
                    UpdatedPackFilePath = updateTexturePath,
                    Type = texture.TexureType,
                    PartOfProject = string.IsNullOrWhiteSpace(errorStr),
                    HasErrors = string.IsNullOrWhiteSpace(errorStr),
                    ErrorString = errorStr
                };
                textureList.Add(textureItem);
            }

            TextureList.Clear();
            foreach (var item in textureList.OrderByDescending(x=>x.HasErrors))
                TextureList.Add(item);

            FilePrefix.Value = _textureService.FilePreFix;
            FilePath.Value = projectFolder;
        }


        string DetermineTextureName(string outputFolder, string texturePath)
        {
            var prefix = _textureService.FilePreFix;
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
            var path = _pfs.GetFullPath(node.MainPackFile);
            var dirName =  Path.GetDirectoryName(path);
            dirName = dirName.Replace(@"variantmeshes\wh_variantmodels\", "", StringComparison.InvariantCultureIgnoreCase);
            var projectDir = DirectoryHelper.Temp + "\\" + dirName;
            return projectDir;
        }

        public void CreateProject()
        {
            // Ensure folder is created and empty! 
            var projectPath = FilePath.Value;
            _logger.Here().Information($"Creating project at {projectPath}");

            if (Directory.Exists(projectPath))
            {
                var numFiles = Directory.GetFiles(projectPath).Length;
                if (numFiles != 0)
                {
                    if (MessageBox.Show($"Project folder {projectPath} already exists. Do you want to delete it?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Directory.Delete(projectPath, true);
                        }
                        catch (Exception e)
                        {
                            _logger.Here().Error($"Unable to delete folder {e.Message}");
                            MessageBox.Show("Unable to delete folder - Can not createa a new project");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            DirectoryHelper.EnsureCreated(projectPath);
            _textureService.ProjectPath = projectPath;

            foreach (var texture in TextureList)
            {
                if (texture.PartOfProject)
                {
                    var folderPath = Path.GetDirectoryName(texture.SystemFilePath);
                    DirectoryHelper.EnsureCreated(folderPath);
                    ExportTextureToFile(texture); 
                }
            }

            UpdateTexturesFromDirectory();
            _textureService.SaveUvMaps(projectPath);
            UpdateViewData();
            OpenFolder();
        }

        void UpdateTexturesFromDirectory()
        {
            foreach (var texture in TextureList)
            {
                if (texture.PartOfProject)
                {
                    AddTextureToPackFile(texture);

                    // Determine which models use this texture and update them
                    var allMeshes = _mainNode.GetMeshesInLod(0, false);
                    foreach (var model in allMeshes)
                    {
                        // To array to create a copy of the list, so we dont get an exception while changing the list while itterating
                        var modelTextures = model.Material.GetAllTextures().ToArray();
                        foreach (var modelTexture in modelTextures)
                        {
                            var equalType = modelTexture.TexureType == texture.Type;
                            var equalPath = string.Compare(modelTexture.Path, texture.PackFilePath, StringComparison.InvariantCultureIgnoreCase) == 0;
                            if (equalType && equalPath)
                                model.UpdateTexture(texture.UpdatedPackFilePath, modelTexture.TexureType, true);
                        }
                    }
                }
            }
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



        public void AddMissingTextures()
        {
    
        }

        void OpenFolder()
        {
            if (Directory.Exists(FilePath.Value))
                Process.Start("explorer.exe", FilePath.Value);
            else
                MessageBox.Show("Project folder not found");
        }

        void Browse()
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = FilePath.Value;
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            FilePath.Value = dialog.SelectedPath;
            _textureService.ProjectPath = dialog.SelectedPath;
            UpdateViewData();
        }

        void RefreshTextures()
        {
            UpdateTexturesFromDirectory();
        }

        string CreateDefaultOutputDirectoryPath(MainEditableNode mainNode)
        {
            var path = DirectoryHelper.Temp + "\\" + Path.GetFileNameWithoutExtension(mainNode.MainPackFile.Name);
            for (int index = 0; index < 1024; index++)
            {
                var potentialPath = (index == 0) ? path : string.Format("{0} _{1}", path, index);
                if (Directory.Exists(potentialPath))
                    continue;

                path = potentialPath;
                break;
            }

            return path;
        }
    }
}
