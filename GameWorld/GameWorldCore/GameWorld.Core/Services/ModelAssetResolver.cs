using System.Collections.ObjectModel;
using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Services;

/// <summary>
/// Resolves a model input to the geometry and effective material data used by
/// AssetEditor's model views.  This type deliberately contains no renderer or
/// UI objects so it can also be used by import/export code.
/// </summary>
public interface IModelAssetResolver
{
    ResolvedModelAsset Resolve(PackFile inputFile);

    /// <summary>
    /// Resolves the effective material for an already parsed RMV2 model. This
    /// keeps the legacy scene-loader entry point on the same interpretation as
    /// <see cref="Resolve(PackFile)"/>.
    /// </summary>
    ResolvedModelMaterials ResolveMaterials(RmvFile model, WsModelFile? wsModel = null, string? modelPath = null);
}

public sealed class ModelAssetResolver : IModelAssetResolver
{
    private readonly IPackFileService? _packFileService;

    public ModelAssetResolver(IPackFileService? packFileService = null)
    {
        _packFileService = packFileService;
    }

    public ResolvedModelAsset Resolve(PackFile inputFile)
    {
        ArgumentNullException.ThrowIfNull(inputFile);

        var diagnostics = new List<string>();
        PackFile geometryFile;
        WsModelFile? wsModel = null;
        PackFile? wsModelFile = null;

        if (IsRmvFile(inputFile.Name))
        {
            geometryFile = inputFile;
            wsModelFile = FindSiblingWsModel(inputFile);
            if (wsModelFile != null)
            {
                try
                {
                    wsModel = new WsModelFile(wsModelFile);
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Unable to read sibling WSModel '{wsModelFile.Name}'; using RMV2 materials. {exception.Message}");
                    wsModelFile = null;
                }
            }
        }
        else if (IsWsModelFile(inputFile.Name))
        {
            wsModelFile = inputFile;
            WsModelFile parsedWsModel;
            try
            {
                parsedWsModel = new WsModelFile(inputFile);
            }
            catch (Exception exception)
            {
                throw new ModelAssetResolutionException($"Unable to read WSModel '{inputFile.Name}'.", exception);
            }

            wsModel = parsedWsModel;
            if (string.IsNullOrWhiteSpace(parsedWsModel.GeometryPath))
                throw new ModelAssetResolutionException($"WSModel '{inputFile.Name}' does not specify a geometry file.");

            geometryFile = FindFile(parsedWsModel.GeometryPath)
                ?? throw new ModelAssetResolutionException($"WSModel '{inputFile.Name}' references missing geometry '{parsedWsModel.GeometryPath}'.");
        }
        else
        {
            throw new ModelAssetResolutionException($"Unsupported model input '{inputFile.Name}'. Expected a .rigid_model_v2 or .wsmodel file.");
        }

        RmvFile rmvFile;
        try
        {
            rmvFile = ModelFactory.Create().Load(geometryFile.DataSource.ReadData());
        }
        catch (Exception exception)
        {
            throw new ModelAssetResolutionException($"Unable to read geometry '{geometryFile.Name}'.", exception);
        }

        var effectiveMaterials = ResolveMaterials(rmvFile, wsModel, wsModelFile?.Name);
        diagnostics.AddRange(effectiveMaterials.Diagnostics);
        return new ResolvedModelAsset(
            inputFile,
            geometryFile,
            wsModelFile,
            wsModel,
            rmvFile,
            effectiveMaterials.PartsByLod,
            diagnostics);
    }

    public ResolvedModelMaterials ResolveMaterials(
        RmvFile rmvFile,
        WsModelFile? wsModel = null,
        string? modelPath = null)
    {
        ArgumentNullException.ThrowIfNull(rmvFile);

        var diagnostics = new List<string>();
        var resolvedWsModel = wsModel;
        var resolvedWsModelPath = modelPath;

        // The legacy loader receives a parsed RMV2 model and its full pack
        // path rather than the source PackFile. Discover the sibling through
        // the pack service so nested models keep their directory context.
        if (resolvedWsModel == null
            && _packFileService != null
            && string.IsNullOrWhiteSpace(modelPath) == false
            && IsRmvFile(modelPath))
        {
            var siblingPath = Path.ChangeExtension(modelPath, ".wsmodel");
            var siblingFile = FindFile(siblingPath);
            if (siblingFile != null)
            {
                try
                {
                    resolvedWsModel = new WsModelFile(siblingFile);
                    resolvedWsModelPath = siblingFile.Name;
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Unable to read sibling WSModel '{siblingFile.Name}'; using RMV2 materials. {exception.Message}");
                }
            }
        }

        var partsByLod = ResolveMaterials(rmvFile, resolvedWsModel, resolvedWsModelPath, diagnostics);
        return new ResolvedModelMaterials(partsByLod, diagnostics);
    }

    private List<IReadOnlyList<ResolvedModelPart>> ResolveMaterials(
        RmvFile rmvFile,
        WsModelFile? wsModel,
        string? wsModelPath,
        List<string> diagnostics)
    {
        var output = new List<IReadOnlyList<ResolvedModelPart>>(rmvFile.ModelList.Length);
        var useWsModel = wsModel != null && HasCompleteMaterialMapping(rmvFile, wsModel, wsModelPath, diagnostics);

        for (var lodIndex = 0; lodIndex < rmvFile.ModelList.Length; lodIndex++)
        {
            var parts = new List<ResolvedModelPart>(rmvFile.ModelList[lodIndex].Length);
            for (var partIndex = 0; partIndex < rmvFile.ModelList[lodIndex].Length; partIndex++)
            {
                var rmvModel = rmvFile.ModelList[lodIndex][partIndex];
                WsModelMaterialFile? wsMaterial = null;
                string? wsMaterialPath = null;

                if (useWsModel)
                {
                    var mapping = wsModel!.MaterialList.First(x => x.LodIndex == lodIndex && x.PartIndex == partIndex);
                    wsMaterialPath = mapping.MaterialPath;
                    var materialFile = FindFile(mapping.MaterialPath);
                    if (materialFile == null)
                    {
                        diagnostics.Add($"WSModel material '{mapping.MaterialPath}' for LOD {lodIndex}, part {partIndex} was not found; using RMV2 material.");
                    }
                    else
                    {
                        try
                        {
                            wsMaterial = new WsModelMaterialFile(materialFile);
                        }
                        catch (Exception exception)
                        {
                            diagnostics.Add($"Unable to read WSModel material '{mapping.MaterialPath}' for LOD {lodIndex}, part {partIndex}; using RMV2 material. {exception.Message}");
                        }
                    }
                }

                var effectiveMaterial = ResolvedModelMaterial.Create(rmvModel.Material, wsMaterial, wsMaterialPath);
                parts.Add(new ResolvedModelPart(lodIndex, partIndex, rmvModel, effectiveMaterial));
            }

            output.Add(new ReadOnlyCollection<ResolvedModelPart>(parts));
        }

        return output;
    }

    private bool HasCompleteMaterialMapping(
        RmvFile rmvFile,
        WsModelFile wsModel,
        string? wsModelPath,
        List<string> diagnostics)
    {
        var expected = new HashSet<(int LodIndex, int PartIndex)>();
        for (var lodIndex = 0; lodIndex < rmvFile.ModelList.Length; lodIndex++)
        {
            for (var partIndex = 0; partIndex < rmvFile.ModelList[lodIndex].Length; partIndex++)
                expected.Add((lodIndex, partIndex));
        }

        var actual = new HashSet<(int LodIndex, int PartIndex)>();
        foreach (var entry in wsModel.MaterialList)
        {
            if (expected.Contains((entry.LodIndex, entry.PartIndex)) == false)
            {
                // The viewport ignores mappings for geometry that is not
                // present in the loaded RMV2. Keep the same behavior here.
                continue;
            }

            if (actual.Add((entry.LodIndex, entry.PartIndex)) == false)
            {
                diagnostics.Add($"WSModel '{wsModelPath ?? "<unknown>"}' contains duplicate material mappings; using RMV2 materials.");
                return false;
            }
        }

        if (actual.SetEquals(expected))
            return true;

        diagnostics.Add($"WSModel '{wsModelPath ?? "<unknown>"}' does not contain one material mapping for every RMV2 LOD/part; using RMV2 materials.");
        return false;
    }

    private PackFile? FindSiblingWsModel(PackFile rmvFile)
    {
        if (_packFileService == null)
            return null;

        var rmvPath = _packFileService.GetFullPath(rmvFile);
        var siblingPath = Path.ChangeExtension(rmvPath, ".wsmodel");
        return FindFile(siblingPath);
    }

    private PackFile? FindFile(string path)
    {
        return _packFileService?.FindFile(path);
    }

    private static bool IsRmvFile(string path) => path.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase);

    private static bool IsWsModelFile(string path) => path.EndsWith(".wsmodel", StringComparison.OrdinalIgnoreCase);
}

public sealed class ModelAssetResolutionException : Exception
{
    public ModelAssetResolutionException(string message)
        : base(message)
    {
    }

    public ModelAssetResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class ResolvedModelMaterials
{
    public ResolvedModelMaterials(
        IReadOnlyList<IReadOnlyList<ResolvedModelPart>> partsByLod,
        IReadOnlyList<string> diagnostics)
    {
        PartsByLod = partsByLod;
        MaterialsByLod = partsByLod
            .Select(parts => (IReadOnlyList<ResolvedModelMaterial>)new ReadOnlyCollection<ResolvedModelMaterial>(parts.Select(x => x.Material).ToList()))
            .ToList();
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<IReadOnlyList<ResolvedModelPart>> PartsByLod { get; }
    public IReadOnlyList<IReadOnlyList<ResolvedModelMaterial>> MaterialsByLod { get; }
    public IReadOnlyList<string> Diagnostics { get; }
}

public sealed class ResolvedModelAsset
{
    public ResolvedModelAsset(
        PackFile inputFile,
        PackFile geometryFile,
        PackFile? wsModelFile,
        WsModelFile? wsModel,
        RmvFile model,
        IReadOnlyList<IReadOnlyList<ResolvedModelPart>> partsByLod,
        IReadOnlyList<string> diagnostics)
    {
        InputFile = inputFile;
        GeometryFile = geometryFile;
        WsModelFile = wsModelFile;
        WsModel = wsModel;
        Model = model;
        PartsByLod = partsByLod;
        MaterialsByLod = partsByLod
            .Select(parts => (IReadOnlyList<ResolvedModelMaterial>)new ReadOnlyCollection<ResolvedModelMaterial>(parts.Select(x => x.Material).ToList()))
            .ToList();
        Diagnostics = diagnostics;
    }

    public PackFile InputFile { get; }
    public PackFile GeometryFile { get; }
    public PackFile? WsModelFile { get; }
    public WsModelFile? WsModel { get; }
    public RmvFile Model { get; }
    public IReadOnlyList<IReadOnlyList<ResolvedModelPart>> PartsByLod { get; }
    public IReadOnlyList<IReadOnlyList<ResolvedModelMaterial>> MaterialsByLod { get; }
    public IReadOnlyList<string> Diagnostics { get; }

    public bool UsesWsModel => WsModel != null;

    public IReadOnlyList<ResolvedModelPart> FirstLod => PartsByLod.Count == 0 ? Array.Empty<ResolvedModelPart>() : PartsByLod[0];
}

public sealed class ResolvedModelPart
{
    public ResolvedModelPart(int lodIndex, int partIndex, RmvModel model, ResolvedModelMaterial material)
    {
        LodIndex = lodIndex;
        PartIndex = partIndex;
        Model = model;
        Material = material;
    }

    public int LodIndex { get; }
    public int PartIndex { get; }
    public RmvModel Model { get; }
    public ResolvedModelMaterial Material { get; }
}

public sealed class ResolvedModelMaterial
{
    private ResolvedModelMaterial(
        IRmvMaterial sourceMaterial,
        WsModelMaterialFile? wsModelMaterial,
        string? wsModelMaterialPath,
        IReadOnlyDictionary<TextureType, string> textures,
        bool hasExplicitAlpha,
        bool alpha)
    {
        SourceMaterial = sourceMaterial;
        WsModelMaterial = wsModelMaterial;
        WsModelMaterialPath = wsModelMaterialPath;
        Textures = textures;
        HasExplicitAlpha = hasExplicitAlpha;
        Alpha = alpha;
    }

    public IRmvMaterial SourceMaterial { get; }
    public WsModelMaterialFile? WsModelMaterial { get; }
    public string? WsModelMaterialPath { get; }
    public IReadOnlyDictionary<TextureType, string> Textures { get; }
    public IReadOnlyDictionary<TextureType, string> EffectiveTextures => Textures;
    public IReadOnlyList<WsModelMaterialParam> Parameters => WsModelMaterial?.Parameters ?? (IReadOnlyList<WsModelMaterialParam>)Array.Empty<WsModelMaterialParam>();
    public string ShaderPath => WsModelMaterial?.ShaderPath ?? string.Empty;
    public bool HasExplicitAlpha { get; }
    public bool Alpha { get; }
    public bool UsesWsModelMaterial => WsModelMaterial != null;

    public string? GetTexture(TextureType textureType)
        => Textures.TryGetValue(textureType, out var texturePath) ? texturePath : null;

    public static ResolvedModelMaterial Create(
        IRmvMaterial sourceMaterial,
        WsModelMaterialFile? wsModelMaterial = null,
        string? wsModelMaterialPath = null)
    {
        ArgumentNullException.ThrowIfNull(sourceMaterial);

        var textures = new Dictionary<TextureType, string>();
        foreach (var texture in sourceMaterial.GetAllTextures())
        {
            if (string.IsNullOrWhiteSpace(texture.Path) == false)
                textures[texture.TexureType] = texture.Path;
        }

        if (wsModelMaterial != null)
        {
            foreach (var texture in wsModelMaterial.Textures)
                // Preserve an explicit empty WSModel slot. The renderer treats
                // a present slot as an override, even when it disables a
                // texture; the exporter will omit the empty path later.
                textures[texture.Key] = texture.Value ?? string.Empty;
        }

        var hasRmvAlpha = false;
        var rmvAlpha = false;
        if (sourceMaterial is WeightedMaterial weightedMaterial
            && weightedMaterial.IntParams.TryGet(WeightedParamterIds.IntParams_Alpha_index, out var rmvAlphaValue))
        {
            hasRmvAlpha = true;
            rmvAlpha = rmvAlphaValue == 1;
        }

        return new ResolvedModelMaterial(
            sourceMaterial,
            wsModelMaterial,
            wsModelMaterialPath,
            new ReadOnlyDictionary<TextureType, string>(textures),
            wsModelMaterial != null || hasRmvAlpha,
            wsModelMaterial?.Alpha ?? rmvAlpha);
    }
}
