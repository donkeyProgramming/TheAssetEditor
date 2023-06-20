using System.IO;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommonControls.Common;
using Serilog;
using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CommonControls.ModelImportExport
{
    public class AssimpDiskService
    {
        ILogger _logger = Logging.Create<AssimpDiskService>();

        private PackFileService _packfileService;
        public AssimpDiskService(PackFileService pfs)
        {
            _packfileService = pfs;
        }
        public void ImportAssimpDiskFileToPack(PackFileContainer container, string parentPackPath, string filePath)
        {
            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var rigidModelExtension = ".rigid_model_v2";
            var outFileName = fileNameNoExt + rigidModelExtension;

            var assimpImporterService = new AssimpImporter(_packfileService);
            assimpImporterService.ImportScene(filePath);

            var rmv2File = assimpImporterService.MakeRMV2File();
            var factory = ModelFactory.Create();
            var buffer = factory.Save(rmv2File);

            var packFile = new PackFile(outFileName, new MemorySource(buffer));
            _packfileService.AddFileToPack(container, parentPackPath, packFile);
        }

       public void Import3dModelToPackTree(PackFileContainer owner, string parentPath)       
       {            
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var files = dialog.FileNames;
                foreach (var file in files)
                {
                    try
                    {                        
                        ImportAssimpDiskFileToPack(owner, parentPath, file);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Failed to import model file {file}. Error : {e.Message}", "Error");
                        _logger.Here().Error($"Failed to load file {file}. Error : {e}");
                    }
                }
            }
        }

        static public string GetDialogFilterSupportedFormats()
        {
            var unmangedLibrary = Assimp.Unmanaged.AssimpLibrary.Instance;
            var suportetFileExtensions = unmangedLibrary.GetExtensionList();

            var filter = "3d Models (ALL)|";

            // All model formats in one
            foreach (var ext in suportetFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            // ech model format separately
            foreach (var ext in suportetFileExtensions)
            {
                filter += "|" + ext.Remove(0, 1) + "(" + ext + ")|" + "*" + ext;
            }

            filter += "|All files(*.*) | *.*";

            return filter;
        }
    }
}
