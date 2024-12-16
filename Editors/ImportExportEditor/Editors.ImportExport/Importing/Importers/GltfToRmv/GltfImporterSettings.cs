using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public record GltfImporterSettings
    (
        string InputGltfFile,
        string DestinationPackPath,
        PackFileContainer DestinationPackFileContainer,
        bool ConvertNormalTextureToOrangeType,
        bool ImportAnimations,
        bool MirrorMesh
    );
}
