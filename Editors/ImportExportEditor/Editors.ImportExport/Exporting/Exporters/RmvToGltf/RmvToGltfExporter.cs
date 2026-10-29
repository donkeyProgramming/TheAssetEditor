using System.Windows;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    internal interface IRmvToGltfExporter
    {
        ExportSupportEnum CanExportFile(PackFile file);
        void Export(RmvToGltfExporterSettings settings);
    }

    public class RmvToGltfExporter : IRmvToGltfExporter
    {
        private readonly ILogger _logger = Logging.Create<RmvToGltfExporter>();
        private readonly IGltfSceneSaver _gltfSaver;
        private readonly GltfMeshBuilder _gltfMeshBuilder;
        private readonly IGltfTextureHandler _gltfTextureHandler;
        private readonly GltfSkeletonBuilder _gltfSkeletonBuilder;
        private readonly GltfAnimationBuilder _gltfAnimationBuilder;
        private readonly ISkeletonAnimationLookUpHelper _skeletonLookUpHelper;
        private readonly IModelAssetResolver _modelAssetResolver;
        private readonly IVariantMeshCompositionResolver? _variantMeshResolver;

        // Keep the pre-composition constructor signature intact. The
        // composition resolver is an additive dependency for VMD exports.
        public RmvToGltfExporter(
            IGltfSceneSaver gltfSaver,
            GltfMeshBuilder gltfMeshBuilder,
            IGltfTextureHandler gltfTextureHandler,
            GltfSkeletonBuilder gltfSkeletonsBuilder,
            GltfAnimationBuilder gltfAnimationCreator,
            ISkeletonAnimationLookUpHelper skeletonLookUpHelper,
            IModelAssetResolver? modelAssetResolver = null)
            : this(
                gltfSaver,
                gltfMeshBuilder,
                gltfTextureHandler,
                gltfSkeletonsBuilder,
                gltfAnimationCreator,
                skeletonLookUpHelper,
                modelAssetResolver,
                null)
        {
        }

        public RmvToGltfExporter(
            IGltfSceneSaver gltfSaver,
            GltfMeshBuilder gltfMeshBuilder,
            IGltfTextureHandler gltfTextureHandler,
            GltfSkeletonBuilder gltfSkeletonsBuilder,
            GltfAnimationBuilder gltfAnimationCreator,
            ISkeletonAnimationLookUpHelper skeletonLookUpHelper,
            IModelAssetResolver? modelAssetResolver,
            IVariantMeshCompositionResolver? variantMeshResolver)
        {
            _gltfSaver = gltfSaver;
            _gltfMeshBuilder = gltfMeshBuilder;
            _gltfTextureHandler = gltfTextureHandler;
            _gltfSkeletonBuilder = gltfSkeletonsBuilder;
            _gltfAnimationBuilder = gltfAnimationCreator;
            _skeletonLookUpHelper = skeletonLookUpHelper;
            _modelAssetResolver = modelAssetResolver ?? new ModelAssetResolver();
            _variantMeshResolver = variantMeshResolver;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name)
                || FileExtensionHelper.IsWsModelFile(file.Name)
                || IsVariantMeshDefinition(file))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        ExportSupportEnum IRmvToGltfExporter.CanExportFile(PackFile file) => CanExportFile(file);

        void IRmvToGltfExporter.Export(RmvToGltfExporterSettings settings) => Export(settings);

        public void Export(RmvToGltfExporterSettings settings)
        {
            LogSettings(settings);

            if (IsVariantMeshDefinition(settings.InputModelFile))
            {
                ExportVariantMesh(settings);
                return;
            }

            var resolvedAsset = _modelAssetResolver.Resolve(settings.InputModelFile);
            foreach (var diagnostic in resolvedAsset.Diagnostics)
                _logger.Here().Warning(diagnostic);

            var outputScene = ModelRoot.CreateModel();
            var modelPart = new ExportModelPart(resolvedAsset, string.Empty, "model", true, true);
            // Keep the established direct RMV/WS behavior: those exports only
            // request a skeleton when animation export is enabled. VMD
            // composition intentionally decouples these choices below so slot
            // attachments can still use a shared skeleton without clips.
            ProcessedGltfSkeleton? skeleton = null;
            global::Shared.GameFormats.Animation.AnimationFile? skeletonFile = null;
            if (settings.ExportAnimations)
            {
                skeleton = CreateSharedSkeleton(
                    [modelPart],
                    settings,
                    outputScene,
                    warnWhenMissing: true,
                    out skeletonFile,
                    out var exportCancelled);
                // Preserve the direct exporter behavior: choosing No in the
                // missing-skeleton warning aborts without saving an output.
                if (exportCancelled)
                    return;
            }
            if (skeleton != null && skeletonFile != null && settings.ExportAnimations)
                _gltfAnimationBuilder.Build(skeletonFile, settings, skeleton, outputScene);
            var textureSession = new GltfTextureExportSession(collisionSafe: false);
            var textures = _gltfTextureHandler.HandleTextures(resolvedAsset, settings, textureSession);
            var meshes = BuildMeshes(modelPart, textures, settings, skeleton != null);

            BuildGltfScene(
                meshes,
                skeleton,
                settings,
                outputScene,
                textures.Select(x => x.SystemFilePath).ToArray());
        }

        private void ExportVariantMesh(RmvToGltfExporterSettings settings)
        {
            if (_variantMeshResolver == null)
                throw new InvalidOperationException("VariantMeshDefinition export requires the variant mesh composition resolver.");

            var composition = _variantMeshResolver.Resolve(settings.InputModelFile);
            foreach (var diagnostic in composition.Diagnostics)
                _logger.Here().Warning(diagnostic);

            if (!composition.HasRenderableContent || composition.Root == null)
            {
                var details = composition.Diagnostics.Count == 0
                    ? "No renderable model candidates were found."
                    : string.Join(Environment.NewLine, composition.Diagnostics);
                throw new InvalidOperationException($"Unable to resolve VariantMeshDefinition '{settings.InputModelFile.Name}'. {details}");
            }

            var modelParts = FlattenModelParts(composition.Root);
            if (modelParts.Count == 0)
                throw new InvalidOperationException($"VariantMeshDefinition '{settings.InputModelFile.Name}' contains no renderable models.");

            var outputScene = ModelRoot.CreateModel();
            var skeleton = CreateSharedSkeleton(
                modelParts,
                settings,
                outputScene,
                warnWhenMissing: false,
                out var skeletonFile,
                out _);
            modelParts = ApplySharedSkeletonCompatibility(modelParts, skeleton);
            var textureSession = new GltfTextureExportSession(collisionSafe: true);
            var meshes = new List<ExportedMesh>();
            var generatedTexturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var modelPart in modelParts)
            {
                var textures = _gltfTextureHandler.HandleTextures(modelPart.Asset, settings, textureSession);
                generatedTexturePaths.UnionWith(textures.Select(x => x.SystemFilePath));
                meshes.AddRange(BuildMeshes(modelPart, textures, settings, skeleton != null && modelPart.UseSharedSkeleton));
            }

            // Skeleton creation and animation export are intentionally separate:
            // attachments and skinning remain useful when animation export is
            // disabled, while selected animations are still emitted exactly once.
            if (skeleton != null && skeletonFile != null && settings.ExportAnimations)
                _gltfAnimationBuilder.Build(skeletonFile, settings, skeleton, outputScene);

            _logger.Here().Information($"VMD Export - Parts={modelParts.Count} MeshCount={meshes.Count} Skeleton={skeleton?.Data.Count}");
            BuildGltfScene(meshes, skeleton, settings, outputScene, generatedTexturePaths);
        }

        private ProcessedGltfSkeleton? CreateSharedSkeleton(
            IReadOnlyList<ExportModelPart> modelParts,
            RmvToGltfExporterSettings settings,
            ModelRoot outputScene,
            bool warnWhenMissing,
            out global::Shared.GameFormats.Animation.AnimationFile? skeletonFile,
            out bool exportCancelled)
        {
            skeletonFile = null;
            exportCancelled = false;
            var skeletonDiagnostics = new List<string>();
            var skeletonName = GltfAnimationCatalogResolver.SelectSharedSkeletonName(
                modelParts.Select(x => x.Asset),
                skeletonDiagnostics);
            if (string.IsNullOrWhiteSpace(skeletonName))
                return null;

            foreach (var diagnostic in skeletonDiagnostics)
                _logger.Here().Warning(diagnostic);

            skeletonFile = _skeletonLookUpHelper.GetSkeletonFileFromName(skeletonName);
            if (skeletonFile == null)
            {
                var message = $"Skeleton '{skeletonName}' was not found; exporting without a glTF skeleton.";
                _logger.Here().Warning(message);
                if (warnWhenMissing && settings.ExportAnimations
                    && MessageBox.Show(
                        "Skeleton file not found, \n(Have you loaded all CA pakcs for the right game?)\n Do you want to continue exporting without skeleton/animations?",
                        "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    exportCancelled = true;
                    return null;
                }
                return null;
            }

            var gltfSkeleton = _gltfSkeletonBuilder.CreateSkeleton(skeletonFile, outputScene, settings);
            return gltfSkeleton;
        }

        private List<ExportModelPart> FlattenModelParts(ResolvedVariantMeshNode node)
        {
            var output = new List<ExportModelPart>();
            var nextIndex = 0;
            foreach (var component in GltfAnimationCatalogResolver.EnumerateComponents(node))
            {
                output.Add(new ExportModelPart(
                    component.Asset,
                    component.AttachmentPoint,
                    $"vmd_part_{nextIndex++:D3}",
                    false,
                    true));
            }
            return output;
        }

        private List<ExportedMesh> BuildMeshes(
            ExportModelPart modelPart,
            List<TextureResult> textures,
            RmvToGltfExporterSettings settings,
            bool willHaveSkeleton)
        {
            var output = new List<ExportedMesh>();
            var meshBuilders = _gltfMeshBuilder.Build(
                modelPart.Asset,
                textures,
                settings,
                willHaveSkeleton,
                modelPart.NamePrefix == "model" ? null : modelPart.NamePrefix);

            for (var i = 0; i < meshBuilders.Count; i++)
            {
                var matrixIndex = -1;
                if (i < modelPart.Asset.FirstLod.Count
                    && modelPart.Asset.FirstLod[i].Material.SourceMaterial is WeightedMaterial weightedMaterial)
                    matrixIndex = weightedMaterial.MatrixIndex;

                var hasWeights = i < modelPart.Asset.FirstLod.Count
                    && modelPart.Asset.FirstLod[i].Model.Mesh.VertexList.Any(x => x.WeightCount > 0);
                var pivotPoint = i < modelPart.Asset.FirstLod.Count
                    ? VecConv.GetSys(GlobalSceneTransforms.FlipVector(
                        modelPart.Asset.FirstLod[i].Material.SourceMaterial.PivotPoint,
                        settings.MirrorMesh))
                    : System.Numerics.Vector3.Zero;
                output.Add(new ExportedMesh(
                    meshBuilders[i],
                    modelPart.AttachmentPoint,
                    matrixIndex,
                    hasWeights,
                    willHaveSkeleton,
                    modelPart.AllowMatrixAttachment,
                    pivotPoint));
            }

            return output;
        }

        internal void BuildGltfScene(
            List<ExportedMesh> meshes,
            ProcessedGltfSkeleton? gltfSkeleton,
            RmvToGltfExporterSettings settings,
            ModelRoot outputScene,
            IReadOnlyCollection<string>? generatedTexturePaths = null)
        {
            var scene = outputScene.UseScene("default");
            foreach (var exportedMesh in meshes)
            {
                var mesh = outputScene.CreateMesh(exportedMesh.MeshBuilder);
                Node? parent = null;

                if (gltfSkeleton != null)
                {
                    var attachmentBone = FindAttachmentBone(
                        gltfSkeleton,
                        exportedMesh.AttachmentPoint,
                        exportedMesh.MatrixIndex,
                        exportedMesh.AllowMatrixAttachment);
                    if (attachmentBone != null)
                        parent = attachmentBone;
                }

                var node = parent?.CreateNode(mesh.Name) ?? scene.CreateNode(mesh.Name);
                // Rigid attachment is the renderer's precedence rule: an
                // attachment resolver plus a valid RMV matrix override means
                // the mesh follows that bone as a rigid object, so its vertex
                // weights must not apply the same skeleton a second time.
                var followsBoneRigidly = parent != null && exportedMesh.MatrixIndex >= 0;
                node.WithLocalTranslation(exportedMesh.PivotPoint);
                if (gltfSkeleton != null
                    && exportedMesh.CanUseSkeleton
                    && exportedMesh.HasWeights
                    && !followsBoneRigidly)
                    node.WithSkinnedMesh(mesh, gltfSkeleton.Data.ToArray());
                else
                    node.WithMesh(mesh);
            }

            _gltfSaver.Save(outputScene, settings.OutputPath, generatedTexturePaths ?? Array.Empty<string>());
        }

        internal static Node? FindAttachmentBone(
            ProcessedGltfSkeleton skeleton,
            string attachmentPoint,
            int matrixIndex,
            bool allowMatrixIndex = true)
        {
            if (!string.IsNullOrWhiteSpace(attachmentPoint))
            {
                // Match SceneObjectEditor.WireAttachmentResolvers: a named
                // attachment is authoritative.  An unknown name does not
                // fall back to the RMV matrix index.
                return skeleton.Data
                    .Select(x => x.Item1)
                    .FirstOrDefault(x => string.Equals(x.Name, attachmentPoint, StringComparison.OrdinalIgnoreCase));
            }

            if (allowMatrixIndex && matrixIndex >= 0 && matrixIndex < skeleton.Data.Count)
                return skeleton.Data[matrixIndex].Item1;

            return null;
        }

        private List<ExportModelPart> ApplySharedSkeletonCompatibility(
            List<ExportModelPart> modelParts,
            ProcessedGltfSkeleton? skeleton)
        {
            if (skeleton == null)
                return modelParts;

            var sharedSkeletonName = GltfAnimationCatalogResolver.SelectSharedSkeletonName(
                modelParts.Select(x => x.Asset));
            if (string.IsNullOrWhiteSpace(sharedSkeletonName))
                return modelParts;

            return modelParts.Select(modelPart =>
            {
                var componentSkeletonName = modelPart.Asset.Model.Header.SkeletonName;
                var canUseSharedSkeleton = string.Equals(
                    componentSkeletonName,
                    sharedSkeletonName,
                    StringComparison.OrdinalIgnoreCase);

                if (!canUseSharedSkeleton && string.IsNullOrWhiteSpace(componentSkeletonName) == false)
                {
                    _logger.Here().Warning(
                        $"VMD component '{modelPart.Asset.InputFile.Name}' uses skeleton '{componentSkeletonName}', "
                        + $"which differs from shared skeleton '{sharedSkeletonName}'; exporting it without skinning.");
                }

                return modelPart with
                {
                    UseSharedSkeleton = canUseSharedSkeleton,
                    // A component with a different named skeleton cannot
                    // safely interpret its own MatrixIndex in the shared
                    // skeleton. Named VMD attachments remain resolvable by
                    // name; pure MatrixIndex attachments stay unparented.
                    AllowMatrixAttachment = canUseSharedSkeleton
                        || string.IsNullOrWhiteSpace(modelPart.AttachmentPoint) == false
                };
            }).ToList();
        }

        private static bool IsVariantMeshDefinition(PackFile file)
            => file.Name.EndsWith(".variantmeshdefinition", StringComparison.OrdinalIgnoreCase);

        private void LogSettings(RmvToGltfExporterSettings settings)
        {
            var str = $"Exporting using {nameof(RmvToGltfExporter)}\n";
            str += $"\tInputModelFile:{settings.InputModelFile?.Name}\n";
            str += $"\tInputAnimationFiles:{settings.InputAnimationFiles?.Count()}\n";
            str += $"\tOutputPath:{settings.OutputPath}\n";
            str += $"\tConvertMaterialTextureToBlender:{settings.ConvertMaterialTextureToBlender}\n";
            str += $"\tConvertNormalTextureToBlue:{settings.ConvertNormalTextureToBlue}\n";
            str += $"\tExportAnimations:{settings.ExportAnimations}\n";
            str += $"\tMirrorMesh:{settings.MirrorMesh}\n";

            _logger.Here().Information(str);
        }

        private sealed record ExportModelPart(
            ResolvedModelAsset Asset,
            string AttachmentPoint,
            string NamePrefix,
            bool UseSharedSkeleton,
            bool AllowMatrixAttachment);

        internal sealed record ExportedMesh(
            IMeshBuilder<MaterialBuilder> MeshBuilder,
            string AttachmentPoint,
            int MatrixIndex,
            bool HasWeights,
            bool CanUseSkeleton,
            bool AllowMatrixAttachment,
            System.Numerics.Vector3 PivotPoint);
    }
}
