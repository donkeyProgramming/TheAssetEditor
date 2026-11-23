using Editors.CscEditor.Data;
using Editors.CscEditor.ViewModels;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Esf;

namespace Test.CscEditor;

public class CscLegacyCameraTests
{
    [TestCase(1)]
    [TestCase(2)]
    public void Static_camera_edits_round_trip_without_upgrading_record_versions(int cameraVersion)
    {
        var scene = CscScene.Load(BuildStaticCamera(cameraVersion));
        var camera = scene.Elements.Single();
        var originalFields = camera.ElementRecord!.Children.ToArray();
        var originalTypeFields = camera.TypeRecord!.Children.ToArray();
        var viewModel = new CscElementViewModel(camera, (_, _) => { });

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsEditable, Is.True);
            Assert.That(viewModel.HasWeight, Is.False);
            Assert.That(viewModel.HasCameraRoll, Is.False);
            Assert.That(viewModel.HasCameraNear, Is.EqualTo(cameraVersion == 2));
            Assert.That(viewModel.CameraFov, Is.EqualTo(30));
            Assert.That(camera.AllChannelKeyframeCounts(), Is.Empty);
            Assert.That(camera.LocalTransform(0).Translation, Is.EqualTo(new Vector3(2, 3, 4)));
        });

        viewModel.PositionX = 7.25f;
        viewModel.CameraFov = 42;
        var reloaded = CscScene.Load(CscSceneWriter.Write(scene));
        var savedCamera = reloaded.Elements.Single();

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.RootVersion, Is.EqualTo(3));
            Assert.That(savedCamera.ElementVersion, Is.EqualTo(5));
            Assert.That(savedCamera.TypeRecord!.Version, Is.EqualTo(cameraVersion));
            Assert.That(savedCamera.EvaluatePosition(0), Is.EqualTo(new Vector3(7.25f, 3, 4)));
            Assert.That(savedCamera.CameraFov!.StaticValue, Is.EqualTo(42));
            Assert.That(savedCamera.ElementRecord!.Children.Skip(7).Select(n => n.Value),
                Is.EqualTo(originalFields.Skip(7).Select(n => n.Value)));
            Assert.That(savedCamera.TypeRecord!.Children.Skip(1).Select(n => n.Value),
                Is.EqualTo(originalTypeFields.Skip(1).Select(n => n.Value)));
            Assert.That(CscSceneWriter.Write(reloaded), Is.EqualTo(CscSceneWriter.Write(scene)));
        });
    }

    [Test]
    public void Static_quaternion_rotation_edits_keep_the_displayed_pose_after_reload()
    {
        var scene = CscScene.Load(BuildStaticCamera(1));
        var camera = scene.Elements.Single();
        var viewModel = new CscElementViewModel(camera, (_, _) => { });
        viewModel.RotationXDegrees = 32;
        viewModel.RotationYDegrees = -47;
        viewModel.RotationZDegrees = 18;
        var before = camera.LocalTransform(0);

        var reloaded = CscScene.Load(CscSceneWriter.Write(scene));
        var after = reloaded.Elements.Single().LocalTransform(0);

        foreach (var (expected, actual) in new[]
        {
            (before.Right, after.Right), (before.Up, after.Up),
            (before.Backward, after.Backward), (before.Translation, after.Translation),
        })
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.00001f));
    }

    [Test]
    public void Static_transform_rejects_keyframes_instead_of_silently_losing_them()
    {
        var scene = CscScene.Load(BuildStaticCamera(1));
        scene.Elements.Single().Position!.Channels[0].AddKeyframe(0, 3);

        Assert.Throws<InvalidDataException>(() => CscSceneWriter.Write(scene));
    }

    [Test]
    public void Unknown_legacy_layout_remains_read_only_and_preserves_raw_fields()
    {
        var scene = CscScene.Load(BuildStaticCamera(1, extraTransformField: true));
        var camera = scene.Elements.Single();
        var originalFields = camera.ElementRecord!.Children.Select(n => n.Value).ToArray();

        var reloaded = CscScene.Load(CscSceneWriter.Write(scene));

        Assert.Multiple(() =>
        {
            Assert.That(camera.IsLegacyRaw, Is.True);
            Assert.That(new CscElementViewModel(camera, (_, _) => { }).IsEditable, Is.False);
            Assert.That(reloaded.Elements.Single().ElementRecord!.Children.Select(n => n.Value), Is.EqualTo(originalFields));
        });
    }

    static byte[] BuildStaticCamera(int cameraVersion, bool extraTransformField = false)
    {
        var elementFields = new List<EsfNode>
        {
            EsfNode.Leaf(EsfNodeKind.U32, 43u), EsfNode.Leaf(EsfNodeKind.Ascii, "[Camera]"),
            EsfNode.Leaf(EsfNodeKind.F32, 0f), EsfNode.Leaf(EsfNodeKind.F32, 10f),
            EsfNode.Leaf(EsfNodeKind.Ascii, "duration"), EsfNode.Leaf(EsfNodeKind.Ascii, "free"),
            EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(2, 3, 4)),
            EsfNode.Leaf(EsfNodeKind.F32, 0.01397378f), EsfNode.Leaf(EsfNodeKind.F32, 0.9548134f),
            EsfNode.Leaf(EsfNodeKind.F32, 0.04547913f), EsfNode.Leaf(EsfNodeKind.F32, -0.293373f),
            EsfNode.Leaf(EsfNodeKind.F32, 1f), EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
            EsfNode.Leaf(EsfNodeKind.F32, 0f), EsfNode.Leaf(EsfNodeKind.F32, 0f),
            EsfNode.Leaf(EsfNodeKind.F32, 0f), EsfNode.Leaf(EsfNodeKind.F32, 1f),
        };
        if (extraTransformField)
            elementFields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, "unknown extension"));
        var cameraFields = new List<EsfNode> { EsfNode.Leaf(EsfNodeKind.F32, 30f) };
        if (cameraVersion == 2)
            cameraFields.AddRange([
                EsfNode.Leaf(EsfNodeKind.F32, 0.5f), EsfNode.Leaf(EsfNodeKind.F32, 20000f),
                EsfNode.Leaf(EsfNodeKind.Bool, false), EsfNode.Leaf(EsfNodeKind.F32, 1f),
                EsfNode.Leaf(EsfNodeKind.F32, 1f), EsfNode.Leaf(EsfNodeKind.F32, 0.2f),
            ]);
        var fields = new List<EsfNode>
        {
            // ROOT v3 has the four-field header; the following three fields were added in v4.
            EsfNode.Leaf(EsfNodeKind.F32, 10f), EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
            EsfNode.Leaf(EsfNodeKind.F32, 0f), EsfNode.Leaf(EsfNodeKind.Ascii, ""),
            EsfNode.Leaf(EsfNodeKind.U32, 1u), EsfNode.Leaf(EsfNodeKind.Ascii, "camera"),
            EsfNode.NewRecord("ELEMENT", 5, EsfRecordFlags.IsRecordNode, [elementFields]),
            EsfNode.NewRecord("CAMERA_ELEMENT", (byte)cameraVersion, EsfRecordFlags.IsRecordNode, [cameraFields]),
            EsfNode.Leaf(EsfNodeKind.U32, 1u), EsfNode.Leaf(EsfNodeKind.U32, uint.MaxValue),
            EsfNode.Leaf(EsfNodeKind.U32, 1u), EsfNode.Leaf(EsfNodeKind.U32, 43u),
            EsfNode.NewRecord("ATTACH_TO_PARAMETERS", 0, EsfRecordFlags.IsRecordNode,
                [[EsfNode.Leaf(EsfNodeKind.U32, uint.MaxValue)]]),
        };
        using var stream = new MemoryStream();
        EsfWriter.Write(new EsfDocument
        {
            Signature = EsfSignature.Caab, Root = EsfNode.NewRecord("ROOT", 3, EsfRecordFlags.IsRecordNode, [fields]),
        }, stream);
        return stream.ToArray();
    }
}
