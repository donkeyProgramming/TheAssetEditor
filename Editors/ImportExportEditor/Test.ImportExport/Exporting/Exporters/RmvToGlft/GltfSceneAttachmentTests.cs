using System.Numerics;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

public class GltfSceneAttachmentTests
{
    [Test]
    public void NamedAttachmentWinsAndUnknownNameDoesNotFallBackToMatrixIndex()
    {
        var namedResult = BuildScene(
            CreateWeightedMesh("named_attachment_mesh"),
            attachmentPoint: "named_joint",
            matrixIndex: 2,
            hasWeights: true,
            canUseSkeleton: true);

        var namedNode = GetMeshNode(namedResult.Saver.ModelRoot!, "named_attachment_mesh");
        Assert.That(namedNode.VisualParent, Is.SameAs(namedResult.NamedJoint));
        Assert.That(namedNode.Skin, Is.Null,
            "A valid attachment plus a matrix override is a rigid renderer attachment, not a second skin transform.");

        var unknownResult = BuildScene(
            CreateStaticMesh("unknown_attachment_mesh"),
            attachmentPoint: "missing_joint",
            matrixIndex: 2,
            hasWeights: false,
            canUseSkeleton: false);

        var unknownNode = GetMeshNode(unknownResult.Saver.ModelRoot!, "unknown_attachment_mesh");
        Assert.That(unknownNode.VisualParent, Is.Null,
            "A named attachment that cannot be resolved must not silently use MatrixIndex.");
    }

    [Test]
    public void MatrixIndexIsUsedOnlyWhenAttachmentNameIsEmpty()
    {
        var result = BuildScene(
            CreateWeightedMesh("matrix_attachment_mesh"),
            attachmentPoint: string.Empty,
            matrixIndex: 2,
            hasWeights: true,
            canUseSkeleton: true);

        var node = GetMeshNode(result.Saver.ModelRoot!, "matrix_attachment_mesh");
        Assert.That(node.VisualParent, Is.SameAs(result.MatrixJoint));
        Assert.That(node.Skin, Is.Null);
    }

    [Test]
    public void WeightedMeshWithoutAttachmentUsesExactlyOneSharedSkin()
    {
        var result = BuildScene(
            CreateWeightedMesh("weighted_mesh"),
            attachmentPoint: string.Empty,
            matrixIndex: -1,
            hasWeights: true,
            canUseSkeleton: true);

        var node = GetMeshNode(result.Saver.ModelRoot!, "weighted_mesh");
        Assert.That(node.Skin, Is.Not.Null);
        Assert.That(result.Saver.ModelRoot!.LogicalSkins, Has.Count.EqualTo(1));
    }

    [Test]
    public void PivotIsStoredAsMeshNodeLocalTranslation()
    {
        var result = BuildScene(
            CreateStaticMesh("pivot_mesh"),
            attachmentPoint: "named_joint",
            matrixIndex: -1,
            hasWeights: false,
            canUseSkeleton: false,
            pivotPoint: new Vector3(-2, 3, 4));

        var node = GetMeshNode(result.Saver.ModelRoot!, "pivot_mesh");
        Assert.That(node.VisualParent, Is.SameAs(result.NamedJoint));
        Assert.That(node.LocalTransform.Translation, Is.EqualTo(new Vector3(-2, 3, 4)));
    }

    private static (TestGltfSceneSaver Saver, Node NamedJoint, Node MatrixJoint) BuildScene(
        IMeshBuilder<MaterialBuilder> meshBuilder,
        string attachmentPoint,
        int matrixIndex,
        bool hasWeights,
        bool canUseSkeleton,
        Vector3? pivotPoint = null)
    {
        var saver = new TestGltfSceneSaver();
        var exporter = CreateExporter(saver);
        var model = ModelRoot.CreateModel();
        var scene = model.UseScene("default");
        var skeletonRoot = scene.CreateNode("skeleton_root");
        var namedJoint = skeletonRoot.CreateNode("named_joint");
        var matrixJoint = skeletonRoot.CreateNode("matrix_joint");
        var unusedJoint = skeletonRoot.CreateNode("unused_joint");
        var skeleton = new ProcessedGltfSkeleton
        {
            Data =
            [
                (skeletonRoot, Matrix4x4.Identity),
                (namedJoint, Matrix4x4.Identity),
                (matrixJoint, Matrix4x4.Identity),
                (unusedJoint, Matrix4x4.Identity)
            ]
        };

        var inputFile = PackFile.CreateFromASCII("test.rigid_model_v2", "test");
        var settings = new RmvToGltfExporterSettings(
            inputFile,
            [],
            Path.Combine(Path.GetTempPath(), "scene-attachment-test.gltf"),
            false,
            false,
            false,
            false,
            false);

        exporter.BuildGltfScene(
            [new RmvToGltfExporter.ExportedMesh(
                meshBuilder,
                attachmentPoint,
                matrixIndex,
                hasWeights,
                canUseSkeleton,
                true,
                pivotPoint ?? Vector3.Zero)],
            skeleton,
            settings,
            model);

        Assert.That(saver.ModelRoot, Is.SameAs(model));
        return (saver, namedJoint, matrixJoint);
    }

    private static RmvToGltfExporter CreateExporter(TestGltfSceneSaver saver)
    {
        var packFileService = new Mock<IPackFileService>().Object;
        var normalExporter = new Mock<IDdsToNormalPngExporter>().Object;
        var materialExporter = new Mock<IDdsToMaterialPngExporter>().Object;
        var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>().Object;
        var modelResolver = new Mock<IModelAssetResolver>().Object;

        return new RmvToGltfExporter(
            saver,
            new GltfMeshBuilder(),
            new GltfTextureHandler(normalExporter, materialExporter),
            new GltfSkeletonBuilder(packFileService),
            new GltfAnimationBuilder(packFileService),
            skeletonLookup,
            modelResolver,
            null);
    }

    private static Node GetMeshNode(ModelRoot model, string name)
        => model.LogicalNodes.Single(x => x.Name == name);

    private static IMeshBuilder<MaterialBuilder> CreateStaticMesh(string name)
    {
        var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty>(name);
        var primitive = mesh.UsePrimitive(new MaterialBuilder($"{name}_material"));
        primitive.AddTriangle(
            CreateStaticVertex(new Vector3(0, 0, 0)),
            CreateStaticVertex(new Vector3(1, 0, 0)),
            CreateStaticVertex(new Vector3(0, 1, 0)));
        return mesh;
    }

    private static IMeshBuilder<MaterialBuilder> CreateWeightedMesh(string name)
    {
        var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(name);
        var primitive = mesh.UsePrimitive(new MaterialBuilder($"{name}_material"));
        primitive.AddTriangle(
            CreateWeightedVertex(new Vector3(0, 0, 0)),
            CreateWeightedVertex(new Vector3(1, 0, 0)),
            CreateWeightedVertex(new Vector3(0, 1, 0)));
        return mesh;
    }

    private static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty> CreateStaticVertex(Vector3 position)
    {
        var vertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexEmpty>();
        vertex.Geometry.Position = position;
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(Vector3.UnitX, 1);
        vertex.Material.TexCoord = Vector2.Zero;
        return vertex;
    }

    private static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> CreateWeightedVertex(Vector3 position)
    {
        var vertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
        vertex.Geometry.Position = position;
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(Vector3.UnitX, 1);
        vertex.Material.TexCoord = Vector2.Zero;
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }
}
