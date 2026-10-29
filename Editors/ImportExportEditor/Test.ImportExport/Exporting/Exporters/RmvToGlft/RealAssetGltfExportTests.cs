using System.Drawing;
using System.IO;
using Editors.ImportExport;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using MeshImportExport;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel.Types;
using Shared.TestUtility;
using SharpGLTF.Schema2;
using Test.TestingUtility.TestUtility;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

/// <summary>
/// End-to-end coverage against the small tracked packs used by the exporter
/// tests. These tests intentionally use the production pack loader, DDS
/// converters, composition resolver, skeleton lookup, animation builder, and
/// scene saver instead of texture/saver mocks.
/// </summary>
public class RealAssetGltfExportTests
{
    private const string KarlWsModelPath =
        @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";

    private const string ThrotVariantMeshPath =
        @"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition";

    private const string ThrotAnimationPath =
        @"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anim";

    [Test]
    [CancelAfter(180_000)]
    public void RealKarlWsModelExportsReloadableGlbWithCorrectBaseColourAndMaskCleanup()
    {
        var packFileService = PackFileSerivceTestHelper.CreateFromPackFile(
            PathHelper.GetDataFile("Karl_and_celestialgeneral.pack"));
        var input = packFileService.FindFile(KarlWsModelPath);
        Assert.That(input, Is.Not.Null, "The tracked Karl WSModel was not found in the pack.");

        var asset = new ModelAssetResolver(packFileService).Resolve(input!);
        var texturedPart = asset.FirstLod.FirstOrDefault(part =>
            part.Material.GetTexture(TextureType.BaseColour) != null
            && part.Material.GetTexture(TextureType.Mask) != null);
        Assert.That(texturedPart, Is.Not.Null, "The tracked Karl WSModel has no BaseColour + Mask part.");

        var baseColourPath = texturedPart!.Material.GetTexture(TextureType.BaseColour)!;
        var maskPath = texturedPart.Material.GetTexture(TextureType.Mask)!;
        var expectedBaseColour = DecodePng(DecodeDds(packFileService, baseColourPath));
        var expectedMask = DecodePng(DecodeDds(packFileService, maskPath));
        var expectedInvertedMask = InvertRgb(expectedMask);
        Assert.That(ImagesEquivalent(expectedBaseColour, expectedMask), Is.False,
            "The selected tracked base-colour and mask fixtures must differ for this regression test.");
        Assert.That(ImagesEquivalent(expectedBaseColour, expectedInvertedMask), Is.False,
            "The selected tracked base-colour and converted auxiliary mask fixtures must differ for this regression test.");

        // BaseColor is emitted for every effective BaseColour/Diffuse texture,
        // while masks are exported as auxiliary PNGs and inverted in RGB.
        // Keep the complete expected sets so a bad material binding cannot be
        // hidden by another part's correctly exported texture.
        var expectedBaseColours = asset.FirstLod
            .SelectMany(part => new[]
            {
                part.Material.GetTexture(TextureType.BaseColour),
                part.Material.GetTexture(TextureType.Diffuse)
            })
            .Where(path => string.IsNullOrWhiteSpace(path) == false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => DecodePng(DecodeDds(packFileService, path!)))
            .ToList();
        var expectedInvertedMasks = asset.FirstLod
            .Select(part => part.Material.GetTexture(TextureType.Mask))
            .Where(path => string.IsNullOrWhiteSpace(path) == false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => InvertRgb(DecodePng(DecodeDds(packFileService, path!))))
            .ToList();
        Assert.That(expectedBaseColours, Is.Not.Empty, "The tracked WSModel has no expected BaseColor textures.");
        Assert.That(expectedInvertedMasks, Is.Not.Empty, "The tracked WSModel has no expected auxiliary mask textures.");

        var outputDirectory = CreateTempDirectory("asset-editor-karl-glb");
        var outputPath = Path.Combine(outputDirectory, "karl.wsmodel.glb");
        var expectedMaskOutputPaths = asset.FirstLod
            .Select(part => part.Material.GetTexture(TextureType.Mask))
            .Where(path => string.IsNullOrWhiteSpace(path) == false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => GetExpectedMaskOutputPath(outputDirectory, path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.That(expectedMaskOutputPaths, Is.Not.Empty);
        var dialogs = new Mock<IStandardDialogs>();
        var lookup = new Mock<ISkeletonAnimationLookUpHelper>();
        var exporter = CreateProductionExporter(packFileService, dialogs.Object, lookup.Object);

        try
        {
            exporter.Export(new RmvToGltfExporterSettings(
                input!,
                [],
                outputPath,
                ExportMaterials: true,
                ConvertMaterialTextureToBlender: false,
                ConvertNormalTextureToBlue: false,
                ExportAnimations: false,
                MirrorMesh: true));

            Assert.That(File.Exists(outputPath), Is.True);
            ModelRoot.Validate(outputPath);
            var model = ModelRoot.Load(outputPath);

            var baseColourImages = model.LogicalMaterials
                .Select(material => material.FindChannel("BaseColor")?.Texture?.PrimaryImage)
                .Where(image => image != null)
                .Select(image => DecodePng(ReadMemoryImage(image!.Content)))
                .ToList();

            Assert.That(baseColourImages, Is.Not.Empty, "The exported glTF has no BaseColor channel.");
            Assert.That(baseColourImages.All(image => expectedBaseColours.Any(expected => ImagesEquivalent(image, expected))), Is.True,
                "Every embedded BaseColor image must match one of the production DDS conversions.");
            Assert.That(baseColourImages.Any(image => ImagesEquivalent(image, expectedBaseColour)), Is.True,
                "The selected tracked part's BaseColor was not embedded in the GLB.");
            Assert.That(baseColourImages.Any(image => expectedInvertedMasks.Any(mask => ImagesEquivalent(image, mask))), Is.False,
                "An inverted auxiliary mask must never replace a BaseColor image.");

            // GltfSceneSaver removes only exact generated TextureResult paths
            // after a successful GLB save. DoTextureMask derives a
            // `<source-stem>_mask.png` path but does not return it as a
            // TextureResult, so those exact auxiliary paths remain.
            var remainingPngs = Directory.GetFiles(outputDirectory, "*.png");
            Assert.That(remainingPngs, Is.EquivalentTo(expectedMaskOutputPaths),
                "Only the exact auxiliary mask paths produced by GltfTextureHandler should remain after a successful GLB save.");
            dialogs.Verify(x => x.ShowExceptionWindow(It.IsAny<Exception>()), Times.Never);
        }
        finally
        {
            DeleteTempDirectory(outputDirectory);
        }
    }

    [Test]
    [CancelAfter(240_000)]
    public void RealThrotVariantMeshExportsOneAnimatedSharedSkeletonWithAttachmentHierarchy()
    {
        var packFileService = PackFileSerivceTestHelper.CreateFromPackFile(
            PathHelper.GetDataFile("Throt.pack"));
        var input = packFileService.FindFile(ThrotVariantMeshPath);
        var animation = packFileService.FindFile(ThrotAnimationPath);
        Assert.That(input, Is.Not.Null, "The tracked Throt VariantMeshDefinition was not found in the pack.");
        Assert.That(animation, Is.Not.Null, "The deterministic tracked humanoid17 animation was not found in the pack.");

        var modelResolver = new ModelAssetResolver(packFileService);
        var composition = new VariantMeshCompositionResolver(packFileService, modelResolver).Resolve(input!);
        Assert.That(composition.Root, Is.Not.Null);
        Assert.That(composition.HasRenderableContent, Is.True);

        var components = GltfAnimationCatalogResolver.EnumerateComponents(composition.Root!).ToList();
        var expectedComponents = components
            .Select((component, index) => new
            {
                Component = component,
                MeshNamePrefix = $"vmd_part_{index:D3}_"
            })
            .ToList();
        var expectedMeshCount = expectedComponents.Sum(component => component.Component.Asset.FirstLod.Count);
        var expectedAttachmentCount = expectedComponents.Count(component =>
            string.IsNullOrWhiteSpace(component.Component.AttachmentPoint) == false);
        Assert.That(expectedMeshCount, Is.GreaterThan(1), "The tracked Throt composition should contain multiple model parts.");
        Assert.That(expectedAttachmentCount, Is.GreaterThan(0),
            "The tracked Throt composition should exercise an attachment point.");

        var outputDirectory = CreateTempDirectory("asset-editor-throt-glb");
        var outputPath = Path.Combine(outputDirectory, "skv_throt.glb");
        var dialogs = new Mock<IStandardDialogs>();
        var eventHub = new Mock<IGlobalEventHub>();
        var skeletonLookup = new SkeletonAnimationLookUpHelper(packFileService, eventHub.Object);
        var exporter = CreateProductionExporter(packFileService, dialogs.Object, skeletonLookup, modelResolver);

        try
        {
            exporter.Export(new RmvToGltfExporterSettings(
                input!,
                [animation!],
                outputPath,
                ExportMaterials: false,
                ConvertMaterialTextureToBlender: false,
                ConvertNormalTextureToBlue: false,
                ExportAnimations: true,
                MirrorMesh: true));

            Assert.That(File.Exists(outputPath), Is.True);
            ModelRoot.Validate(outputPath);
            var model = ModelRoot.Load(outputPath);

            var componentNodes = model.LogicalNodes
                .Where(node => node.Mesh != null
                    && node.Name.StartsWith("vmd_part_", StringComparison.Ordinal))
                .ToList();
            Assert.That(componentNodes, Has.Count.EqualTo(expectedMeshCount));
            foreach (var expectedComponent in expectedComponents)
            {
                var nodesForComponent = componentNodes
                    .Where(node => node.Name.StartsWith(expectedComponent.MeshNamePrefix, StringComparison.Ordinal))
                    .ToList();
                Assert.That(nodesForComponent, Has.Count.EqualTo(expectedComponent.Component.Asset.FirstLod.Count),
                    $"The component '{expectedComponent.Component.Asset.InputFile.Name}' did not retain its generated {expectedComponent.MeshNamePrefix} mesh prefix.");
            }
            var skinnedNodes = componentNodes.Where(node => node.Skin != null).ToList();
            Assert.That(skinnedNodes, Is.Not.Empty);

            Assert.That(model.LogicalAnimations, Has.Count.EqualTo(1),
                "The selected animation must be emitted exactly once for the composed scene.");
            var animationClip = model.LogicalAnimations.Single();
            Assert.That(animationClip.Channels, Is.Not.Empty);

            var animatedTargets = animationClip.Channels
                .Select(channel => channel.TargetNode)
                .ToHashSet();
            var animatedHierarchy = animatedTargets
                .SelectMany(target => EnumerateAncestors(target).Skip(1))
                .ToHashSet();

            // SharpGLTF can materialize one Skin object per skinned node even
            // when all of those objects describe the same skeleton.  The
            // invariant we need from the VMD export is the shared ordered
            // joint binding and hierarchy, not the number or reference
            // identity of Skin records.
            var referenceSkin = skinnedNodes[0].Skin!;
            var referenceJoints = referenceSkin.Joints.ToList();
            Assert.That(referenceJoints, Is.Not.Empty,
                "The exported weighted meshes must reference at least one skeleton joint.");
            Assert.That(skinnedNodes.All(node =>
            {
                var skin = node.Skin!;
                return skin.Joints.SequenceEqual(referenceJoints)
                    && ReferenceEquals(skin.Skeleton, referenceSkin.Skeleton);
            }), Is.True,
                "Every weighted component mesh must use the same ordered joints and compatible skeleton root.");

            var referenceHierarchyRoot = referenceJoints
                .Select(GetHierarchyRoot)
                .Distinct()
                .Single();
            Assert.That(referenceJoints.All(joint =>
                ReferenceEquals(GetHierarchyRoot(joint), referenceHierarchyRoot)), Is.True,
                "The shared skin joints must belong to one hierarchy root.");
            if (referenceSkin.Skeleton != null)
            {
                Assert.That(referenceJoints.All(joint =>
                    IsSameOrAncestor(referenceSkin.Skeleton, joint)), Is.True,
                    "The declared skin skeleton root must contain every shared joint.");
            }

            Assert.That(referenceJoints.All(joint =>
                animatedTargets.Contains(joint) || animatedHierarchy.Contains(joint)), Is.True,
                "Every shared skin joint must be animated or lie in the selected animation hierarchy.");
            Assert.That(animatedTargets.All(referenceJoints.Contains), Is.True,
                "The selected animation must target the shared skin hierarchy rather than another node set.");

            var declaredAttachments = expectedComponents
                .Where(component => string.IsNullOrWhiteSpace(component.Component.AttachmentPoint) == false)
                .ToList();
            var attachedNodes = new List<Node>();
            foreach (var expectedComponent in declaredAttachments)
            {
                var nodesForComponent = componentNodes
                    .Where(node => node.Name.StartsWith(expectedComponent.MeshNamePrefix, StringComparison.Ordinal))
                    .ToList();
                var expectedAttachmentPoint = expectedComponent.Component.AttachmentPoint;

                Assert.That(nodesForComponent.All(node =>
                    node.VisualParent != null
                    && node.VisualParent.Mesh == null
                    && string.Equals(node.VisualParent.Name, expectedAttachmentPoint, StringComparison.OrdinalIgnoreCase)), Is.True,
                    $"Every {expectedComponent.MeshNamePrefix} mesh must be parented to its declared attachment bone '{expectedAttachmentPoint}'.");

                attachedNodes.AddRange(nodesForComponent);
            }

            Assert.That(attachedNodes, Has.Count.GreaterThan(0),
                "At least one declared VMD component must be parented to its named skeleton attachment bone.");
            foreach (var attachedNode in attachedNodes)
            {
                var parent = attachedNode.VisualParent!;
                Assert.That(animatedTargets.Contains(parent) || animatedHierarchy.Contains(parent), Is.True,
                    $"The declared attachment bone '{parent.Name}' must be animated or lie in the animated hierarchy.");
            }
            dialogs.Verify(x => x.ShowExceptionWindow(It.IsAny<Exception>()), Times.Never);
        }
        finally
        {
            skeletonLookup.Dispose();
            DeleteTempDirectory(outputDirectory);
        }
    }

    private static RmvToGltfExporter CreateProductionExporter(
        IPackFileService packFileService,
        IStandardDialogs dialogs,
        ISkeletonAnimationLookUpHelper skeletonLookup,
        IModelAssetResolver? modelResolver = null)
    {
        modelResolver ??= new ModelAssetResolver(packFileService);
        var materialExporter = new DdsToMaterialPngExporter(packFileService, new SystemImageSaveHandler());
        var normalExporter = new DdsToNormalPngExporter(packFileService, new SystemImageSaveHandler());
        var compositionResolver = new VariantMeshCompositionResolver(packFileService, modelResolver);

        return new RmvToGltfExporter(
            new GltfSceneSaver(dialogs),
            new GltfMeshBuilder(),
            new GltfTextureHandler(normalExporter, materialExporter, packFileService),
            new GltfSkeletonBuilder(packFileService),
            new GltfAnimationBuilder(packFileService),
            skeletonLookup,
            modelResolver,
            compositionResolver);
    }

    private static byte[] DecodeDds(IPackFileService packFileService, string path)
    {
        var file = packFileService.FindFile(path);
        Assert.That(file, Is.Not.Null, $"Tracked texture '{path}' was not found.");
        return TextureHelper.ConvertDdsToPng(file!.DataSource.ReadData());
    }

    private static byte[] ReadMemoryImage(SharpGLTF.Memory.MemoryImage image)
    {
        using var stream = image.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static DecodedImage DecodePng(byte[] png)
    {
        using var stream = new MemoryStream(png);
        using var image = System.Drawing.Image.FromStream(stream);
        using var bitmap = new Bitmap(image);
        var pixels = new byte[bitmap.Width * bitmap.Height * 4];
        var offset = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                pixels[offset++] = color.R;
                pixels[offset++] = color.G;
                pixels[offset++] = color.B;
                pixels[offset++] = color.A;
            }
        }

        return new DecodedImage(bitmap.Width, bitmap.Height, pixels);
    }

    private static bool ImagesEquivalent(DecodedImage left, DecodedImage right)
    {
        if (left.Width != right.Width || left.Height != right.Height)
            return false;

        // PNG metadata and byte encoding may differ, but the production
        // conversion should preserve the decoded RGBA pixels exactly.
        return left.Pixels.SequenceEqual(right.Pixels);
    }

    private static DecodedImage InvertRgb(DecodedImage source)
    {
        var pixels = source.Pixels.ToArray();
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }

        return new DecodedImage(source.Width, source.Height, pixels);
    }

    private static string GetExpectedMaskOutputPath(string outputDirectory, string sourcePath)
        => Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourcePath)}_mask.png");

    private static IEnumerable<Node> EnumerateAncestors(Node node)
    {
        for (Node? current = node; current != null; current = current.VisualParent)
            yield return current;
    }

    private static Node GetHierarchyRoot(Node node)
    {
        var root = node;
        while (root.VisualParent != null)
            root = root.VisualParent;
        return root;
    }

    private static bool IsSameOrAncestor(Node ancestor, Node node)
    {
        for (Node? current = node; current != null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    private static string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed record DecodedImage(int Width, int Height, byte[] Pixels);
}
