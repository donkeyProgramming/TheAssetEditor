using Editors.AnimationMeta.SuperView.Editing;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Test.AnimationMeta;

[TestFixture]
internal class SpatialMetaDataCatalogTests
{
    [TestCaseSource(nameof(SupportedSpatialMetaData))]
    public void SupportedSpatialMetaData_UsesEditablePositionAndExpectedRotation(
        Type sourceType,
        SpatialMetaDataKind expectedKind,
        bool expectedCanRotate)
    {
        var source = (ParsedMetadataAttribute)Activator.CreateInstance(sourceType)!;

        Assert.That(
            SpatialMetaDataCatalog.TryCreate(source, out var binding),
            Is.True,
            sourceType.Name);

        var position = new Vector3(2, 3, 4);
        binding.Position = position;
        Assert.Multiple(() =>
        {
            Assert.That(binding.Kind, Is.EqualTo(expectedKind));
            Assert.That(binding.Position, Is.EqualTo(position));
            Assert.That(binding.CanRotate, Is.EqualTo(expectedCanRotate));
        });

        if (!expectedCanRotate)
            return;

        var orientation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitY,
            MathHelper.PiOver4);
        binding.Orientation = orientation;
        AssertQuaternion(binding.Orientation!.Value, orientation);
    }

    [TestCaseSource(nameof(UnsupportedSpatialMetaData))]
    public void UnverifiedSpatialMetaData_DoesNotExposeWritableGizmo(
        ParsedMetadataAttribute source)
    {
        Assert.That(
            SpatialMetaDataCatalog.TryCreate(source, out _),
            Is.False,
            source.GetType().Name);
    }

    [Test]
    public void TrackedBlood_UsesBoneBindingWhileUntrackedBloodUsesRootBinding()
    {
        var tracked = new Blood_v12 { Tracking = true };
        var untracked = new Blood_v12 { Tracking = false };

        Assert.That(
            SpatialMetaDataCatalog.TryCreate(tracked, out var trackedBinding),
            Is.True);
        Assert.That(
            SpatialMetaDataCatalog.TryCreate(untracked, out var untrackedBinding),
            Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(trackedBinding.AttachToBone, Is.True);
            Assert.That(untrackedBinding.AttachToBone, Is.False);
        });
    }

    private static IEnumerable<TestCaseData> SupportedSpatialMetaData()
    {
        foreach (var type in new[]
        {
            typeof(Effect_v1),
            typeof(Effect_v2),
            typeof(Effect_v3),
            typeof(Effect_v4),
            typeof(Effect_v5),
            typeof(Effect_v6),
            typeof(Effect_v7),
            typeof(Effect_v8),
            typeof(Effect_v9),
            typeof(Effect_v10),
            typeof(Effect_v11),
            typeof(Effect_v12),
        })
        {
            yield return SpatialCase(
                type,
                SpatialMetaDataKind.Effect,
                true);
        }

        foreach (var type in new[]
        {
            typeof(Prop_v2),
            typeof(Prop_v3),
            typeof(Prop_v4),
            typeof(Prop_v10),
            typeof(Prop_v11),
            typeof(Prop_v12),
            typeof(Prop_v13),
            typeof(Prop_v14),
            typeof(Prop_v15),
            typeof(Prop_v12_3K),
            typeof(Prop_v13_3K),
            typeof(AnimatedProp_v0),
            typeof(AnimatedProp_v2),
            typeof(AnimatedProp_v3),
            typeof(AnimatedProp_v4),
            typeof(AnimatedProp_v10),
            typeof(AnimatedProp_v11),
            typeof(AnimatedProp_v12),
            typeof(AnimatedProp_v13),
            typeof(AnimatedProp_v14),
            typeof(AnimatedProp_v15),
            typeof(AnimatedProp_v12_3K),
            typeof(AnimatedProp_v13_3K),
        })
        {
            yield return SpatialCase(
                type,
                SpatialMetaDataKind.Prop,
                true);
        }

        foreach (var type in new[]
        {
            typeof(Blood_v5),
            typeof(Blood_v11),
            typeof(Blood_v12),
        })
        {
            yield return SpatialCase(
                type,
                SpatialMetaDataKind.Blood,
                true);
        }

        yield return SpatialCase(
            typeof(CameraShakePos),
            SpatialMetaDataKind.CameraShake,
            false);
        yield return SpatialCase(
            typeof(CrewLocation_v2),
            SpatialMetaDataKind.CrewLocation,
            true);
        yield return SpatialCase(
            typeof(CrewLocation_v3),
            SpatialMetaDataKind.CrewLocation,
            true);
        yield return SpatialCase(
            typeof(CrewLocation_v10),
            SpatialMetaDataKind.CrewLocation,
            true);
        yield return SpatialCase(
            typeof(SoundTrigger_v4),
            SpatialMetaDataKind.SoundTrigger,
            false);
        yield return SpatialCase(
            typeof(SoundTrigger_v10),
            SpatialMetaDataKind.SoundTrigger,
            false);
        yield return SpatialCase(
            typeof(SoundTrigger_v11),
            SpatialMetaDataKind.SoundTrigger,
            false);
        yield return SpatialCase(
            typeof(SoundBuilding_v2),
            SpatialMetaDataKind.SoundBuilding,
            false);
        yield return SpatialCase(
            typeof(Transform_v10),
            SpatialMetaDataKind.Transform,
            true);
    }

    private static IEnumerable<ParsedMetadataAttribute> UnsupportedSpatialMetaData()
    {
        yield return new Position_v10();
        yield return new SoundPosition_v10();
        yield return new SoundSphere_v10();
        yield return new FreezeWeapon_v10();
    }

    private static TestCaseData SpatialCase(
        Type sourceType,
        SpatialMetaDataKind kind,
        bool canRotate) =>
        new(sourceType, kind, canRotate)
        {
            TestName = $"{sourceType.Name}_SpatialBinding"
        };

    private static void AssertQuaternion(
        Quaternion actual,
        Quaternion expected)
    {
        Assert.That(
            Math.Abs(Quaternion.Dot(actual, expected)),
            Is.EqualTo(1).Within(0.0001f));
    }
}
