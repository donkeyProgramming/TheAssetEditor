using System.Numerics;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

public class VmdSkeletonExportTests
{
    [Test]
    public void VmdExportsSkeletonWithoutAnimationsAndBuildsSelectedAnimationsOnce()
    {
        var input = PackFile.CreateFromASCII("root.variantmeshdefinition", "<VARIANT_MESH />");
        var asset = CreateAsset(input);
        var root = new ResolvedVariantMeshNode("root", input, null) { ModelAsset = asset };
        var secondInput = PackFile.CreateFromASCII("second.rigid_model_v2", "second");
        root.Slots.Add(new ResolvedVariantMeshSlot("second", string.Empty)
        {
            SelectedChild = new ResolvedVariantMeshNode("second", secondInput, null)
            {
                ModelAsset = CreateAsset(secondInput, "second")
            }
        });
        var composition = new ResolvedVariantMeshComposition(input, root, []);
        var compositionResolver = new Mock<IVariantMeshCompositionResolver>();
        compositionResolver.Setup(x => x.Resolve(input)).Returns(composition);

        var packFileService = new Mock<IPackFileService>().Object;
        var skeletonFile = CreateSkeletonFile();
        var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
        skeletonLookup
            .Setup(x => x.GetSkeletonFileFromName("test_skeleton"))
            .Returns(skeletonFile);
        var animationBuilder = new CountingAnimationBuilder(packFileService);
        var saver = new TestGltfSceneSaver();
        var exporter = new RmvToGltfExporter(
            saver,
            new GltfMeshBuilder(),
            new EmptyTextureHandler(),
            new GltfSkeletonBuilder(packFileService),
            animationBuilder,
            skeletonLookup.Object,
            new Mock<IModelAssetResolver>().Object,
            compositionResolver.Object);

        exporter.Export(new RmvToGltfExporterSettings(
            input,
            [],
            Path.Combine(Path.GetTempPath(), "vmd-skeleton-only.gltf"),
            false,
            false,
            false,
            false,
            false));

        Assert.That(saver.ModelRoot, Is.Not.Null);
        Assert.That(saver.ModelRoot!.LogicalNodes.Any(x => x.Name == "root"), Is.True);
        Assert.That(
            saver.ModelRoot.LogicalNodes.Any(x => x.Name == "//skeleton//test_skeleton"),
            Is.True,
            "A composed VMD export keeps its shared skeleton even when animation clips are disabled.");
        Assert.That(saver.ModelRoot.LogicalSkins, Has.Count.EqualTo(0));
        Assert.That(saver.ModelRoot.LogicalAnimations, Has.Count.EqualTo(0));

        var selectedAnimation = PackFile.CreateFromASCII("idle.anim", "not parsed by the counting test builder");
        exporter.Export(new RmvToGltfExporterSettings(
            input,
            [selectedAnimation],
            Path.Combine(Path.GetTempPath(), "vmd-with-animation.gltf"),
            false,
            false,
            false,
            true,
            false));

        Assert.That(animationBuilder.BuildCount, Is.EqualTo(1),
            "A composed export has one shared skeleton and one animation-build pass regardless of part count.");
    }

    [Test]
    public void DoesNotSkinComponentAgainstDifferentNamedSharedSkeleton()
    {
        var rootInput = PackFile.CreateFromASCII("root.variantmeshdefinition", "<VARIANT_MESH />");
        var childInput = PackFile.CreateFromASCII("child.rigid_model_v2", "child");
        var root = new ResolvedVariantMeshNode("root", rootInput, null)
        {
            ModelAsset = CreateAsset(rootInput, "root", "test_skeleton", weighted: true)
        };
        root.Slots.Add(new ResolvedVariantMeshSlot("child", string.Empty)
        {
            SelectedChild = new ResolvedVariantMeshNode("child", childInput, null)
            {
                ModelAsset = CreateAsset(childInput, "child", "other_skeleton", weighted: true, matrixIndex: 0)
            }
        });

        var composition = new ResolvedVariantMeshComposition(rootInput, root, []);
        var compositionResolver = new Mock<IVariantMeshCompositionResolver>();
        compositionResolver.Setup(x => x.Resolve(rootInput)).Returns(composition);

        var packFileService = new Mock<IPackFileService>().Object;
        var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
        skeletonLookup
            .Setup(x => x.GetSkeletonFileFromName("test_skeleton"))
            .Returns(CreateSkeletonFile());
        var saver = new TestGltfSceneSaver();
        var exporter = new RmvToGltfExporter(
            saver,
            new GltfMeshBuilder(),
            new EmptyTextureHandler(),
            new GltfSkeletonBuilder(packFileService),
            new CountingAnimationBuilder(packFileService),
            skeletonLookup.Object,
            new Mock<IModelAssetResolver>().Object,
            compositionResolver.Object);

        exporter.Export(new RmvToGltfExporterSettings(
            rootInput,
            [],
            Path.Combine(Path.GetTempPath(), "vmd-different-skeleton.gltf"),
            false,
            false,
            false,
            false,
            false));

        Assert.That(saver.ModelRoot, Is.Not.Null);
        var rootNode = saver.ModelRoot!.LogicalNodes.Single(x => x.Name == "vmd_part_000_000_root");
        var childNode = saver.ModelRoot.LogicalNodes.Single(x => x.Name == "vmd_part_001_000_child");
        Assert.That(rootNode.Skin, Is.Not.Null);
        Assert.That(childNode.Skin, Is.Null,
            "A component declaring another skeleton must remain unskinned rather than binding to the shared root skeleton.");
        Assert.That(childNode.VisualParent, Is.Null,
            "A differing component skeleton must not interpret its MatrixIndex against the shared root skeleton.");
        Assert.That(saver.ModelRoot.LogicalSkins, Has.Count.EqualTo(1));
    }

    private static ResolvedModelAsset CreateAsset(
        PackFile input,
        string modelName = "synthetic",
        string skeletonName = "test_skeleton",
        bool weighted = false,
        int matrixIndex = -1)
    {
        var material = new WeightedMaterial { ModelName = modelName, MatrixIndex = matrixIndex };
        var model = new RmvModel
        {
            Material = material,
            Mesh = new RmvMesh
            {
                VertexList =
                [
                    CreateVertex(new Vector3(0, 0, 0), weighted),
                    CreateVertex(new Vector3(1, 0, 0), weighted),
                    CreateVertex(new Vector3(0, 1, 0), weighted)
                ],
                IndexList = [0, 1, 2]
            }
        };
        var header = new RmvFileHeader
        {
            Version = RmvVersionEnum.RMV2_V6,
            LodCount = 1,
            SkeletonName = skeletonName
        };
        var rmv = new RmvFile
        {
            Header = header,
            ModelList = [new[] { model }],
            LodHeaders = [LodHeaderFactory.Create().CreateEmpty(RmvVersionEnum.RMV2_V6, 0, 0, 0)]
        };
        var part = new ResolvedModelPart(0, 0, model, ResolvedModelMaterial.Create(material));
        return new ResolvedModelAsset(input, input, null, null, rmv, [new[] { part }], []);
    }

    private static CommonVertex CreateVertex(Vector3 position, bool weighted = false)
    {
        var vertex = new CommonVertex
        {
            Position = new Microsoft.Xna.Framework.Vector4(position.X, position.Y, position.Z, 1),
            Normal = Microsoft.Xna.Framework.Vector3.UnitZ,
            Tangent = Microsoft.Xna.Framework.Vector3.UnitX,
            Uv = Microsoft.Xna.Framework.Vector2.Zero,
            BoneIndex = new byte[4],
            BoneWeight = new float[4]
        };

        if (weighted)
        {
            vertex.WeightCount = 1;
            vertex.BoneIndex[0] = 0;
            vertex.BoneWeight[0] = 1;
        }

        return vertex;
    }

    private static AnimationFile CreateSkeletonFile()
    {
        var file = new AnimationFile();
        file.Header.SkeletonName = "test_skeleton";
        file.Bones = [new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = AnimationFile.BoneIndexNoParent }];
        var frame = new AnimationFile.Frame();
        frame.Transforms.Add(new RmvVector3(Microsoft.Xna.Framework.Vector3.Zero));
        frame.Quaternion.Add(new RmvVector4(0, 0, 0, 1));
        var part = new AnimationFile.AnimationPart();
        part.DynamicFrames.Add(frame);
        file.AnimationParts.Add(part);
        return file;
    }

    private sealed class EmptyTextureHandler : IGltfTextureHandler
    {
        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings)
            => [];

        public List<TextureResult> HandleTextures(ResolvedModelAsset asset, RmvToGltfExporterSettings settings)
            => [];

        public List<TextureResult> HandleTextures(
            ResolvedModelAsset asset,
            RmvToGltfExporterSettings settings,
            GltfTextureExportSession session)
            => [];
    }

    private sealed class CountingAnimationBuilder : GltfAnimationBuilder
    {
        public CountingAnimationBuilder(IPackFileService packFileService)
            : base(packFileService)
        {
        }

        public int BuildCount { get; private set; }

        public override void Build(
            AnimationFile animSkeleton,
            RmvToGltfExporterSettings settings,
            ProcessedGltfSkeleton gltfSkeleton,
            SharpGLTF.Schema2.ModelRoot outputScene)
        {
            BuildCount++;
        }
    }
}
