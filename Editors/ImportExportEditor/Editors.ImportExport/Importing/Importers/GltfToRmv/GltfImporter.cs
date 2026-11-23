using System.IO;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public class GltfImporter
    {
        private const string Value = "//skeleton//";
        private const string HumanoidSkeletonPath = @"animations\skeletons\humanoid01.anim";
        private readonly IPackFileService _packFileService;
        private readonly ILogger _logger = Logging.Create<GltfImporter>();
        private readonly ISkeletonAnimationLookUpHelper _skeletonLookUpHelper;
        private readonly RmvMaterialBuilder _materialBuilder;

        public GltfImporter(IPackFileService packFileSerivce, ISkeletonAnimationLookUpHelper skeletonLookUpHelper, RmvMaterialBuilder materialBuilder)
        {
            _packFileService = packFileSerivce;
            _skeletonLookUpHelper = skeletonLookUpHelper;
            _materialBuilder = materialBuilder;
        }

        public ImportSupportEnum CanImportFile(PackFile file)
        {
            if (FileExtensionHelper.IsGltfFile(file.Name))
                return ImportSupportEnum.HighPriority;

            return ImportSupportEnum.NotSupported;
        }

        public ImportResult Import(GltfImporterSettings settings, IProgress<ImportProgress>? progress = null) =>
            Import(settings, progress, CancellationToken.None);

        /// <summary>
        /// Builds every output in memory first and writes them to the pack in one go,
        /// so a failed or cancelled import leaves the pack untouched.
        /// </summary>
        public ImportResult Import(GltfImporterSettings settings, IProgress<ImportProgress>? progress, CancellationToken cancellationToken)
        {
            try
            {
                return ImportCore(settings, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.Here().Error(exception, $"Failed to import glTF file {settings.InputGltfFile}");
                return ImportResult.Failure(exception);
            }
        }

        private ImportResult ImportCore(GltfImporterSettings settings, IProgress<ImportProgress>? progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ImportProgress("Reading and validating the glTF/GLB file...", Path.GetFileName(settings.InputGltfFile)));
            var modelRoot = CreateModelRoot(settings);
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ImportProgress(
                "Checking the scene, skeleton and import settings...",
                $"{RmvMeshBuilder.GetMeshSources(modelRoot).Count:N0} mesh parts · {modelRoot.LogicalMaterials.Count:N0} materials · {modelRoot.LogicalAnimations.Count:N0} animations"));

            // RMV2 has no true translucency, so BLEND materials can't be converted safely.
            // When any are present, skip all materials and textures but keep importing the rest.
            var blendMaterialNames = settings.ImportMaterials ? RmvMaterialBuilder.GetBlendMaterialNames(modelRoot) : [];
            var importMaterials = settings.ImportMaterials && blendMaterialNames.Count == 0;

            var skeletonData = GetSkeletonData(settings, modelRoot);
            cancellationToken.ThrowIfCancellationRequested();
            var humanoidScale = GetHumanoidScale(settings, skeletonData);
            var scaleFactor = humanoidScale.ScaleFactor;
            var pendingFiles = new List<NewPackFileEntry>();
            MaterialImportSummary? materialSummary = null;

            if (settings.ImportAnimations && modelRoot.LogicalAnimations.Count > 0 && skeletonData.SkeletonAnimFile == null)
                throw new InvalidDataException("Importing glTF animations needs a valid game skeleton marker and the loaded CA pack files.");

            RmvFile? rmv2File = null;
            var meshSummary = new RmvMeshImportSummary([]);
            if (settings.ImportMeshes || importMaterials)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var meshBuildResult = RmvMeshBuilder.BuildWithSummary(settings, modelRoot, skeletonData.SkeletonAnimFile, skeletonData.SkeletonName ?? "", scaleFactor, progress);
                rmv2File = meshBuildResult.File;
                meshSummary = meshBuildResult.Summary;
                if (rmv2File == null)
                    throw new InvalidDataException("The glTF scene has no meshes to import.");

                if (importMaterials)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var materialBuildResult = _materialBuilder.BuildRmvFileMaterials(settings, modelRoot, rmv2File, meshBuildResult.ModelSources, progress);
                    pendingFiles.AddRange(materialBuildResult.Files);
                    materialSummary = materialBuildResult.Summary;
                }
            }

            if (settings.ImportAnimations)
                pendingFiles.AddRange(ImportAnimations(settings, modelRoot, skeletonData.SkeletonAnimFile, skeletonData.SkeletonName ?? "", scaleFactor, progress, cancellationToken));

            if (skeletonData.WasCreated && skeletonData.SkeletonAnimFile != null && (settings.ImportMeshes || settings.ImportAnimations))
            {
                HumanoidScaleCalculator.ScaleTranslations(skeletonData.SkeletonAnimFile, scaleFactor);
                GltfSourceForwardConverter.ConvertAnimation(skeletonData.SkeletonAnimFile, settings.SourceForwardDirection);
                pendingFiles.Add(CreateSkeletonEntry(skeletonData.SkeletonName!, skeletonData.SkeletonAnimFile));
            }

            if (settings.ImportMeshes && rmv2File != null)
                pendingFiles.Add(CreateRmvEntry(settings, GetImportedPackFileName(settings), rmv2File));

            progress?.Report(new ImportProgress("Checking the files to write...", $"{pendingFiles.Count:N0} files"));
            cancellationToken.ThrowIfCancellationRequested();
            var validation = ValidatePendingFiles(pendingFiles);
            if (validation.Errors.Count != 0)
                return ImportResult.Failure(validation.Errors);

            if (validation.Files.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new ImportProgress("Writing to the pack...", $"{validation.Files.Count:N0} files"));
                var conflicts = PackFileDispatcherWriter.AddFilesToPackIfNoConflicts(_packFileService, settings.DestinationPackFileContainer, validation.Files, validation.Paths);
                if (conflicts.Count != 0)
                    return ImportResult.Failure(conflicts.Select(path => $"The pack already contains {path}").ToList());
            }

            progress?.Report(new ImportProgress("Import finished", $"{validation.Files.Count:N0} files", validation.Files.Count, validation.Files.Count));

            var warnings = BuildMeshWarnings(meshSummary).ToList();
            if (blendMaterialNames.Count != 0)
                warnings.Add($"These glTF BLEND (translucent) materials can't be converted safely, so all materials and textures were skipped. The other selected content was still imported: {string.Join(", ", blendMaterialNames)}");

            return ImportResult.Success(validation.Paths, warnings, humanoidScale, materialSummary, BuildSourceForwardSummary(settings.SourceForwardDirection));
        }

        private static IReadOnlyList<NewPackFileEntry> ImportAnimations(
            GltfImporterSettings settings,
            ModelRoot modelRoot,
            AnimationFile? skeletonAnimFile,
            string skeletonName,
            float scaleFactor,
            IProgress<ImportProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (modelRoot.LogicalAnimations.Count == 0)
                return [];
            if (skeletonAnimFile == null)
                throw new InvalidDataException("The glTF has animations, but no game skeleton could be found for them. Import stopped so no broken .anim files are written.");

            var baseFileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            var animationCount = modelRoot.LogicalAnimations.Count;
            var animationEntries = new List<NewPackFileEntry>();
            for (var animationIndex = 0; animationIndex < animationCount; animationIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var animation = modelRoot.LogicalAnimations[animationIndex];
                var animationName = GetSafeAnimationName(animation, animationIndex);
                var status = $"Converting animation {animationIndex + 1:N0}/{animationCount:N0}...";
                var animFile = AnimationBuilder.Build(
                    new AnimationBuilderSettings(
                        modelRoot,
                        skeletonName,
                        settings.AnimationKeysPerSecond,
                        settings.DestinationPackFileContainer,
                        settings.DestinationPackPath,
                        settings.AutoDetectAnimationKeysPerSecond),
                    skeletonAnimFile,
                    animation,
                    (completed, total) => progress?.Report(new ImportProgress(status, $"Animation \"{animationName}\" · frame {completed:N0}/{total:N0}", completed, total)),
                    cancellationToken);
                progress?.Report(new ImportProgress(status, $"Animation \"{animationName}\" · adjusting translations and facing"));
                HumanoidScaleCalculator.ScaleTranslations(animFile, scaleFactor);
                GltfSourceForwardConverter.ConvertAnimation(animFile, settings.SourceForwardDirection);

                var candidateFileName = $"{baseFileName}_{animationName}.anim";
                var animBytes = AnimationFile.ConvertToBytes(animFile);
                var packFileImported = new PackFile(candidateFileName, new MemorySource(animBytes));
                animationEntries.Add(new NewPackFileEntry(settings.DestinationPackPath, packFileImported));
                progress?.Report(new ImportProgress(status, candidateFileName, animationIndex + 1, animationCount));
            }

            return animationEntries;
        }

        private static string GetSafeAnimationName(Animation animation, int animationIndex)
        {
            var name = string.IsNullOrWhiteSpace(animation.Name) ? $"animation_{animationIndex + 1}" : animation.Name;
            // AE's own glTF export names each clip after its .anim file; don't end up with ".anim.anim".
            if (name.Length > ".anim".Length && name.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                name = name[..^".anim".Length];
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidCharacter, '_');

            return name.Replace(' ', '_');
        }

        private static NewPackFileEntry CreateRmvEntry(GltfImporterSettings settings, string importedFileName, RmvFile rmv2File)
        {
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);
            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));
            return new NewPackFileEntry(settings.DestinationPackPath, packFileImported);
        }

        private static SourceForwardImportSummary BuildSourceForwardSummary(GltfSourceForwardDirection sourceForwardDirection) =>
            sourceForwardDirection switch
            {
                GltfSourceForwardDirection.PositiveZ => new("+Z (standard glTF)", "No extra rotation; the game +Z front is kept."),
                GltfSourceForwardDirection.PositiveX => new("+X (Unreal/PSK)", "Rotated the source +X onto the game +Z front."),
                GltfSourceForwardDirection.NegativeX => new("-X", "Rotated the source -X onto the game +Z front."),
                GltfSourceForwardDirection.NegativeZ => new("-Z", "Rotated the source -Z onto the game +Z front."),
                _ => throw new ArgumentOutOfRangeException(nameof(sourceForwardDirection), sourceForwardDirection, null),
            };

        private HumanoidScaleImportSummary GetHumanoidScale(GltfImporterSettings settings, SkeletonData skeletonData)
        {
            if (!settings.AutoScaleHumanoid)
                return new HumanoidScaleImportSummary(false, null, null, 1, "Not applied: automatic humanoid scaling is turned off.");

            if (!skeletonData.IsExternal || skeletonData.SkeletonAnimFile == null)
                return new HumanoidScaleImportSummary(false, null, null, 1, "Not applied: static models and assets with a game skeleton marker keep their size.");

            if (settings.SelectedGame != GameTypeEnum.Warhammer3)
                throw new InvalidDataException("Automatic humanoid scaling only supports Warhammer 3. Select Warhammer 3, or turn off automatic humanoid scaling.");

            return HumanoidScaleCalculator.Calculate(skeletonData.SkeletonAnimFile, GetCaHumanoidSkeleton);
        }

        private AnimationFile GetCaHumanoidSkeleton()
        {
            var caPacks = _packFileService.GetAllPackfileContainers().Where(container => container.IsCaPackFile).Reverse();
            foreach (var container in caPacks)
            {
                var file = container.FindFile(HumanoidSkeletonPath);
                if (file != null)
                    return AnimationFile.Create(file);
            }

            throw new InvalidDataException($"The loaded CA packs don't contain {HumanoidSkeletonPath}, so the game's standard humanoid size can't be measured.");
        }

        private static IReadOnlyList<string> BuildMeshWarnings(RmvMeshImportSummary summary)
        {
            var warnings = new List<string>();
            if (summary.SplitPrimitiveCount > 0)
                warnings.Add($"Split {summary.SplitPrimitiveCount} oversized mesh parts into {summary.GeneratedSplitSegmentCount} RMV2 meshes, to stay within 65,536 vertices and 16-bit indices per mesh.");

            if (summary.TotalAffectedVertices > 0)
            {
                var segments = string.Join("; ", summary.Segments
                    .Where(segment => segment.AffectedVertices > 0)
                    .Select(segment => $"{segment.ModelName}: {segment.AffectedVertices} vertices"));
                warnings.Add($"Skin weights were cut to RMV2's 4-bone limit on {summary.TotalAffectedVertices} vertices ({segments}). The largest weight dropped from one vertex was {summary.MaximumDiscardedWeight:P1}; {summary.VerticesAboveTenPercentDiscarded} vertices lost more than 10%. Only weights stored in the glTF file are counted.");
            }

            AddSegmentWarning(warnings, summary.Segments.Where(segment => segment.RebuiltNormals), "These mesh parts had no normals, so they were rebuilt from the triangles");
            AddSegmentWarning(warnings, summary.Segments.Where(segment => segment.RebuiltTangents), "These mesh parts had no tangents, so they were rebuilt from the geometry and UVs");
            AddSegmentWarning(warnings, summary.Segments.Where(segment => segment.DefaultedTextureCoordinates), "These mesh parts had no UVs, so zero UVs and a stable tangent basis were used");
            AddSegmentWarning(warnings, summary.Segments.Where(segment => segment.IgnoredVertexColors), "These mesh parts have vertex colours, which the RMV2 import doesn't support; they were ignored");
            AddSegmentWarning(warnings, summary.Segments.Where(segment => segment.IgnoredMorphTargets), "These mesh parts have morph targets (shape keys), which the RMV2 import doesn't support; they were ignored");
            return warnings;
        }

        private static void AddSegmentWarning(ICollection<string> warnings, IEnumerable<RmvMeshSegmentImportSummary> segments, string message)
        {
            var names = segments.Select(segment => segment.ModelName).ToList();
            if (names.Count == 0)
                return;

            warnings.Add($"{message}: {string.Join(", ", names)}");
        }

        private static ModelRoot CreateModelRoot(GltfImporterSettings settings)
        {
            try
            {
                ModelRoot.Validate(settings.InputGltfFile);
                return ModelRoot.Load(settings.InputGltfFile);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Can't read the glTF/GLB file. It may be invalid, or a file it references may be missing.", ex);
            }
        }

        private sealed record SkeletonData(string? SkeletonName, AnimationFile? SkeletonAnimFile, bool WasCreated, bool IsExternal);

        private SkeletonData GetSkeletonData(GltfImporterSettings settings, ModelRoot modelRoot)
        {
            var skeletonName = FetchSkeletonIdStringFromScene(modelRoot);

            if (string.IsNullOrWhiteSpace(skeletonName))
            {
                // No //skeleton// marker: a skinned file from outside the game gets a new skeleton built from its own skin.
                if (modelRoot.LogicalSkins.Count > 0)
                {
                    var importedSkeleton = GltfSkeletonImporter.BuildExternal(modelRoot, settings.InputGltfFile, settings.NewSkeletonName, mirrorMesh: true);
                    return new SkeletonData(importedSkeleton.Header.SkeletonName, importedSkeleton, true, true);
                }

                _logger.Here().Information("Skeleton ID not found in scene; importing as a static model.");
                return new SkeletonData("", null, false, false);
            }

            var skeletonAnimFile = _skeletonLookUpHelper.GetSkeletonFileFromName(skeletonName);
            if (skeletonAnimFile != null)
                return new SkeletonData(skeletonName, skeletonAnimFile, false, false);

            if (modelRoot.LogicalSkins.Count > 0)
            {
                var importedSkeleton = GltfSkeletonImporter.Build(modelRoot, skeletonName, mirrorMesh: true);
                return new SkeletonData(skeletonName, importedSkeleton, true, false);
            }

            throw new InvalidDataException($"Can't find the game skeleton \"{skeletonName}\", and the glTF has no skin to build one from. Have you selected the correct game and loaded all CA pack files?");
        }

        private static NewPackFileEntry CreateSkeletonEntry(string skeletonName, AnimationFile skeleton)
        {
            var fileName = Path.GetFileNameWithoutExtension(skeletonName) + ".anim";
            var packFile = new PackFile(fileName, new MemorySource(AnimationFile.ConvertToBytes(skeleton)));
            return new NewPackFileEntry(@"animations\skeletons", packFile);
        }

        private static string GetImportedPackFileName(GltfImporterSettings settings) => Path.GetFileNameWithoutExtension(settings.InputGltfFile) + ".rigid_model_v2";

        private static (List<NewPackFileEntry> Files, IReadOnlyList<string> Paths, IReadOnlyList<string> Errors) ValidatePendingFiles(IReadOnlyList<NewPackFileEntry> pendingFiles)
        {
            var normalizedFiles = pendingFiles
                .Select(entry =>
                {
                    var path = PathNormalization.NormalizeFileName(Path.Combine(PathNormalization.NormalizeDirectoryPath(entry.DirectoyPath), entry.PackFile.Name.Trim()));
                    var file = new PackFile(Path.GetFileName(path), entry.PackFile.DataSource);
                    return new NewPackFileEntry(Path.GetDirectoryName(path) ?? "", file);
                })
                .ToList();
            var paths = normalizedFiles
                .Select(entry => Path.Combine(entry.DirectoyPath, entry.PackFile.Name))
                .ToList();
            var errors = paths
                .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => $"The import would write {path} more than once")
                .ToList();
            return (normalizedFiles, paths, errors);
        }

        private static string FetchSkeletonIdStringFromScene(ModelRoot modelRoot)
        {
            var skeletonMarkers = modelRoot.LogicalNodes
                .Where(node => node.Name?.StartsWith(Value, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            if (skeletonMarkers.Count == 0)
                return "";

            // Strip the prefix itself. TrimStart(chars) would also eat leading name letters
            // from the set "/skeleton", turning e.g. "snake01" into "ake01".
            var skeletonName = skeletonMarkers.Count == 1 ? skeletonMarkers[0].Name[Value.Length..].Trim() : "";
            if (string.IsNullOrWhiteSpace(skeletonName))
                throw new InvalidDataException("The glTF must contain exactly one valid //skeleton// marker node.");

            return skeletonName.ToLowerInvariant();
        }
    }
}
