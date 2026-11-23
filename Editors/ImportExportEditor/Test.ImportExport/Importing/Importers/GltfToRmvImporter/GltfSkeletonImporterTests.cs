using System.Numerics;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Shared.Core.Services;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class GltfSkeletonImporterTests
{
    [Test]
    public void Build_PreservesJointOrderHierarchyAndBindPose()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var root = scene.CreateNode("root");
        root.LocalMatrix =
            Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.25f)) *
            Matrix4x4.CreateTranslation(-1, 2, 3);
        var child = root.CreateNode("child");
        child.LocalMatrix = Matrix4x4.CreateTranslation(-4, 5, 6);
        modelRoot.CreateSkin("test_skin").BindJoints(Matrix4x4.Identity, root, child);

        var result = GltfSkeletonImporter.Build(
            modelRoot,
            "test_skeleton",
            mirrorMesh: true);

        var frame = result.AnimationParts[0].DynamicFrames[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Header.SkeletonName, Is.EqualTo("test_skeleton"));
            Assert.That(result.Bones.Select(bone => bone.Name), Is.EqualTo(new[] { "root", "child" }));
            Assert.That(result.Bones.Select(bone => bone.ParentId), Is.EqualTo(new[] { -1, 0 }));
            Assert.That(result.AnimationParts[0].DynamicFrames, Has.Count.EqualTo(2));
            Assert.That(frame.Transforms[0].X, Is.EqualTo(1).Within(0.0001f));
            Assert.That(frame.Transforms[0].Y, Is.EqualTo(2).Within(0.0001f));
            Assert.That(frame.Transforms[0].Z, Is.EqualTo(3).Within(0.0001f));
            Assert.That(frame.Transforms[1].X, Is.EqualTo(4).Within(0.0001f));
            Assert.That(frame.Transforms[1].Y, Is.EqualTo(5).Within(0.0001f));
            Assert.That(frame.Transforms[1].Z, Is.EqualTo(6).Within(0.0001f));
        });
    }

    [Test]
    public void Build_MultipleLogicalSkeletons_ThrowsInsteadOfPickingLargestSkin()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var firstRoot = scene.CreateNode("first_root");
        var secondRoot = scene.CreateNode("second_root");
        modelRoot.CreateSkin("first").BindJoints(
            Matrix4x4.Identity,
            firstRoot);
        modelRoot.CreateSkin("second").BindJoints(
            Matrix4x4.Identity,
            secondRoot);

        Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.Build(
                modelRoot,
                "test_skeleton",
                mirrorMesh: true));
    }

    [Test]
    public void BuildExternal_ArmatureAncestorScale_IsBakedIntoBoneTranslations()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("Armature");
        armature.LocalMatrix = Matrix4x4.CreateScale(2);
        var root = armature.CreateNode("root");
        root.LocalMatrix = Matrix4x4.CreateTranslation(0, 1, 0);
        var child = root.CreateNode("child");
        child.LocalMatrix = Matrix4x4.CreateTranslation(0, 2, 0);
        var skin = modelRoot.CreateSkin("Armature");
        skin.Skeleton = armature;
        skin.BindJoints(Matrix4x4.Identity, root, child);

        var result = GltfSkeletonImporter.BuildExternal(
            modelRoot,
            "external.glb",
            skeletonName: null,
            mirrorMesh: true);

        var frame = result.AnimationParts[0].DynamicFrames[0];
        Assert.Multiple(() =>
        {
            Assert.That(frame.Transforms[0].Y, Is.EqualTo(2).Within(0.0001f));
            Assert.That(frame.Transforms[1].Y, Is.EqualTo(4).Within(0.0001f));
        });
    }

    [Test]
    public void BuildExternal_BoneScale_ReturnsReadableError()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var root = scene.CreateNode("root");
        root.LocalMatrix = Matrix4x4.CreateScale(2);
        modelRoot.CreateSkin("Armature").BindJoints(Matrix4x4.Identity, root);

        var exception = Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.BuildExternal(
                modelRoot,
                "external.glb",
                skeletonName: null,
                mirrorMesh: true));

        Assert.That(exception!.Message, Does.Contain("scale"));
    }

    [Test]
    public void BuildExternal_BoneShear_ReturnsReadableError()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var root = scene.CreateNode("root");
        var shear = Matrix4x4.Identity;
        shear.M12 = 0.25f;
        root.LocalMatrix = shear;
        modelRoot.CreateSkin("Armature").BindJoints(Matrix4x4.Identity, root);

        var exception = Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.BuildExternal(
                modelRoot,
                "external.glb",
                skeletonName: null,
                mirrorMesh: true));

        Assert.That(exception!.Message, Does.Contain("shear"));
    }

    [Test]
    public void BuildExternal_EquivalentSecondarySkinWithBoneScale_ReturnsReadableError()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var firstRoot = scene.CreateNode("root");
        var firstChild = firstRoot.CreateNode("child");
        modelRoot.CreateSkin("FirstArmature").BindJoints(
            Matrix4x4.Identity,
            firstRoot,
            firstChild);

        var secondRoot = scene.CreateNode("root");
        var secondChild = secondRoot.CreateNode("child");
        secondChild.LocalMatrix = Matrix4x4.CreateScale(2);
        modelRoot.CreateSkin("SecondArmature").BindJoints(
            Matrix4x4.Identity,
            secondRoot,
            secondChild);

        var exception = Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.BuildExternal(
                modelRoot,
                "external.glb",
                skeletonName: null,
                mirrorMesh: true));

        Assert.That(exception!.Message, Does.Contain("scale"));
    }

    [Test]
    public void BuildExternal_WithoutSkinOrArmatureName_UsesSourceFileName()
    {
        var modelRoot = ModelRoot.CreateModel();
        var root = modelRoot.UseScene("default").CreateNode("root");
        var skin = modelRoot.CreateSkin();
        skin.BindJoints(Matrix4x4.Identity, root);
        skin.Skeleton = null;

        var result = GltfSkeletonImporter.BuildExternal(
            modelRoot,
            @"C:\assets\hero_source.glb",
            skeletonName: null,
            mirrorMesh: true);

        Assert.That(result.Header.SkeletonName, Is.EqualTo("hero_source"));
    }

    [Test]
    public void BuildExternal_WithoutSkinName_UsesArmatureName()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ExternalArmature");
        var root = armature.CreateNode("root");
        var skin = modelRoot.CreateSkin();
        skin.Skeleton = armature;
        skin.BindJoints(Matrix4x4.Identity, root);

        var result = GltfSkeletonImporter.BuildExternal(
            modelRoot,
            "hero_source.glb",
            skeletonName: null,
            mirrorMesh: true);

        Assert.That(result.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
    }

    [Test]
    public void BuildExternal_WithoutSkinSkeletonProperty_UsesNamedJointAncestor()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ExternalArmature");
        var root = armature.CreateNode("root");
        var skin = modelRoot.CreateSkin();
        skin.BindJoints(Matrix4x4.Identity, root);
        skin.Skeleton = null;

        var result = GltfSkeletonImporter.BuildExternal(
            modelRoot,
            "hero_source.glb",
            skeletonName: null,
            mirrorMesh: true);

        Assert.That(result.Header.SkeletonName, Is.EqualTo("ExternalArmature"));
    }

    [Test]
    public void BuildExternal_CaseInsensitiveDuplicateBoneNames_ReturnsReadableError()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var first = scene.CreateNode("Bone");
        var second = first.CreateNode("bone");
        modelRoot.CreateSkin("Armature").BindJoints(
            Matrix4x4.Identity,
            first,
            second);

        var exception = Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.BuildExternal(
                modelRoot,
                "external.glb",
                skeletonName: null,
                mirrorMesh: true));

        Assert.That(exception!.Message, Does.Contain("differ only by case"));
    }

    [Test]
    public void BuildExternal_MoreThan256Bones_ReturnsReadableError()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var joints = new List<Node>();
        var parent = scene.CreateNode("bone_0");
        joints.Add(parent);
        for (var boneIndex = 1; boneIndex < 257; boneIndex++)
        {
            parent = parent.CreateNode($"bone_{boneIndex}");
            joints.Add(parent);
        }
        modelRoot.CreateSkin("Armature").BindJoints(
            Matrix4x4.Identity,
            joints.ToArray());

        var exception = Assert.Throws<InvalidDataException>(() =>
            GltfSkeletonImporter.BuildExternal(
                modelRoot,
                "external.glb",
                skeletonName: null,
                mirrorMesh: true));

        Assert.That(exception!.Message, Does.Contain("256 bones"));
    }

}
