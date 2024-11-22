using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(

        PackFile InputModelFile,
        List<PackFile> InputAnimationFiles,
        string OutputPath,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations,
        bool MirrorMesh
    );
}
