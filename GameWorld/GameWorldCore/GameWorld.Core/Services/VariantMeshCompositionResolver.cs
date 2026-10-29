using System.Collections.ObjectModel;
using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Vmd;
using static Shared.GameFormats.Vmd.VariantMeshDefinition;

namespace GameWorld.Core.Services;

/// <summary>
/// Resolves a VariantMeshDefinition into the default, renderable composition
/// without creating scene nodes.  The viewport still owns its scene-node
/// layout; exporters and other consumers can use this representation when
/// they need the same asset-selection rules without depending on MonoGame.
/// </summary>
public interface IVariantMeshCompositionResolver
{
    ResolvedVariantMeshComposition Resolve(PackFile inputFile);
}

public sealed class VariantMeshCompositionResolver : IVariantMeshCompositionResolver
{
    private readonly IPackFileService _packFileService;
    private readonly IModelAssetResolver _modelAssetResolver;

    public VariantMeshCompositionResolver(
        IPackFileService packFileService,
        IModelAssetResolver? modelAssetResolver = null)
    {
        _packFileService = packFileService;
        _modelAssetResolver = modelAssetResolver ?? new ModelAssetResolver(packFileService);
    }

    public ResolvedVariantMeshComposition Resolve(PackFile inputFile)
    {
        ArgumentNullException.ThrowIfNull(inputFile);

        var diagnostics = new List<string>();
        var activeDefinitions = new List<string>();
        var root = ResolveDefinition(inputFile, diagnostics, activeDefinitions, "root");

        return new ResolvedVariantMeshComposition(inputFile, root, diagnostics);
    }

    private ResolvedVariantMeshNode? ResolveDefinition(
        PackFile file,
        List<string> diagnostics,
        List<string> activeDefinitions,
        string context)
    {
        var definitionKey = GetDefinitionKey(file);
        var activeIndex = activeDefinitions.FindIndex(x => string.Equals(x, definitionKey, StringComparison.OrdinalIgnoreCase));
        if (activeIndex >= 0)
        {
            var cycle = activeDefinitions.Skip(activeIndex).Append(definitionKey);
            diagnostics.Add($"VariantMeshDefinition cycle detected while resolving {context}: {string.Join(" -> ", cycle)}.");
            return null;
        }

        activeDefinitions.Add(definitionKey);
        try
        {
            VariantMesh definition;
            try
            {
                definition = VariantMeshDefinitionLoader.Load(file);
            }
            catch (Exception exception)
            {
                diagnostics.Add($"Unable to parse VariantMeshDefinition '{DescribeFile(file)}' while resolving {context}: {exception.Message}");
                return null;
            }

            return ResolveDefinition(
                definition,
                file,
                diagnostics,
                activeDefinitions,
                context,
                definitionKey);
        }
        finally
        {
            activeDefinitions.RemoveAt(activeDefinitions.Count - 1);
        }
    }

    private ResolvedVariantMeshNode ResolveDefinition(
        VariantMesh definition,
        PackFile ownerFile,
        List<string> diagnostics,
        List<string> activeDefinitions,
        string context,
        string definitionKey)
    {
        var node = new ResolvedVariantMeshNode(
            definitionKey,
            ownerFile,
            definition.ModelReference);

        if (string.IsNullOrWhiteSpace(definition.ModelReference) == false)
        {
            node.ResolvedModelReference = ResolveCandidate(
                definition.ModelReference,
                diagnostics,
                activeDefinitions,
                $"model reference in {context}");
        }

        foreach (var slot in definition.ChildSlots ?? [])
        {
            var resolvedSlot = new ResolvedVariantMeshSlot(
                slot.Name ?? string.Empty,
                slot.AttachmentPoint ?? string.Empty);

            // VariantMeshDefinitionLoader preserves these as two collections
            // and the viewport visits inline meshes before references. Keep
            // that order so default selection matches the normal preview.
            foreach (var childMesh in slot.ChildMeshes ?? [])
            {
                var candidate = ResolveCandidate(
                    childMesh,
                    ownerFile,
                    diagnostics,
                    activeDefinitions,
                    $"slot '{resolvedSlot.Name}' in {context}");
                if (candidate?.HasRenderableContent == true)
                {
                    resolvedSlot.SelectedChild = candidate;
                    break;
                }
            }

            if (resolvedSlot.SelectedChild == null)
            {
                foreach (var childReference in slot.ChildReferences ?? [])
                {
                    var candidate = ResolveCandidate(
                        childReference.Reference,
                        diagnostics,
                        activeDefinitions,
                        $"slot '{resolvedSlot.Name}' in {context}");
                    if (candidate?.HasRenderableContent == true)
                    {
                        resolvedSlot.SelectedChild = candidate;
                        break;
                    }
                }
            }

            if (resolvedSlot.SelectedChild == null
                && ((slot.ChildMeshes?.Count ?? 0) > 0 || (slot.ChildReferences?.Count ?? 0) > 0))
            {
                diagnostics.Add($"No candidate could be resolved for slot '{resolvedSlot.Name}' in {context}.");
            }

            node.Slots.Add(resolvedSlot);
        }

        return node;
    }

    private ResolvedVariantMeshNode? ResolveCandidate(
        VariantMesh definition,
        PackFile ownerFile,
        List<string> diagnostics,
        List<string> activeDefinitions,
        string context)
    {
        var node = ResolveInlineDefinition(definition, ownerFile, diagnostics, activeDefinitions, context);
        if (node?.HasRenderableContent != true)
            return null;
        return node;
    }

    private ResolvedVariantMeshNode? ResolveInlineDefinition(
        VariantMesh definition,
        PackFile ownerFile,
        List<string> diagnostics,
        List<string> activeDefinitions,
        string context)
    {
        var node = new ResolvedVariantMeshNode(
            $"{GetDefinitionKey(ownerFile)}::{context}",
            ownerFile,
            definition.ModelReference);

        if (string.IsNullOrWhiteSpace(definition.ModelReference) == false)
        {
            node.ResolvedModelReference = ResolveCandidate(
                definition.ModelReference,
                diagnostics,
                activeDefinitions,
                $"model reference in {context}");
        }

        foreach (var slot in definition.ChildSlots ?? [])
        {
            var resolvedSlot = new ResolvedVariantMeshSlot(
                slot.Name ?? string.Empty,
                slot.AttachmentPoint ?? string.Empty);

            foreach (var childMesh in slot.ChildMeshes ?? [])
            {
                var candidate = ResolveCandidate(childMesh, ownerFile, diagnostics, activeDefinitions, $"slot '{resolvedSlot.Name}' in {context}");
                if (candidate?.HasRenderableContent == true)
                {
                    resolvedSlot.SelectedChild = candidate;
                    break;
                }
            }

            if (resolvedSlot.SelectedChild == null)
            {
                foreach (var childReference in slot.ChildReferences ?? [])
                {
                    var candidate = ResolveCandidate(childReference.Reference, diagnostics, activeDefinitions, $"slot '{resolvedSlot.Name}' in {context}");
                    if (candidate?.HasRenderableContent == true)
                    {
                        resolvedSlot.SelectedChild = candidate;
                        break;
                    }
                }
            }

            if (resolvedSlot.SelectedChild == null
                && ((slot.ChildMeshes?.Count ?? 0) > 0 || (slot.ChildReferences?.Count ?? 0) > 0))
            {
                diagnostics.Add($"No candidate could be resolved for slot '{resolvedSlot.Name}' in {context}.");
            }

            node.Slots.Add(resolvedSlot);
        }

        return node;
    }

    private ResolvedVariantMeshNode? ResolveCandidate(
        string? reference,
        List<string> diagnostics,
        List<string> activeDefinitions,
        string context)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            diagnostics.Add($"Empty candidate in {context}.");
            return null;
        }

        var candidateFile = FindReference(reference);
        if (candidateFile == null)
        {
            diagnostics.Add($"Candidate '{reference}' in {context} was not found; trying the next candidate.");
            return null;
        }

        if (IsVmd(candidateFile))
            return ResolveDefinition(candidateFile, diagnostics, activeDefinitions, context);

        if (!IsModel(candidateFile))
        {
            diagnostics.Add($"Candidate '{DescribeFile(candidateFile)}' in {context} is not an RMV2, WSModel, or VariantMeshDefinition.");
            return null;
        }

        try
        {
            var asset = _modelAssetResolver.Resolve(candidateFile);
            var node = new ResolvedVariantMeshNode(
                GetDefinitionKey(candidateFile),
                candidateFile,
                null)
            {
                ModelAsset = asset
            };
            return node;
        }
        catch (Exception exception)
        {
            diagnostics.Add($"Candidate '{DescribeFile(candidateFile)}' in {context} could not be loaded; trying the next candidate. {exception.Message}");
            return null;
        }
    }

    private PackFile? FindReference(string reference)
    {
        var normalizedReference = NormalizePackPath(reference).ToLowerInvariant();
        // Keep candidate lookup identical to ComplexMeshLoader: VMD paths are
        // pack-root-relative, not relative to the definition's containing
        // folder.  This also makes the default-candidate order deterministic
        // across the viewport and export paths.
        return _packFileService.FindFile(normalizedReference);
    }

    private string GetDefinitionKey(PackFile file)
        => NormalizeCyclePath(TryGetPackPath(file) ?? file.Name);

    private string? TryGetPackPath(PackFile file)
    {
        try
        {
            var fullPath = _packFileService.GetFullPath(file);
            return string.IsNullOrWhiteSpace(fullPath) ? file.Name : fullPath;
        }
        catch
        {
            return file.Name;
        }
    }

    private string DescribeFile(PackFile file)
        => TryGetPackPath(file) ?? file.Name;

    private static string NormalizePackPath(string path)
        => path.Replace('/', '\\').Trim().TrimStart('\\');

    private static string NormalizeCyclePath(string path)
        => NormalizePackPath(path).ToLowerInvariant();

    private static bool IsVmd(PackFile file)
        => file.Name.EndsWith(".variantmeshdefinition", StringComparison.OrdinalIgnoreCase);

    private static bool IsModel(PackFile file)
        => file.Name.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase)
           || file.Name.EndsWith(".wsmodel", StringComparison.OrdinalIgnoreCase);
}

public sealed class ResolvedVariantMeshComposition
{
    public ResolvedVariantMeshComposition(
        PackFile inputFile,
        ResolvedVariantMeshNode? root,
        IReadOnlyList<string> diagnostics)
    {
        InputFile = inputFile;
        Root = root;
        Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToList());
    }

    public PackFile InputFile { get; }
    public ResolvedVariantMeshNode? Root { get; }
    public IReadOnlyList<string> Diagnostics { get; }
    public bool HasRenderableContent => Root?.HasRenderableContent == true;
}

public sealed class ResolvedVariantMeshNode
{
    public ResolvedVariantMeshNode(string key, PackFile sourceFile, string? modelReference)
    {
        Key = key;
        SourceFile = sourceFile;
        ModelReference = modelReference;
    }

    public string Key { get; }
    public PackFile SourceFile { get; }
    public string? ModelReference { get; }
    public ResolvedModelAsset? ModelAsset { get; set; }
    public ResolvedVariantMeshNode? ResolvedModelReference { get; set; }
    public List<ResolvedVariantMeshSlot> Slots { get; } = [];

    public bool HasRenderableContent
        => (ModelAsset?.FirstLod.Count ?? 0) > 0
           || ResolvedModelReference?.HasRenderableContent == true
           || Slots.Any(x => x.SelectedChild?.HasRenderableContent == true);
}

public sealed class ResolvedVariantMeshSlot
{
    public ResolvedVariantMeshSlot(string name, string attachmentPoint)
    {
        Name = name;
        AttachmentPoint = attachmentPoint;
    }

    public string Name { get; }
    public string AttachmentPoint { get; }
    public ResolvedVariantMeshNode? SelectedChild { get; set; }
}
