using System.Drawing;
using System.IO;
using System.Numerics;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{

    public interface IDdsToNormalPngExporter
    {
        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap);
        public ExportSupportEnum CanExportFile(PackFile file);
    }

    public class DdsToNormalPngExporter : IDdsToNormalPngExporter
    {
        private readonly IPackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;

        public DdsToNormalPngExporter(IPackFileService packFileService, IImageSaveHandler imageSaveHandler) 
        {
            _pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsMaterialFile(file.Name))
                return ExportSupportEnum.HighPriority;
            else if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap)
        {
            var packFile = _pfs.FindFile(filePath);
            if (packFile == null)
                return "";

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var outDirectory = Path.GetDirectoryName(outputPath);
            var rawFilePath = outDirectory + "/" + fileName + "_raw.png";

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                throw new Exception($"Could not read file data. bytes.Count = {bytes?.Length}");

            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (imgBytes == null || !imgBytes.Any())
                throw new Exception($"image data invalid/empty. imgBytes.Count = {imgBytes?.Length}");

            _imageSaveHandler.Save(imgBytes, rawFilePath);

            return rawFilePath;
        }
    }
}
