using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public enum GltfSourceForwardDirection
    {
        PositiveZ,
        PositiveX,
        NegativeX,
        NegativeZ,
    }

    public record GltfImporterSettings
    (
        string InputGltfFile,
        string DestinationPackPath,
        IPackFileContainer DestinationPackFileContainer,
        GameTypeEnum SelectedGame,
        bool ImportMeshes,        
        bool ImportMaterials,
        bool ConvertMaterialFromBlenderType,
        bool ConvertNormalTextureFromBlueToOrangeType,
        bool ImportAnimations,
        float AnimationKeysPerSecond,
        bool AutoDetectAnimationKeysPerSecond = true,
        string? NewSkeletonName = null,
        bool AutoScaleHumanoid = true,
        GltfSourceForwardDirection SourceForwardDirection =
            GltfSourceForwardDirection.PositiveZ
    );
}
