using System.Numerics;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.TestUtility;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.Core.Services;
using Shared.Core.Settings;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using XNA = Microsoft.Xna.Framework;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class RmvMeshBuilderSceneTests
{
    [Test]
    public void Build_UsesEveryPrimitiveAndSceneNodeInstance()
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("shared_mesh");
        AddTriangle(geometry.UsePrimitive(CreateMaterial("first")), 0);
        AddTriangle(geometry.UsePrimitive(CreateMaterial("second")), 2);

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        scene.CreateNode("first_instance").WithMesh(mesh);
        var translatedNode = scene.CreateNode("second_instance").WithMesh(mesh);
        translatedNode.LocalMatrix = Matrix4x4.CreateTranslation(5, 0, 0);

        var result = RmvMeshBuilder.Build(CreateSettings(), modelRoot, null, "");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ModelList[0], Has.Length.EqualTo(4));
        Assert.That(result.LodHeaders[0].MeshCount, Is.EqualTo(4));
        Assert.That(result.ModelList[0].Select(x => x.Material.ModelName), Is.EqualTo(new[]
        {
            "first_instance_part1",
            "first_instance_part2",
            "second_instance_part1",
            "second_instance_part2",
        }));

        var firstInstanceX = result.ModelList[0][0].Mesh.VertexList[0].Position.X;
        var secondInstanceX = result.ModelList[0][2].Mesh.VertexList[0].Position.X;
        Assert.That(secondInstanceX, Is.EqualTo(firstInstanceX - 5).Within(0.0001f));
    }

    [Test]
    public void BuildWithSummary_MoreThanFourInfluences_KeepsStableStrongestFourAndQuantizesTo255()
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints8>("mesh");
        var primitive = geometry.UsePrimitive(CreateMaterial("material"));
        var bindings = new (int, float)[]
        {
            (0, 0.05f),
            (1, 0.30f),
            (2, 0.10f),
            (3, 0.25f),
            (4, 0.15f),
            (5, 0.10f),
            (6, 0.05f),
            (7, 0),
        };
        primitive.AddTriangle(
            CreateEightInfluenceVertex(0, 0, bindings),
            CreateEightInfluenceVertex(1, 0, bindings),
            CreateEightInfluenceVertex(0, 1, bindings));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("armature");
        var joints = new Node[8];
        var parent = armature;
        for (var index = 0; index < joints.Length; index++)
        {
            joints[index] = parent.CreateNode($"bone_{index}");
            parent = joints[index];
        }
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            joints.Select(joint => (joint, Matrix4x4.Identity)).ToArray());
        var skeleton = CreateSkeletonFile(
            "test_skeleton",
            joints.Select(joint => joint.Name).ToArray());

        var result = RmvMeshBuilder.BuildWithSummary(
            CreateSettings(),
            modelRoot,
            skeleton,
            "test_skeleton");

        var vertex = result.File!.ModelList[0][0].Mesh.VertexList[0];
        Assert.Multiple(() =>
        {
            Assert.That(vertex.BoneIndex, Is.EqualTo(new byte[] { 1, 3, 4, 2 }));
            Assert.That(
                vertex.BoneWeight.Select(weight => (byte)(weight * byte.MaxValue)).Sum(value => value),
                Is.EqualTo(byte.MaxValue));
            Assert.That(result.Summary.TotalAffectedVertices, Is.EqualTo(3));
            Assert.That(result.Summary.Segments.Single().ModelName, Is.EqualTo("mesh_node"));
            Assert.That(result.Summary.Segments.Single().AffectedVertices, Is.EqualTo(3));
            Assert.That(result.Summary.MaximumDiscardedWeight, Is.EqualTo(0.20f).Within(0.0001f));
            Assert.That(result.Summary.VerticesAboveTenPercentDiscarded, Is.EqualTo(3));
        });
    }

    [Test]
    public void BuildWithSummary_MissingNormalTangentAndUv_RebuildsFiniteBasisAndReportsRepairs()
    {
        var geometry = new MeshBuilder<VertexPosition, VertexEmpty, VertexEmpty>("mesh");
        var primitive = geometry.UsePrimitive(CreateMaterial("material"));
        primitive.AddTriangle(
            CreatePositionOnlyVertex(0, 0),
            CreatePositionOnlyVertex(1, 0),
            CreatePositionOnlyVertex(0, 1));
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").WithMesh(mesh);

        var result = RmvMeshBuilder.BuildWithSummary(
            CreateSettings(),
            modelRoot,
            null,
            "");

        var segment = result.Summary.Segments.Single();
        var vertices = result.File!.ModelList[0][0].Mesh.VertexList;
        Assert.Multiple(() =>
        {
            Assert.That(segment.RebuiltNormals, Is.True);
            Assert.That(segment.RebuiltTangents, Is.True);
            Assert.That(segment.DefaultedTextureCoordinates, Is.True);
            Assert.That(vertices, Has.All.Matches<CommonVertex>(vertex =>
                IsFiniteUnitVector(vertex.Normal) &&
                IsFiniteUnitVector(vertex.Tangent) &&
                IsFiniteUnitVector(vertex.BiNormal) &&
                vertex.Uv == XNA.Vector2.Zero));
        });
    }

    [Test]
    public void BuildWithSummary_ValidNegativeTangentHandedness_PreservesTangentBasis()
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(CreateMaterial("material"));
        primitive.AddTriangle(
            CreateVertex(0, 0, -1),
            CreateVertex(1, 0, -1),
            CreateVertex(0, 1, -1));
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").WithMesh(mesh);

        var result = RmvMeshBuilder.BuildWithSummary(
            CreateSettings(),
            modelRoot,
            null,
            "");

        var segment = result.Summary.Segments.Single();
        var vertex = result.File!.ModelList[0][0].Mesh.VertexList[0];
        Assert.Multiple(() =>
        {
            Assert.That(segment.RebuiltTangents, Is.False);
            Assert.That(vertex.Tangent.X, Is.EqualTo(-1).Within(0.0001f));
            Assert.That(vertex.BiNormal.Y, Is.EqualTo(-1).Within(0.0001f));
        });
    }

    [Test]
    public void Import_GlbContainer_AddsRmvFile()
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("mesh");
        AddTriangle(geometry.UsePrimitive(CreateMaterial("material")), 0);
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        modelRoot.UseScene("default").CreateNode("mesh_node").WithMesh(mesh);

        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var materialBuilder = new RmvMaterialBuilder();
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                materialBuilder);
            var destination = PackFileContainer.CreatePackFile("test");
            var settings = new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                true,
                false,
                false,
                false,
                20);

            var succeeded = importer.Import(settings).Succeeded;

            Assert.That(succeeded, Is.True);
            Assert.That(
                destination.FileList.Keys,
                Does.Contain($"models\\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2".ToLowerInvariant()));
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_MultiplePartialAnimations_CreatesOneAnimPerClip()
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("mesh");
        AddTriangle(geometry.UsePrimitive(CreateMaterial("material")), 0);
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        scene.CreateNode("mesh_node").WithMesh(mesh);
        scene.CreateNode("//skeleton//test_skeleton");
        var boneNode = scene.CreateNode("root");

        foreach (var animationName in new[] { "Idle Pose", "Walk" })
        {
            var animation = modelRoot.CreateAnimation(animationName);
            animation.CreateTranslationChannel(boneNode, new Dictionary<float, Vector3>
            {
                [0] = Vector3.Zero,
                [0.1f] = Vector3.One,
            });
        }

        var skeleton = new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader { SkeletonName = "test_skeleton" },
            Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, ParentId = -1, Name = "root" },
            ],
            AnimationParts =
            [
                new AnimationFile.AnimationPart
                {
                    DynamicFrames =
                    [
                        new AnimationFile.Frame
                        {
                            Transforms = [new RmvVector3(0, 0, 0)],
                            Quaternion = [new RmvVector4(0, 0, 0, 1)],
                        },
                    ],
                },
            ],
        };

        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
            skeletonLookup
                .Setup(service => service.GetSkeletonFileFromName("test_skeleton"))
                .Returns(skeleton);
            var materialBuilder = new RmvMaterialBuilder();
            var importer = new GltfImporter(
                packFileService,
                skeletonLookup.Object,
                materialBuilder);
            var destination = PackFileContainer.CreatePackFile("test");
            var settings = new GltfImporterSettings(
                glbPath,
                "animations",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                true,
                20);

            importer.Import(settings);

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            Assert.Multiple(() =>
            {
                Assert.That(destination.FileList.Keys, Does.Contain($"animations\\{baseName}_idle_pose.anim"));
                Assert.That(destination.FileList.Keys, Does.Contain($"animations\\{baseName}_walk.anim"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_MissingGameSkeleton_CreatesSkeletonAnim()
    {
        var modelRoot = CreateSkinnedModelRoot("test_skeleton");
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var materialBuilder = new RmvMaterialBuilder();
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                materialBuilder);
            var destination = PackFileContainer.CreatePackFile("test");
            var settings = new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                false,
                20);

            importer.Import(settings);

            var skeletonFile = destination.FileList["animations\\skeletons\\test_skeleton.anim"];
            var skeleton = AnimationFile.Create(skeletonFile);
            Assert.Multiple(() =>
            {
                Assert.That(skeleton.Header.SkeletonName, Is.EqualTo("test_skeleton"));
                Assert.That(skeleton.Bones.Select(bone => bone.Name), Is.EqualTo(new[] { "root", "child" }));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_StandardGlbWithoutSkeletonMarker_CreatesNamedExternalSkeletonAndRmv()
    {
        var modelRoot = CreateSkinnedModelRoot(null);
        modelRoot.LogicalSkins.Single().Name = "ExternalArmature";
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());
            var destination = PackFileContainer.CreatePackFile("test");

            var result = importer.Import(new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                false,
                20));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(
                    destination.FileList.Keys,
                    Does.Contain(@"animations\skeletons\externalarmature.anim"));
                Assert.That(
                    destination.FileList.Keys,
                    Does.Contain($"models\\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2".ToLowerInvariant()));
            });

            var skeleton = AnimationFile.Create(
                destination.FileList[@"animations\skeletons\externalarmature.anim"]);
            Assert.Multiple(() =>
            {
                Assert.That(skeleton.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
                Assert.That(skeleton.Bones.Select(bone => bone.Name), Is.EqualTo(new[] { "root", "child" }));
                Assert.That(skeleton.Bones.Select(bone => bone.ParentId), Is.EqualTo(new[] { -1, 0 }));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_BlenderExternalSkeletonFixture_CreatesLod0ModelAndSkeleton()
    {
        var glbPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "blender_external_skeleton.glb");
        var modelRoot = ModelRoot.Load(glbPath);
        Assert.That(
            modelRoot.LogicalNodes,
            Has.None.Matches<Node>(node =>
                node.Name?.StartsWith("//skeleton//", StringComparison.OrdinalIgnoreCase) == true));

        var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var destination = PackFileContainer.CreatePackFile("test");
        var importer = new GltfImporter(
            packFileService,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(CreateSettings(glbPath, destination));

        Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
        var rmvPath = @"models\blender_external_skeleton.rigid_model_v2";
        var skeletonPath = @"animations\skeletons\externalarmature.anim";
        var rmv = ModelFactory.Create().Load(
            destination.FileList[rmvPath].DataSource.ReadData());
        var skeleton = AnimationFile.Create(destination.FileList[skeletonPath]);
        Assert.Multiple(() =>
        {
            Assert.That(rmv.LodHeaders, Has.Length.EqualTo(1));
            Assert.That(rmv.ModelList[0], Has.Length.EqualTo(1));
            Assert.That(rmv.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
            Assert.That(skeleton.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
            Assert.That(skeleton.Bones.Select(bone => bone.Name), Is.EqualTo(new[] { "root", "child" }));
        });
    }

    [Test]
    public void Import_ExternalMeshAttributeFixture_RepairsGeometryAndReportsEveryLoss()
    {
        var gltfPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "Gltf",
            "external_mesh_attributes.gltf");
        var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
        var destination = PackFileContainer.CreatePackFile("test");
        var importer = new GltfImporter(
            packFileService,
            Mock.Of<ISkeletonAnimationLookUpHelper>(),
            new RmvMaterialBuilder());

        var result = importer.Import(CreateSettings(gltfPath, destination));

        Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
        var rmv = ModelFactory.Create().Load(
            destination.FileList[@"models\external_mesh_attributes.rigid_model_v2"]
                .DataSource.ReadData());
        var vertices = rmv.ModelList[0][0].Mesh.VertexList;
        Assert.Multiple(() =>
        {
            Assert.That(result.Warnings, Has.Some.Contains("on 3 vertices"));
            Assert.That(result.Warnings, Has.Some.Contains("mesh_node"));
            Assert.That(result.Warnings, Has.Some.Contains("20.0%"));
            Assert.That(result.Warnings, Has.Some.Contains("no normals"));
            Assert.That(result.Warnings, Has.Some.Contains("no tangents"));
            Assert.That(result.Warnings, Has.Some.Contains("UV"));
            Assert.That(result.Warnings, Has.Some.Contains("vertex colours"));
            Assert.That(result.Warnings, Has.Some.Contains("morph targets"));
            Assert.That(vertices, Has.All.Matches<CommonVertex>(vertex =>
                vertex.BoneWeight.Select(weight => (byte)(weight * byte.MaxValue)).Sum(value => value) == byte.MaxValue &&
                IsFiniteUnitVector(vertex.Normal) &&
                IsFiniteUnitVector(vertex.Tangent) &&
                IsFiniteUnitVector(vertex.BiNormal) &&
                vertex.Uv == XNA.Vector2.Zero));
        });
    }

    [Test]
    public void Import_StandardGlbWithEditedSkeletonName_UsesRequestedName()
    {
        var modelRoot = CreateSkinnedModelRoot(null);
        modelRoot.LogicalSkins.Single().Name = "ExternalArmature";
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination) with
            {
                NewSkeletonName = "CustomSkeleton",
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(
                    destination.FileList.Keys,
                    Does.Contain(@"animations\skeletons\customskeleton.anim"));
                Assert.That(
                    AnimationFile.Create(
                        destination.FileList[@"animations\skeletons\customskeleton.anim"])
                        .Header.SkeletonName,
                    Is.EqualTo("CustomSkeleton"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExternalSkinWithPathInSkeletonName_ReturnsFailureAndLeavesPackUnchanged()
    {
        var modelRoot = CreateSkinnedModelRoot(null);
        modelRoot.LogicalSkins.Single().Name = "ExternalArmature";
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination) with
            {
                NewSkeletonName = @"folder\CustomSkeleton",
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("skeleton name"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_EquivalentSkinInstances_UsesOneLogicalSkeletonForEveryMesh()
    {
        var modelRoot = CreateMultipleSkinnedModelRoot(equivalentHierarchy: true);
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.That(result.Succeeded, Is.True);
            var rmvPath = $"models\\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2"
                .ToLowerInvariant();
            var rmv = ModelFactory.Create().Load(
                destination.FileList[rmvPath].DataSource.ReadData());
            Assert.Multiple(() =>
            {
                Assert.That(rmv.ModelList[0], Has.Length.EqualTo(2));
                Assert.That(
                    destination.FileList.Keys.Count(path =>
                        path.StartsWith(@"animations\skeletons\", StringComparison.OrdinalIgnoreCase)),
                    Is.EqualTo(1));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_DifferentSkinHierarchies_ReturnsFailureAndLeavesPackUnchanged()
    {
        var modelRoot = CreateMultipleSkinnedModelRoot(equivalentHierarchy: false);
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("more than one skeleton"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_DifferentSkinJointSets_ReturnsFailureAndLeavesPackUnchanged()
    {
        var modelRoot = CreateMultipleSkinnedModelRoot(
            equivalentHierarchy: true,
            secondChildName: "other_child");
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("more than one skeleton"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_EquivalentSkinStructuresWithDifferentBindPoses_ReturnsFailureAndLeavesPackUnchanged()
    {
        var modelRoot = CreateMultipleSkinnedModelRoot(
            equivalentHierarchy: true,
            secondChildTranslationY: 2);
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var importer = new GltfImporter(
                packFileService,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("different bind poses"));
                Assert.That(destination.FileList, Is.Empty);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExistingGameSkeleton_DoesNotCopySkeletonAnim()
    {
        var modelRoot = CreateSkinnedModelRoot("test_skeleton");
        var glbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(glbPath);
            var packFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
            skeletonLookup
                .Setup(service => service.GetSkeletonFileFromName("test_skeleton"))
                .Returns(CreateSkeletonFile("test_skeleton"));
            var materialBuilder = new RmvMaterialBuilder();
            var importer = new GltfImporter(
                packFileService,
                skeletonLookup.Object,
                materialBuilder);
            var destination = PackFileContainer.CreatePackFile("test");
            var settings = new GltfImporterSettings(
                glbPath,
                "models",
                destination,
                GameTypeEnum.Warhammer3,
                true,
                false,
                false,
                false,
                false,
                20,
                NewSkeletonName: "ShouldBeIgnored");

            importer.Import(settings);

            Assert.Multiple(() =>
            {
                Assert.That(
                    destination.FileList.Keys,
                    Does.Contain($"models\\{Path.GetFileNameWithoutExtension(glbPath)}.rigid_model_v2".ToLowerInvariant()));
                Assert.That(
                    destination.FileList.Keys,
                    Does.Not.Contain("animations\\skeletons\\test_skeleton.anim"));
                Assert.That(
                    destination.FileList.Keys,
                    Does.Not.Contain("animations\\skeletons\\shouldbeignored.anim"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    private static GltfImporterSettings CreateSettings() => new(
        "scene.gltf",
        "models",
        PackFileContainer.CreatePackFile("test"),
        GameTypeEnum.Warhammer3,
        true,
        false,
        false,
        false,
        false,
        20);

    private static GltfImporterSettings CreateSettings(
        string inputFile,
        PackFileContainer destination) => new(
        inputFile,
        "models",
        destination,
        GameTypeEnum.Warhammer3,
        true,
        false,
        false,
        false,
        false,
        20);

    private static ModelRoot CreateSkinnedModelRoot(string? skeletonName)
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("mesh");
        AddTriangle(geometry.UsePrimitive(CreateMaterial("material")), 0);
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        if (skeletonName != null)
            scene.CreateNode($"//skeleton//{skeletonName}");
        var root = scene.CreateNode("root");
        var child = root.CreateNode("child");
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, Matrix4x4.Identity),
            (child, Matrix4x4.Identity));
        return modelRoot;
    }

    private static ModelRoot CreateMultipleSkinnedModelRoot(
        bool equivalentHierarchy,
        string secondChildName = "child",
        float secondChildTranslationY = 0)
    {
        var geometry = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>("mesh");
        AddTriangle(geometry.UsePrimitive(CreateMaterial("material")), 0);
        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");

        var firstRoot = scene.CreateNode("root");
        var firstChild = firstRoot.CreateNode("child");
        scene.CreateNode("first_mesh").WithSkinnedMesh(
            mesh,
            (firstRoot, Matrix4x4.Identity),
            (firstChild, Matrix4x4.Identity));
        modelRoot.LogicalSkins.Last().Name = "ExternalArmature";

        var secondArmature = scene.CreateNode("second_armature");
        var secondRoot = secondArmature.CreateNode("root");
        var secondChild = equivalentHierarchy
            ? secondRoot.CreateNode(secondChildName)
            : secondArmature.CreateNode(secondChildName);
        secondChild.LocalMatrix = Matrix4x4.CreateTranslation(
            0,
            secondChildTranslationY,
            0);
        scene.CreateNode("second_mesh").WithSkinnedMesh(
            mesh,
            (secondRoot, Matrix4x4.Identity),
            (secondChild, Matrix4x4.CreateTranslation(
                0,
                -secondChildTranslationY,
                0)));
        modelRoot.LogicalSkins.Last().Name = "ExternalArmatureCopy";

        return modelRoot;
    }

    private static AnimationFile CreateSkeletonFile(string skeletonName)
    {
        var frame = new AnimationFile.Frame
        {
            Transforms = [new RmvVector3(0, 0, 0), new RmvVector3(0, 1, 0)],
            Quaternion = [new RmvVector4(0, 0, 0, 1), new RmvVector4(0, 0, 0, 1)],
        };
        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                SkeletonName = skeletonName,
                AnimationTotalPlayTimeInSec = 0.1f,
            },
            Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, ParentId = -1, Name = "root" },
                new AnimationFile.BoneInfo { Id = 1, ParentId = 0, Name = "child" },
            ],
            AnimationParts =
            [
                new AnimationFile.AnimationPart
                {
                    DynamicFrames = [frame, frame],
                },
            ],
        };
    }

    private static AnimationFile CreateSkeletonFile(
        string skeletonName,
        IReadOnlyList<string?> boneNames)
    {
        var frame = new AnimationFile.Frame
        {
            Transforms = boneNames.Select(_ => new RmvVector3(0, 0, 0)).ToList(),
            Quaternion = boneNames.Select(_ => new RmvVector4(0, 0, 0, 1)).ToList(),
        };
        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                SkeletonName = skeletonName,
                AnimationTotalPlayTimeInSec = 0.1f,
            },
            Bones = boneNames
                .Select((name, index) => new AnimationFile.BoneInfo
                {
                    Id = index,
                    ParentId = -1,
                    Name = name!,
                })
                .ToArray(),
            AnimationParts =
            [
                new AnimationFile.AnimationPart
                {
                    DynamicFrames = [frame, frame],
                },
            ],
        };
    }

    private static MaterialBuilder CreateMaterial(string name) =>
        new MaterialBuilder(name).WithMetallicRoughness();

    private static void AddTriangle(
        IPrimitiveBuilder primitive,
        float xOffset)
    {
        primitive.AddTriangle(
            CreateVertex(xOffset, 0),
            CreateVertex(xOffset + 1, 0),
            CreateVertex(xOffset, 1));
    }

    private static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> CreateVertex(
        float x,
        float y,
        float tangentHandedness = 1)
    {
        var vertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, tangentHandedness);
        vertex.Material.TexCoord = new Vector2(x, y);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }

    private static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints8> CreateEightInfluenceVertex(
        float x,
        float y,
        (int, float)[] bindings)
    {
        var vertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints8>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new Vector2(x, y);
        vertex.Skinning.SetBindings(bindings);
        return vertex;
    }

    private static VertexBuilder<VertexPosition, VertexEmpty, VertexEmpty> CreatePositionOnlyVertex(
        float x,
        float y)
    {
        var vertex = new VertexBuilder<VertexPosition, VertexEmpty, VertexEmpty>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        return vertex;
    }

    private static bool IsFiniteUnitVector(XNA.Vector3 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z) &&
        Math.Abs(value.LengthSquared() - 1) < 0.0001f;
}
