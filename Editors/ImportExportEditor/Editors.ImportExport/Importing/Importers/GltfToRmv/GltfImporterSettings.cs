using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public record GltfImporterSettings
    (
        string InputGltfFile,
        string DestinationPackPath,
        PackFileContainer DestinationPackFileContainer,
        GameTypeEnum SelectedGame,
        bool ImportMeshes,        
        bool ImportMaterials,
        bool ConvertMaterialFromBlenderType,
        bool ConvertNormalTextureFromBlueToOrangeType,
        bool ImportAnimations,
        float AnimationKeysPerSecond,
        bool MirrorMesh
    );
}
