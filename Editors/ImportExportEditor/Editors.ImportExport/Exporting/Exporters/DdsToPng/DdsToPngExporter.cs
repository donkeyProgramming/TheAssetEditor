using System.IO;
using Editors.ImportExport.Common;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToPng
{
    public class DdsToPngExporter
    {
        private readonly IPackFileService _packFileService;
        private readonly IImageSaveHandler _imageSaveHandler;

        public DdsToPngExporter(IPackFileService pfs, IImageSaveHandler imageSaveHandler)
        {
            _packFileService = pfs;
            _imageSaveHandler = imageSaveHandler;
        }
        internal ImportExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsFile(file.Name))
                return ImportExportSupportEnum.Supported;
            return ImportExportSupportEnum.NotSupported;
        }

        public void Export(string outputPath, PackFile file)
        {
            var bytes = file.DataSource.ReadData();
            var fileDirectory = outputPath + "/" + Path.GetFileNameWithoutExtension(file.Name) + ".png";
            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            _imageSaveHandler.Save(imgBytes, fileDirectory);
        }

    }
}
