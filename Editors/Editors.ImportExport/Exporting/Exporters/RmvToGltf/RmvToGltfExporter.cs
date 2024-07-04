using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToPng
{
    internal class RmvToGltfExporter
    {
        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        internal void Export(string outputPath)
        {
            throw new NotImplementedException();
        }
    }
}
