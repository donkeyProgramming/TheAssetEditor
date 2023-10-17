using System.Linq;
using System.Windows.Forms;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Interfaces.AssetManagement;
using CommonControls.Services;

namespace CommonControls.Events.UiCommands
{
    public class ExportAssetCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;
        private readonly IAssetExporterProvider _assetManagementFactory;


        public ExportAssetCommand(PackFileService packFileService, IAssetExporterProvider assetExportProvider)
        {
            _packFileService = packFileService;
            _assetManagementFactory = assetExportProvider;
        }

        /// <summary>
        /// Exports complete asset from packfile, input path can be RMV2, WSMODE, VMD
        /// </summary>        
        public void Execute(PackFileContainer fileOwner, string pathModel, string pathAnimationClip = "")
        {
            var exportData = ExportHelper.FetchParsedInputFiles(_packFileService, pathModel);

            exportData.DestinationPath = GetPickFile();
            if (!exportData.DestinationPath.Any()) { 
                return; 
            }

            var exporter = _assetManagementFactory.GetExporter(".fbx");

            var binaryFileData = exporter.ExportAsset(exportData);
        }

        private string GetPickFile()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = "";
                saveFileDialog.Filter = "FBX Files (*.fbx)|*.fbx|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {                    
                    filePath = saveFileDialog.FileName;

                    return filePath;
                }
            }

            return "";
        }
    }

}
