using System.Collections.ObjectModel;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Presentation;
using Editors.ImportExport.Exporting.Presentation.RmvToGltf;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

public class GltfAnimationCatalogTests
{
    [TestCase("model.rigid_model_v2")]
    [TestCase("model.wsmodel")]
    public void DirectModelCatalogUsesResolvedSkeletonAndPreservesAnimationContainers(string fileName)
    {
        var input = PackFile.CreateFromASCII(fileName, "model");
        var asset = CreateAsset(input, "direct_skeleton");
        var modelResolver = new Mock<IModelAssetResolver>();
        modelResolver.Setup(x => x.Resolve(input)).Returns(asset);
        var lookup = CreateLookup("direct_skeleton", out var animationReference);

        var resolver = new GltfAnimationCatalogResolver(
            modelResolver.Object,
            new Mock<IVariantMeshCompositionResolver>().Object,
            lookup.Object);

        var catalog = resolver.Resolve(input);

        Assert.That(catalog.SkeletonName, Is.EqualTo("direct_skeleton"));
        Assert.That(catalog.HasSkeletonFile, Is.True);
        Assert.That(catalog.Animations, Has.Count.EqualTo(1));
        Assert.That(catalog.Animations[0], Is.SameAs(animationReference));
        Assert.That(catalog.Animations[0].Container, Is.SameAs(animationReference.Container));
    }

    [Test]
    public void VmdCatalogUsesExporterOrderAndReportsDifferentComponentSkeletons()
    {
        var input = PackFile.CreateFromASCII("root.variantmeshdefinition", "root");
        var root = new ResolvedVariantMeshNode("root", input, null)
        {
            ResolvedModelReference = new ResolvedVariantMeshNode(
                "nested",
                PackFile.CreateFromASCII("nested.variantmeshdefinition", "nested"),
                null)
            {
                ModelAsset = CreateAsset(
                    PackFile.CreateFromASCII("nested.rigid_model_v2", "nested"),
                    "nested_skeleton")
            }
        };
        root.Slots.Add(new ResolvedVariantMeshSlot("fallback", "hand_joint")
        {
            SelectedChild = new ResolvedVariantMeshNode(
                "component",
                PackFile.CreateFromASCII("component.rigid_model_v2", "component"),
                null)
            {
                ModelAsset = CreateAsset(
                    PackFile.CreateFromASCII("component.rigid_model_v2", "component"),
                    "other_skeleton")
            }
        });

        var composition = new ResolvedVariantMeshComposition(input, root, []);
        var compositionResolver = new Mock<IVariantMeshCompositionResolver>();
        compositionResolver.Setup(x => x.Resolve(input)).Returns(composition);
        var lookup = CreateLookup("nested_skeleton", out _);

        var resolver = new GltfAnimationCatalogResolver(
            new Mock<IModelAssetResolver>().Object,
            compositionResolver.Object,
            lookup.Object);

        var catalog = resolver.Resolve(input);

        Assert.That(catalog.SkeletonName, Is.EqualTo("nested_skeleton"));
        Assert.That(catalog.HasSkeletonFile, Is.True);
        Assert.That(
            catalog.Diagnostics.Any(x => x.Contains("different skeletons", StringComparison.OrdinalIgnoreCase)),
            Is.True);
    }

    [Test]
    public void ExporterCoreInitializesSupportedExporterBeforeExecute()
    {
        var source = PackFile.CreateFromASCII("model.rigid_model_v2", "model");
        var exporter = new TrackingExporter(ExportSupportEnum.HighPriority);
        var viewModel = new ExporterCoreViewModel([exporter]);

        viewModel.Initialize(source);

        Assert.That(exporter.InitializedSource, Is.SameAs(source));
        Assert.That(viewModel.SelectedExporter, Is.SameAs(exporter));
    }

    [Test]
    public void ExportViewModelPassesOnlySelectedResolvedAnimationFiles()
    {
        var source = PackFile.CreateFromASCII("model.rigid_model_v2", "model");
        var asset = CreateAsset(source, "direct_skeleton");
        var modelResolver = new Mock<IModelAssetResolver>();
        modelResolver.Setup(x => x.Resolve(source)).Returns(asset);

        var container = new Mock<IPackFileContainer>().Object;
        var firstAnimation = PackFile.CreateFromASCII("animations\\first.anim", "first");
        var idleAnimation = PackFile.CreateFromASCII("animations\\stand_idle.anim", "idle");
        var firstReference = new AnimationReference("animations\\first.anim", container);
        var idleReference = new AnimationReference("animations\\stand_idle.anim", container);
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        lookup.Setup(x => x.GetSkeletonFileFromName("direct_skeleton")).Returns(new Shared.GameFormats.Animation.AnimationFile());
        lookup.Setup(x => x.GetAnimationsForSkeleton("direct_skeleton"))
            .Returns(new ObservableCollection<AnimationReference> { firstReference, idleReference });

        var packFileService = new Mock<IPackFileService>();
        packFileService
            .Setup(x => x.FindFile(firstReference.AnimationFile, container))
            .Returns(firstAnimation);
        packFileService
            .Setup(x => x.FindFile(idleReference.AnimationFile, container))
            .Returns(idleAnimation);

        var exportService = new CapturingExporter();
        var catalogResolver = new GltfAnimationCatalogResolver(
            modelResolver.Object,
            new Mock<IVariantMeshCompositionResolver>().Object,
            lookup.Object);
        var viewModel = new RmvToGltfExporterViewModel(
            exportService,
            packFileService.Object,
            catalogResolver);

        viewModel.Initialize(source);
        Assert.That(viewModel.SelectedAnimationCount, Is.EqualTo(1));
        viewModel.AnimationOptions[0].IsSelected = true;
        viewModel.AnimationOptions[1].IsSelected = false;
        viewModel.ExportAnimations = true;
        viewModel.Execute(source, "output.gltf", false);

        Assert.That(exportService.Settings, Is.Not.Null);
        Assert.That(exportService.Settings!.InputAnimationFiles, Is.EqualTo(new[] { firstAnimation }));

        viewModel.ExportAnimations = false;
        viewModel.Execute(source, "output-no-animation.gltf", false);
        Assert.That(exportService.Settings!.InputAnimationFiles, Is.Empty);
    }

    private static Mock<ISkeletonAnimationLookUpHelper> CreateLookup(
        string skeletonName,
        out AnimationReference animationReference)
    {
        var container = new Mock<IPackFileContainer>().Object;
        animationReference = new AnimationReference($"animations\\{skeletonName}\\stand_idle.anim", container);
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        lookup.Setup(x => x.GetSkeletonFileFromName(skeletonName))
            .Returns(new Shared.GameFormats.Animation.AnimationFile());
        lookup.Setup(x => x.GetAnimationsForSkeleton(skeletonName))
            .Returns(new ObservableCollection<AnimationReference> { animationReference });
        return lookup;
    }

    private static ResolvedModelAsset CreateAsset(PackFile input, string skeletonName)
    {
        var material = new WeightedMaterial { ModelName = Path.GetFileNameWithoutExtension(input.Name) };
        var model = new RmvModel { Material = material };
        var rmv = new RmvFile
        {
            Header = new RmvFileHeader { SkeletonName = skeletonName, LodCount = 1 },
            ModelList = [new[] { model }],
            LodHeaders = []
        };
        var part = new ResolvedModelPart(0, 0, model, ResolvedModelMaterial.Create(material));
        return new ResolvedModelAsset(input, input, null, null, rmv, [new[] { part }], []);
    }

    private sealed class TrackingExporter : IExporterViewModel
    {
        private readonly ExportSupportEnum _support;

        public TrackingExporter(ExportSupportEnum support)
        {
            _support = support;
        }

        public string DisplayName => "Tracking";
        public string OutputExtension => ".test";
        public PackFile? InitializedSource { get; private set; }

        public ExportSupportEnum CanExportFile(PackFile file) => _support;
        public void Initialize(PackFile exportSource) => InitializedSource = exportSource;
        public void Execute(PackFile exportSource, string outputPath, bool generateImporter) { }
    }

    private sealed class CapturingExporter : IRmvToGltfExporter
    {
        public RmvToGltfExporterSettings? Settings { get; private set; }

        public ExportSupportEnum CanExportFile(PackFile file) => ExportSupportEnum.HighPriority;

        public void Export(RmvToGltfExporterSettings settings) => Settings = settings;
    }
}
