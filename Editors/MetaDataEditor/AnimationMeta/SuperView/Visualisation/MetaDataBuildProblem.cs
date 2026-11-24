using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Visualisation
{
    /// <summary>
    /// Why the preview drew a tag only in part, or not at all: a model, animation or vfx file that is not in the loaded packs,
    /// a docking slot or bone that is missing, or values the drawing refuses. ReasonKey is a localisation key with one
    /// {0}, filled with Detail.
    /// After Szy-Cathay/TheAssetEditor-CN MetaDataBuildDiagnostic (b32717bbc).
    /// </summary>
    public sealed record MetaDataBuildProblem(ParsedMetadataAttribute Source, string ReasonKey, string Detail = "");
}
