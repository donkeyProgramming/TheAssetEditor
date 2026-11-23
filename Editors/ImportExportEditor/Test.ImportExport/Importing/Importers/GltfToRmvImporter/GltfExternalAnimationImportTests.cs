using System.Numerics;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.Animation;
using Shared.TestUtility;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfExternalAnimationImportTests
{
    [Test]
    public void Import_ExternalSkeletonWithMultiplePartialAnimations_CreatesEveryAnim()
    {
        var glbPath = CreateExternalSkinnedGlb((modelRoot, _, root, child) =>
        {
            modelRoot.CreateAnimation("Idle Pose").CreateRotationChannel(
                root,
                new Dictionary<float, Quaternion>
                {
                    [0.0f / 24.0f] = Quaternion.Identity,
                    [1.0f / 24.0f] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.1f),
                    [2.0f / 24.0f] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.2f),
                });
            modelRoot.CreateAnimation("Walk/Forward").CreateTranslationChannel(
                child,
                new Dictionary<float, Vector3>
                {
                    [0.00f] = Vector3.UnitY,
                    [0.07f] = Vector3.UnitY * 2,
                });
        });

        try
        {
            var innerPackFileService = PackFileSerivceTestHelper.Create(TestData.InputPack);
            var destination = PackFileContainer.CreatePackFile("test");
            var packFileService = new Mock<IPackFileService>();
            packFileService
                .Setup(service => service.AddFilesToPack(
                    destination,
                    It.IsAny<List<NewPackFileEntry>>()))
                .Callback<IPackFileContainer, List<NewPackFileEntry>>((container, entries) =>
                        innerPackFileService.AddFilesToPack(
                            container,
                            entries));
            var importer = new GltfImporter(
                packFileService.Object,
                Mock.Of<ISkeletonAnimationLookUpHelper>(),
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(glbPath, destination));

            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();
            var idlePath = $@"animations\{baseName}_idle_pose.anim";
            var walkPath = $@"animations\{baseName}_walk_forward.anim";
            var idle = AnimationFile.Create(destination.FileList[idlePath]);
            var walk = AnimationFile.Create(destination.FileList[walkPath]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(result.OutputPaths, Is.EquivalentTo(destination.FileList.Keys));
                Assert.That(destination.FileList.Keys, Does.Contain(
                    @"animations\skeletons\externalarmature.anim"));
                Assert.That(destination.FileList.Keys, Does.Contain(idlePath));
                Assert.That(destination.FileList.Keys, Does.Contain(walkPath));
                Assert.That(destination.FileList.Keys.Count(path => path.EndsWith(".anim")), Is.EqualTo(3));
                Assert.That(idle.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
                Assert.That(walk.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
                Assert.That(idle.Header.FrameRate, Is.EqualTo(24.0f).Within(0.001f));
                Assert.That(walk.Header.FrameRate, Is.EqualTo(20.0f).Within(0.001f));
                Assert.That(idle.AnimationParts[0].DynamicFrames[0].Transforms[0].X, Is.Zero.Within(0.0001f));
                Assert.That(idle.AnimationParts[0].DynamicFrames[0].Transforms[1].Y, Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(walk.AnimationParts[0].DynamicFrames[0].Quaternion[0].W, Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(walk.AnimationParts[0].DynamicFrames[0].Transforms[1].X, Is.Zero.Within(0.0001f));
                Assert.That(walk.AnimationParts[0].DynamicFrames[0].Quaternion[1].X, Is.Zero.Within(0.0001f));
                Assert.That(walk.AnimationParts[0].DynamicFrames[0].Quaternion[1].W, Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(
                    Math.Abs(idle.AnimationParts[0].DynamicFrames[^1].Quaternion[0].Z),
                    Is.GreaterThan(0.05f));
                Assert.That(
                    walk.AnimationParts[0].DynamicFrames[^1].Transforms[1].Y,
                    Is.GreaterThan(1.5f));
            });
            packFileService.Verify(
                service => service.AddFilesToPack(
                    destination,
                    It.IsAny<List<NewPackFileEntry>>()),
                Times.Once);
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExternalSkeletonWithSingleAnimation_UsesSourceAndActionNames()
    {
        var glbPath = CreateExternalSkinnedGlb((modelRoot, _, root, _) =>
        {
            modelRoot.CreateAnimation("Idle Pose").CreateTranslationChannel(
                root,
                new Dictionary<float, Vector3>
                {
                    [0.00f] = Vector3.Zero,
                    [0.05f] = Vector3.UnitY,
                });
        });

        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var result = CreateImporter().Import(CreateSettings(glbPath, destination));
            var baseName = Path.GetFileNameWithoutExtension(glbPath).ToLowerInvariant();

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, string.Join(Environment.NewLine, result.Errors));
                Assert.That(
                    destination.FileList.Keys,
                    Does.Contain($@"animations\{baseName}_idle_pose.anim"));
                Assert.That(
                    destination.FileList.Keys,
                    Does.Not.Contain($@"animations\{baseName}.anim"));
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_ExternalAnimationWithBoneScale_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalSkinnedGlb((modelRoot, _, root, _) =>
        {
            modelRoot.CreateAnimation("Scaled Action").CreateScaleChannel(
                root,
                new Dictionary<float, Vector3>
                {
                    [0.00f] = Vector3.One,
                    [0.05f] = new Vector3(1.00015f, 1, 1),
                });
        });

        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var importer = CreateImporter();

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Scaled Action"));
                Assert.That(result.Errors, Has.Some.Contains("root"));
                Assert.That(result.Errors, Has.Some.Contains("0.05"));
                Assert.That(result.Errors, Has.Some.Contains("scale"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [TestCase(".gltf", false)]
    [TestCase(".glb", false)]
    [TestCase(".gltf", true)]
    [TestCase(".glb", true)]
    public void Import_BlenderRoundTripWithUnitScaleKeys_CreatesAnimation(
        string extension,
        bool diagonalBindRotation)
    {
        var fixture = CreateExistingGameSkeletonRoundTrip(extension, diagonalBindRotation);

        try
        {
            var destination = PackFileContainer.CreatePackFile("test");
            var skeletonLookup = new Mock<ISkeletonAnimationLookUpHelper>();
            skeletonLookup
                .Setup(lookup => lookup.GetSkeletonFileFromName("test_skeleton"))
                .Returns(fixture.Skeleton);
            var importer = new GltfImporter(
                PackFileSerivceTestHelper.Create(TestData.InputPack),
                skeletonLookup.Object,
                new RmvMaterialBuilder());

            var result = importer.Import(CreateSettings(fixture.Path, destination));

            Assert.That(
                result.Succeeded,
                Is.True,
                result.Exception?.ToString() ??
                string.Join(Environment.NewLine, result.Errors));
            var animationPath = result.OutputPaths.Single(path =>
                path.EndsWith(
                    "_blender_roundtrip.anim",
                    StringComparison.OrdinalIgnoreCase));
            var imported = AnimationFile.Create(destination.FileList[animationPath]);
            var frames = imported.AnimationParts[0].DynamicFrames;
            var firstFrame = frames[0];
            var rotation = firstFrame.Quaternion[0];
            var rotationLength = MathF.Sqrt(
                rotation.X * rotation.X +
                rotation.Y * rotation.Y +
                rotation.Z * rotation.Z +
                rotation.W * rotation.W);
            var storedRotation = fixture.Skeleton.AnimationParts[0]
                .DynamicFrames[0].Quaternion[0];
            var expectedRotation = Quaternion.Normalize(new Quaternion(
                storedRotation.X,
                storedRotation.Y,
                storedRotation.Z,
                storedRotation.W));
            var actualRotation = Quaternion.Normalize(new Quaternion(
                rotation.X,
                rotation.Y,
                rotation.Z,
                rotation.W));
            Assert.Multiple(() =>
            {
                Assert.That(rotationLength, Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(
                    MathF.Abs(Quaternion.Dot(expectedRotation, actualRotation)),
                    Is.GreaterThan(0.9999f));
                foreach (var frame in frames)
                {
                    Assert.That(frame.Transforms[0].X, Is.Zero.Within(0.000001f));
                    Assert.That(frame.Transforms[0].Y, Is.Zero.Within(0.000001f));
                    Assert.That(frame.Transforms[0].Z, Is.Zero.Within(0.000001f));
                }
                Assert.That(
                    result.OutputPaths,
                    Does.Not.Contain(@"animations\skeletons\test_skeleton.anim"));
            });
        }
        finally
        {
            Directory.Delete(fixture.Directory, true);
        }
    }

    [Test]
    public void Import_ExternalAnimationTargetsNodeOutsideSkeleton_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalSkinnedGlb((modelRoot, armature, _, _) =>
        {
            var unrelatedNode = armature.CreateNode("camera_rig");
            modelRoot.CreateAnimation("Camera Motion").CreateTranslationChannel(
                unrelatedNode,
                new Dictionary<float, Vector3>
                {
                    [0.00f] = Vector3.Zero,
                    [0.05f] = Vector3.UnitX,
                });
        });

        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);
            var importer = CreateImporter();

            var result = importer.Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("Camera Motion"));
                Assert.That(result.Errors, Has.Some.Contains("camera_rig"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    [Test]
    public void Import_AnimationNamesCollideAfterSanitizing_ReturnsFailureAndLeavesPackUnchanged()
    {
        var glbPath = CreateExternalSkinnedGlb((modelRoot, _, root, _) =>
        {
            foreach (var name in new[] { "Walk/Forward", @"Walk\Forward" })
            {
                modelRoot.CreateAnimation(name).CreateTranslationChannel(
                    root,
                    new Dictionary<float, Vector3>
                    {
                        [0.00f] = Vector3.Zero,
                        [0.05f] = Vector3.UnitY,
                    });
            }
        });

        try
        {
            var destination = CreateDestinationWithExistingFile(out var existingFile);

            var result = CreateImporter().Import(CreateSettings(glbPath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Errors, Has.Some.Contains("more than once"));
                AssertDestinationUnchanged(destination, existingFile);
            });
        }
        finally
        {
            File.Delete(glbPath);
        }
    }

    private static GltfImporter CreateImporter() => new(
        PackFileSerivceTestHelper.Create(TestData.InputPack),
        Mock.Of<ISkeletonAnimationLookUpHelper>(),
        new RmvMaterialBuilder());

    private static GltfImporterSettings CreateSettings(
        string glbPath,
        PackFileContainer destination) => new(
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

    private static PackFileContainer CreateDestinationWithExistingFile(
        out PackFile existingFile)
    {
        var destination = PackFileContainer.CreatePackFile("test");
        existingFile = new PackFile(
            "existing.bin",
            new MemorySource([7, 8, 9]));
        destination.FileList[@"animations\existing.bin"] = existingFile;
        return destination;
    }

    private static void AssertDestinationUnchanged(
        PackFileContainer destination,
        PackFile existingFile)
    {
        Assert.That(destination.FileList, Has.Count.EqualTo(1));
        Assert.That(destination.FileList[@"animations\existing.bin"], Is.SameAs(existingFile));
        Assert.That(
            existingFile.DataSource.ReadData(),
            Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    private static string CreateExternalSkinnedGlb(
        Action<ModelRoot, Node, Node, Node> createAnimations)
    {
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        var primitive = geometry.UsePrimitive(
            new MaterialBuilder("material").WithMetallicRoughness());
        primitive.AddTriangle(
            CreateVertex(0, 0),
            CreateVertex(1, 0),
            CreateVertex(0, 1));

        var modelRoot = ModelRoot.CreateModel();
        var mesh = modelRoot.CreateMesh(geometry);
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ExternalArmature");
        var root = armature.CreateNode("root");
        var child = root.CreateNode("child");
        child.LocalMatrix = Matrix4x4.CreateTranslation(Vector3.UnitY);
        Assert.That(Matrix4x4.Invert(root.WorldMatrix, out var rootInverseBind), Is.True);
        Assert.That(Matrix4x4.Invert(child.WorldMatrix, out var childInverseBind), Is.True);
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            mesh,
            (root, rootInverseBind),
            (child, childInverseBind));
        modelRoot.LogicalSkins.Single().Name = "ExternalArmature";
        root.LocalMatrix = Matrix4x4.CreateTranslation(5, 0, 0);
        child.LocalMatrix =
            Matrix4x4.CreateRotationX(0.35f) *
            Matrix4x4.CreateTranslation(Vector3.UnitY);
        createAnimations(modelRoot, armature, root, child);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        modelRoot.SaveGLB(path);
        return path;
    }

    private static (string Directory, string Path, AnimationFile Skeleton)
        CreateExistingGameSkeletonRoundTrip(string extension, bool diagonalBindRotation)
    {
        var geometry = new MeshBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>("mesh");
        geometry.UsePrimitive(
                new MaterialBuilder("material").WithMetallicRoughness())
            .AddTriangle(
                CreateVertex(0, 0),
                CreateVertex(1, 0),
                CreateVertex(0, 1));
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        scene.CreateNode("//skeleton//test_skeleton");
        var joint = scene.CreateNode("root");
        scene.CreateNode("mesh_node").WithSkinnedMesh(
            modelRoot.CreateMesh(geometry),
            (joint, Matrix4x4.Identity));
        var animation = modelRoot.CreateAnimation("Blender Roundtrip");
        animation.CreateScaleChannel(joint, new Dictionary<float, Vector3>
        {
            [0] = Vector3.One,
            [1] = new Vector3(0.9999999f, 1, 1),
        });
        animation.CreateRotationChannel(joint, new Dictionary<float, Quaternion>
        {
            [0] = Quaternion.Identity,
            [1] = Quaternion.Identity,
        });

        var directory = Path.Combine(
            Path.GetTempPath(),
            $"gltf_original_roundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"original_roundtrip{extension}");
        if (string.Equals(extension, ".gltf", StringComparison.OrdinalIgnoreCase))
            modelRoot.SaveGLTF(path);
        else
            modelRoot.SaveGLB(path);

        return (directory, path, CreateStoredGameSkeleton(diagonalBindRotation));
    }

    private static AnimationFile CreateStoredGameSkeleton(bool diagonalBindRotation)
    {
        var targetBindRotation = Quaternion.CreateFromAxisAngle(
            diagonalBindRotation ? Vector3.Normalize(new Vector3(1, 2, 3)) : Vector3.UnitZ,
            MathF.PI / 2.0f);
        const float storedQuaternionLength = 1.000078f;
        var frame = new AnimationFile.Frame
        {
            Transforms =
            [
                new Shared.GameFormats.RigidModel.Transforms.RmvVector3(0, 0, 0),
            ],
            Quaternion =
            [
                new Shared.GameFormats.RigidModel.Transforms.RmvVector4(
                    targetBindRotation.X * storedQuaternionLength,
                    targetBindRotation.Y * storedQuaternionLength,
                    targetBindRotation.Z * storedQuaternionLength,
                    targetBindRotation.W * storedQuaternionLength),
            ],
        };
        var part = new AnimationFile.AnimationPart
        {
            DynamicFrames = [frame],
        };
        part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(0));
        part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(0));

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                FrameRate = 20,
                SkeletonName = "test_skeleton",
            },
            Bones =
            [
                new AnimationFile.BoneInfo
                {
                    Id = 0,
                    ParentId = -1,
                    Name = "root",
                },
            ],
            AnimationParts = [part],
        };
    }

    private static VertexBuilder<
        VertexPositionNormalTangent,
        VertexTexture1,
        VertexJoints4> CreateVertex(float x, float y)
    {
        var vertex = new VertexBuilder<
            VertexPositionNormalTangent,
            VertexTexture1,
            VertexJoints4>();
        vertex.Geometry.Position = new Vector3(x, y, 0);
        vertex.Geometry.Normal = Vector3.UnitZ;
        vertex.Geometry.Tangent = new Vector4(1, 0, 0, 1);
        vertex.Material.TexCoord = new Vector2(x, y);
        vertex.Skinning.SetBindings((0, 1), (0, 0), (0, 0), (0, 0));
        return vertex;
    }
}
