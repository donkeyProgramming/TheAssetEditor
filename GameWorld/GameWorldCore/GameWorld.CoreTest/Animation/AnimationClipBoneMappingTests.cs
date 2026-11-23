using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

// Ported from Szy-Cathay/TheAssetEditor-CN a55a3c253 (tracks matched by bone name; the fork's hierarchy rebasing is not ported)
namespace GameWorld.Core.Test.Animation;

[TestFixture]
internal class AnimationClipBoneMappingTests
{
    [Test]
    public void Constructor_ReorderedBoneTable_UsesBoneNamesForTranslationsAndRotations()
    {
        var source = CreateFile("root", "left", "right");
        var target = CreateFile("root", "right", "left");
        var skeleton = new GameSkeleton(target, new AnimationPlayer());

        var clip = new AnimationClip(source, skeleton);

        Assert.Multiple(() =>
        {
            Assert.That(clip.DynamicFrames[0].Position[1], Is.EqualTo(new Vector3(3, 6, 9)));
            Assert.That(clip.DynamicFrames[0].Position[2], Is.EqualTo(new Vector3(2, 4, 6)));
            Assert.That(clip.DynamicFrames[0].Rotation[1], Is.EqualTo(ToQuaternion(source.AnimationParts[0].DynamicFrames[0].Quaternion[2])));
            Assert.That(clip.DynamicFrames[0].Rotation[2], Is.EqualTo(ToQuaternion(source.AnimationParts[0].DynamicFrames[0].Quaternion[1])));
        });
    }

    [Test]
    public void Constructor_ExtraSourceBone_DoesNotShiftRemainingTracks()
    {
        var source = CreateFile("root", "accessory", "left", "right");
        var skeleton = new GameSkeleton(CreateFile("root", "left", "right"), new AnimationPlayer());

        var clip = new AnimationClip(source, skeleton);

        Assert.Multiple(() =>
        {
            Assert.That(clip.DynamicFrames[0].Position, Has.Count.EqualTo(3));
            Assert.That(clip.DynamicFrames[0].Position[1], Is.EqualTo(new Vector3(3, 6, 9)));
            Assert.That(clip.DynamicFrames[0].Position[2], Is.EqualTo(new Vector3(4, 8, 12)));
        });
    }

    [Test]
    public void Constructor_MissingTargetTrack_PreservesItsBindPose()
    {
        var source = CreateFile("root", "left");
        var skeleton = new GameSkeleton(CreateFile("root", "accessory", "left"), new AnimationPlayer());

        var clip = new AnimationClip(source, skeleton);

        Assert.Multiple(() =>
        {
            Assert.That(clip.DynamicFrames[0].Position, Has.Count.EqualTo(3));
            Assert.That(clip.DynamicFrames[0].Position[1], Is.EqualTo(skeleton.Translation[1]));
            Assert.That(clip.DynamicFrames[0].Rotation[1], Is.EqualTo(skeleton.Rotation[1]));
            Assert.That(clip.DynamicFrames[0].Position[2], Is.EqualTo(new Vector3(2, 4, 6)));
        });
    }

    [Test]
    public void Constructor_ReorderedStaticTracks_UsesBoneNames()
    {
        var source = CreateFile("root", "left", "right");
        var part = source.AnimationParts[0];
        part.StaticFrame = part.DynamicFrames[0];
        for (var index = 0; index < source.Bones.Length; index++)
        {
            part.TranslationMappings[index] = new AnimationFile.AnimationBoneMapping(10000 + index);
            part.RotationMappings[index] = new AnimationFile.AnimationBoneMapping(10000 + index);
        }
        part.DynamicFrames.Clear();
        var skeleton = new GameSkeleton(CreateFile("root", "right", "left"), new AnimationPlayer());

        var clip = new AnimationClip(source, skeleton);

        Assert.Multiple(() =>
        {
            Assert.That(clip.DynamicFrames[0].Position[1], Is.EqualTo(new Vector3(3, 6, 9)));
            Assert.That(clip.DynamicFrames[0].Position[2], Is.EqualTo(new Vector3(2, 4, 6)));
        });
    }

    [Test]
    public void Constructor_UnnamedBoneTable_PreservesIndexBasedTracks()
    {
        var source = CreateFile("", "");
        var skeleton = new GameSkeleton(CreateFile("root", "left"), new AnimationPlayer());

        var clip = new AnimationClip(source, skeleton);

        Assert.That(clip.DynamicFrames[0].Position,
            Is.EqualTo(new[] { new Vector3(1, 2, 3), new Vector3(2, 4, 6) }));
    }

    private static AnimationFile CreateFile(params string[] names)
    {
        var file = new AnimationFile
        {
            Bones = names.Select((name, index) => new AnimationFile.BoneInfo
            {
                Id = index,
                Name = name,
                ParentId = index == 0 ? -1 : 0,
            }).ToArray(),
        };
        file.Header.SkeletonName = "shared";
        file.Header.AnimationTotalPlayTimeInSec = 1;
        var part = new AnimationFile.AnimationPart();
        var frame = new AnimationFile.Frame();
        for (var index = 0; index < names.Length; index++)
        {
            frame.Transforms.Add(new RmvVector3(index + 1, (index + 1) * 2, (index + 1) * 3));
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, index * 0.3f);
            frame.Quaternion.Add(new RmvVector4(rotation.X, rotation.Y, rotation.Z, rotation.W));
            part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(index));
            part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(index));
        }
        part.DynamicFrames.Add(frame);
        file.AnimationParts.Add(part);
        return file;
    }

    private static Quaternion ToQuaternion(RmvVector4 value) => new(value.X, value.Y, value.Z, value.W);
}
