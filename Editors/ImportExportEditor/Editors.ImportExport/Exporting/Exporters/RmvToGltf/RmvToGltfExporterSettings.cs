using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(

        PackFile InputModelFile,
        List<PackFile> InputAnimationFiles,
        string OutputPath,
        bool ExportMaterials, 
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations,
        bool MirrorMesh,

        // Displacement map quality settings for 3D printing
        bool ExportDisplacementMaps = false,  // NEW: Control whether to export displacement variants
        int DisplacementIterations = 10,
        float DisplacementContrast = 0.1f,
        float DisplacementSharpness = 1.0f,
        bool Export16BitDisplacement = true,
        bool UseMultiScaleProcessing = true,
        bool UsePoissonReconstruction = true
    );
}
