using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.TestUtility;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Services;

public class VariantMeshCompositionResolverTests
{
    private readonly string _romePack = PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack");

    [Test]
    public void SelectsFirstCandidateThatLoadsSuccessfully()
    {
        var first = PackFile.CreateFromASCII("first.rigid_model_v2", "first");
        var second = PackFile.CreateFromASCII("second.rigid_model_v2", "second");
        var root = PackFile.CreateFromASCII("root.variantmeshdefinition", """
            <VARIANT_MESH>
              <SLOT name="weapon" attach_point="hand">
                <VARIANT_MESH_REFERENCE definition="first.rigid_model_v2" />
                <VARIANT_MESH_REFERENCE definition="second.rigid_model_v2" />
              </SLOT>
            </VARIANT_MESH>
            """);

        var files = new[] { root, first, second };
        var packFileService = CreatePackFileService(files);
        var modelResolver = new Mock<IModelAssetResolver>(MockBehavior.Strict);
        modelResolver
            .Setup(x => x.Resolve(first))
            .Throws(new ModelAssetResolutionException("broken first candidate"));
        modelResolver
            .Setup(x => x.Resolve(second))
            .Returns(CreatePlaceholderAsset(second));

        var result = new VariantMeshCompositionResolver(packFileService.Object, modelResolver.Object).Resolve(root);

        Assert.That(result.HasRenderableContent, Is.True);
        Assert.That(result.Root!.Slots, Has.Count.EqualTo(1));
        Assert.That(result.Root.Slots[0].AttachmentPoint, Is.EqualTo("hand"));
        Assert.That(result.Root.Slots[0].SelectedChild!.ModelAsset!.InputFile, Is.SameAs(second));
        Assert.That(result.Diagnostics.Any(x => x.Contains("broken first candidate", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void DetectsNestedCaseInsensitiveCycleWithDiagnostic()
    {
        var root = PackFile.CreateFromASCII("Models\\Root.variantmeshdefinition", """
            <VARIANT_MESH>
              <SLOT name="child">
                <VARIANT_MESH_REFERENCE definition="models/Child.variantmeshdefinition" />
              </SLOT>
            </VARIANT_MESH>
            """);
        var child = PackFile.CreateFromASCII("Models\\Child.variantmeshdefinition", """
            <VARIANT_MESH>
              <SLOT name="back">
                <VARIANT_MESH_REFERENCE definition="models/root.VARIANTMESHDEFINITION" />
              </SLOT>
            </VARIANT_MESH>
            """);

        var packFileService = CreatePackFileService(root, child);
        var result = new VariantMeshCompositionResolver(packFileService.Object, new Mock<IModelAssetResolver>().Object).Resolve(root);

        Assert.That(result.HasRenderableContent, Is.False);
        Assert.That(result.Diagnostics.Any(x => x.Contains("cycle", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(result.Diagnostics.Any(x => x.Contains("root.variantmeshdefinition", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void ResolvesNestedVmdModelReference()
    {
        var root = PackFile.CreateFromASCII("Models\\Root.variantmeshdefinition", """
            <VARIANT_MESH>
              <SLOT name="child" attach_point="hand">
                <VARIANT_MESH_REFERENCE definition="models/Child.variantmeshdefinition" />
              </SLOT>
            </VARIANT_MESH>
            """);
        var child = PackFile.CreateFromASCII("Models\\Child.variantmeshdefinition", """
            <VARIANT_MESH model="models/Body.rigid_model_v2" />
            """);
        var model = PackFile.CreateFromASCII("Models\\Body.rigid_model_v2", "body");
        var packFileService = CreatePackFileService(root, child, model);
        var modelResolver = new Mock<IModelAssetResolver>(MockBehavior.Strict);
        modelResolver
            .Setup(x => x.Resolve(model))
            .Returns(CreatePlaceholderAsset(model));

        var result = new VariantMeshCompositionResolver(packFileService.Object, modelResolver.Object).Resolve(root);

        Assert.That(result.HasRenderableContent, Is.True);
        var selectedChild = result.Root!.Slots.Single().SelectedChild;
        Assert.That(selectedChild, Is.Not.Null);
        Assert.That(selectedChild!.ResolvedModelReference!.ModelAsset!.InputFile, Is.SameAs(model));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void DoesNotResolveCandidateRelativeToDefinitionFolder()
    {
        var root = PackFile.CreateFromASCII("Models\\Root.variantmeshdefinition", """
            <VARIANT_MESH>
              <SLOT name="child">
                <VARIANT_MESH_REFERENCE definition="Child.rigid_model_v2" />
              </SLOT>
            </VARIANT_MESH>
            """);
        var nestedCandidate = PackFile.CreateFromASCII("Models\\Child.rigid_model_v2", "child");
        var packFileService = CreatePackFileService(root, nestedCandidate);
        var modelResolver = new Mock<IModelAssetResolver>(MockBehavior.Strict);

        var result = new VariantMeshCompositionResolver(packFileService.Object, modelResolver.Object).Resolve(root);

        Assert.That(result.HasRenderableContent, Is.False);
        Assert.That(modelResolver.Invocations, Is.Empty,
            "Root-relative lookup must not reinterpret a missing candidate as relative to the VMD folder.");
        Assert.That(result.Diagnostics.Any(x => x.Contains("Child.rigid_model_v2", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void ResolvesTrackedShieldVariantMeshDefinition()
    {
        var packFileService = PackFileSerivceTestHelper.Create(_romePack);
        var definition = packFileService.FindFile(@"variantmeshes\_variantmodels\man\shield\celtic_oval_patterns.variantmeshdefinition");
        Assert.That(definition, Is.Not.Null);

        var result = new VariantMeshCompositionResolver(packFileService, new ModelAssetResolver(packFileService)).Resolve(definition!);

        Assert.That(result.HasRenderableContent, Is.True);
        Assert.That(result.Root!.Slots, Has.Count.EqualTo(1));
        Assert.That(result.Root.Slots[0].SelectedChild, Is.Not.Null);

        // The tracked slot contains an inline VARIANT_MESH node whose model
        // attribute is represented by ResolvedModelReference.  The exporter
        // intentionally traverses that node before its slot children.
        var selectedChild = result.Root.Slots[0].SelectedChild!;
        Assert.That(selectedChild.ResolvedModelReference, Is.Not.Null);
        Assert.That(selectedChild.ResolvedModelReference!.ModelAsset, Is.Not.Null);
        Assert.That(selectedChild.ResolvedModelReference.ModelAsset!.FirstLod, Is.Not.Empty);
    }

    private static Mock<IPackFileService> CreatePackFileService(params PackFile[] files)
    {
        var byPath = files.ToDictionary(x => Normalize(x.Name), StringComparer.OrdinalIgnoreCase);
        var mock = new Mock<IPackFileService>(MockBehavior.Loose);
        mock.Setup(x => x.FindFile(It.IsAny<string>(), It.IsAny<IPackFileContainer?>()))
            .Returns((string path, IPackFileContainer? _) => byPath.GetValueOrDefault(Normalize(path)));
        mock.Setup(x => x.GetFullPath(It.IsAny<PackFile>(), It.IsAny<IPackFileContainer?>()))
            .Returns((PackFile file, IPackFileContainer? _) => files.First(x => ReferenceEquals(x, file)).Name);
        return mock;
    }

    private static ResolvedModelAsset CreatePlaceholderAsset(PackFile file)
    {
        var model = new RmvModel
        {
            Material = new WeightedMaterial(),
            Mesh = new RmvMesh { VertexList = [], IndexList = [] }
        };
        var header = new RmvFileHeader { Version = RmvVersionEnum.RMV2_V6, LodCount = 1 };
        header.SkeletonName = string.Empty;
        var rmv = new RmvFile
        {
            Header = header,
            ModelList = new[] { new[] { model } },
            LodHeaders = new[] { LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V6, 0, 0, 0) }
        };
        var part = new ResolvedModelPart(0, 0, model, ResolvedModelMaterial.Create(model.Material));
        return new(file, file, null, null, rmv, new[] { new[] { part } }, []);
    }

    private static string Normalize(string path)
        => path.Replace('/', '\\').Trim().ToLowerInvariant();
}
