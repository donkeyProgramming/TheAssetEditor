using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.Core.PackFiles;
using System.IO;
using SharpGLTF.Materials;
using System.Numerics;
using SharpGLTF.Schema2;
using Shared.GameFormats.Animation;
using MeshImportExport;
using System.Windows;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(
        PackFile InputFile,
        string OutputPath,
        bool ExportTextures,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations
    );


    public class RmvToGltfExporter
    {
        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _exporterNormalBlue;
        private readonly DdsToPngExporter _ddsToPngExporter;

        public RmvToGltfExporter(PackFileService packFileSerivce, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToPngExporter ddsToPngExporter)
        {
            _packFileService = packFileSerivce;
            _exporterNormalBlue = ddsToNormalPngExporter;
            _ddsToPngExporter = ddsToPngExporter;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if(FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        internal void Export(RmvToGltfExporterSettings settings)
        {

            //need to adjust for static mesh
            //need to have a mesh only export
            _ddsToPngExporter.Export(settings.OutputPath, settings.InputFile);
            //setting for material texture conversion
            //setting for animations export

            //Have not attached the output path yet as the UI does not change that value as of now.
            //The file will go to C:/franz/ each export currently.
            //model.SaveGLTF(settings.OutputPath + settings.InputFile.Name);

            //need to fix the naming of the textures so I know when there are multiple normal maps to convert
            if (settings.ConvertNormalTextureToBlue == true)
            {
                var name = Path.GetFileNameWithoutExtension(settings.InputFile.Name);
                _exporterNormalBlue.Export("C:/franz/", name + "_0.png");
            }
        }
    }
}
