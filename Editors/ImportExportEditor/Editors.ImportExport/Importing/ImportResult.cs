namespace Editors.ImportExport.Importing;

public sealed record ImportResult(
    bool Succeeded,
    IReadOnlyList<string> OutputPaths,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    Exception? Exception = null,
    HumanoidScaleImportSummary? HumanoidScale = null,
    MaterialImportSummary? MaterialSummary = null,
    SourceForwardImportSummary? SourceForward = null)
{
    public static ImportResult Success(
        IReadOnlyList<string> outputPaths,
        IReadOnlyList<string>? warnings = null,
        HumanoidScaleImportSummary? humanoidScale = null,
        MaterialImportSummary? materialSummary = null,
        SourceForwardImportSummary? sourceForward = null) =>
        new(
            true,
            outputPaths,
            warnings ?? [],
            [],
            HumanoidScale: humanoidScale,
            MaterialSummary: materialSummary,
            SourceForward: sourceForward);

    public static ImportResult Failure(Exception exception) =>
        new(false, [], [], [exception.Message], exception);

    public static ImportResult Failure(
        IReadOnlyList<string> errors) =>
        new(false, [], [], errors);
}

public sealed record HumanoidScaleImportSummary(
    bool Applied,
    float? SourceHeight,
    float? ReferenceHeight,
    float ScaleFactor,
    string Reason);

public sealed record SourceForwardImportSummary(
    string SourceDirection,
    string Conversion);

public sealed record MaterialImportSummary(
    IReadOnlyList<MaskedMaterialImportSummary> MaskedMaterials,
    IReadOnlyList<SkippedMaterialSemantic> SkippedSemantics);

public sealed record MaskedMaterialImportSummary(
    string MaterialName,
    float AlphaCutoff);

public sealed record SkippedMaterialSemantic(
    string MaterialName,
    string Semantic);
