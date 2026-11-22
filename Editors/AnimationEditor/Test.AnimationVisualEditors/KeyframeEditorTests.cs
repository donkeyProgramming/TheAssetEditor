using Editors.AnimationVisualEditors.AnimationKeyframeEditor;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Test.TestingUtility.Shared;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;
using Shared.Core.Commands;
using Shared.Core.ToolCreation;
using static Test.AnimationVisualEditors.VanillaRiders;

namespace Test.AnimationVisualEditors
{
    public class KeyframeEditorTests
    {
        const string RiderAnimation = @"animations\battle\humanoid01\rider\horse01\spear_and_shield\stand\hu1_hr1_sps_rider1_stand_01.anim";
        const string IdleAnimation = @"animations\battle\humanoid01\rider\horse01\spear_and_shield\stand\hu1_hr1_sps_rider1_stand_idle_01.anim";

        static (AnimationKeyframeEditorViewModel Editor, SceneObject Rider, AnimationPlayerViewModel Controls) Open()
        {
            var runner = VanillaRiders.Load();
            var editor = (AnimationKeyframeEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationKeyFrame_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            sceneObjectEditor.SetMesh(editor.Rider, runner.PackFileService.FindFile(RiderMesh)!);
            sceneObjectEditor.SetAnimation(editor.Rider, RiderAnimation);
            var controls = runner.GetRequiredServiceInCurrentEditorScope<AnimationPlayerViewModel>();
            controls.IsEnabled.Value = true;
            return (editor, editor.Rider, controls);
        }

        [Test]
        public void TheControlsUnderTheViewportStepAndWrap()
        {
            var (_, rider, controls) = Open();
            var last = rider.Player.FrameCount() - 1;
            Assert.That(controls.PlayerItems.Select(x => x.SlotName.Value), Is.EqualTo(new[] { "Model" }), "no mount row until a mount is asked for");

            controls.SetAnimationFirstFrame();
            controls.SetAnimationPrivFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(last), "Prev on the first frame goes to the last");

            rider.Player.CurrentFrame = last - 1;
            controls.SetAnimationNextFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(last), "Next onto the last frame stays there");
            Assert.That(rider.Player.IsPlaying, Is.False);

            controls.SetAnimationNextFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(0), "Next on the last frame goes to the first");

            controls.LoopAnimation.Value = true;
            controls.SetAnimationLastFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(last));
            Assert.That(controls.LoopAnimation.Value, Is.True, "Last leaves Loop alone");
        }

        [Test]
        public void ShowsTheFrameAndEditsIt()
        {
            var (editor, rider, controls) = Open();
            var frames = rider.AnimationClip.DynamicFrames.Count;
            Assert.That(editor.CurrentFrameNumber.Value, Is.EqualTo("0"));
            Assert.That(editor.TotalFrameNumber.Value, Is.EqualTo(frames.ToString()));

            rider.Player.CurrentFrame = 2;
            editor.DuplicateFrame();
            Assert.That(rider.AnimationClip.DynamicFrames, Has.Count.EqualTo(frames + 1));
            Assert.That(rider.AnimationClip.DynamicFrames[3].Position, Is.EqualTo(rider.AnimationClip.DynamicFrames[2].Position));
            editor.UndoPose();
            Assert.That(rider.AnimationClip.DynamicFrames, Has.Count.EqualTo(frames));

            // Copy frame 0's pose onto frame 30, then step on with the controls
            rider.Player.CurrentFrame = 0;
            editor.CopyCurrentPose();
            rider.Player.CurrentFrame = 30;
            editor.PasteIntoCurrentFrame();
            Assert.That(rider.AnimationClip.DynamicFrames[30].Rotation, Is.EqualTo(rider.AnimationClip.DynamicFrames[0].Rotation));
            Assert.That(editor.IsDirty.Value, Is.True);
            controls.SetAnimationNextFrame();
            Assert.That(editor.CurrentFrameNumber.Value, Is.EqualTo("31"));
        }

        [Test]
        public void TheMountOnlyShowsUpWhenAskedFor()
        {
            var (editor, rider, controls) = Open();
            Assert.That(editor.WithMount, Is.False);
            Assert.That(editor.SceneObjects.Select(x => x.HeaderName), Is.EqualTo(new[] { "Model:" }));

            editor.Pause();
            rider.Player.CurrentFrame = 5;
            editor.WithMount = true;
            Assert.That(editor.SceneObjects.Select(x => x.HeaderName), Is.EqualTo(new[] { "Rider:", "Mount:" }));
            Assert.That(controls.PlayerItems.Select(x => x.SlotName.Value), Is.EqualTo(new[] { "Rider", "Mount" }));
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(5), "showing the mount leaves a paused rider where it was");
            Assert.That(rider.Player.IsPlaying, Is.False);
            Assert.That(editor.Mount.ShowMesh.Value, Is.True);

            controls.SetAnimationNextFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(6));

            editor.WithMount = false;
            Assert.That(editor.SceneObjects.Select(x => x.HeaderName), Is.EqualTo(new[] { "Model:" }));
            Assert.That(controls.PlayerItems.Select(x => x.SlotName.Value), Is.EqualTo(new[] { "Model" }));
            Assert.That(controls.SelectedMainAnimation.Asset, Is.SameAs(rider));
            Assert.That(editor.Mount.ShowMesh.Value, Is.False);

            controls.SetAnimationNextFrame();
            Assert.That(rider.Player.CurrentFrame, Is.EqualTo(7), "the controls still step the model on its own");
        }

        [Test]
        public void LoadingAMountAnimationLeavesTheRiderEditAlone()
        {
            var runner = VanillaRiders.Load();
            var editor = (AnimationKeyframeEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationKeyFrame_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            sceneObjectEditor.SetMesh(editor.Rider, runner.PackFileService.FindFile(RiderMesh)!);
            sceneObjectEditor.SetAnimation(editor.Rider, RiderAnimation);
            var riderDuration = editor.Rider.AnimationClip.PlayTimeInSec;

            editor.FramesDurationInSeconds = "2";
            editor.ApplyDuration();
            Assert.That(editor.IsDirty.Value, Is.True);

            editor.WithMount = true;
            sceneObjectEditor.SetMesh(editor.Mount, runner.PackFileService.FindFile(MountMesh)!);
            var mountAnimation = Set(runner.PackFileService, MountSet).Entries.First(x => x.SlotName == "RUN_1").AnimationFile;
            sceneObjectEditor.SetAnimation(editor.Mount, mountAnimation);
            Assert.That(editor.Mount.AnimationClip.PlayTimeInSec, Is.Not.EqualTo(riderDuration).Within(0.01f), "precondition: the two durations tell the clips apart");

            Assert.That(editor.IsDirty.Value, Is.True, "the rider's edit is still unsaved");
            Assert.That(editor.FramesDurationInSeconds, Is.EqualTo("2"));
            editor.ResetDuration();
            Assert.That(editor.Rider.AnimationClip.PlayTimeInSec, Is.EqualTo(riderDuration), "Reset goes back to the rider's own duration");
        }

        // Calls the selection component's click handler at a pixel, as a left click there does
        static void Click(AssetEditorTestRunner runner, Vector2 pixel, bool shift = false, bool ctrl = false)
        {
            var selection = runner.GetRequiredServiceInCurrentEditorScope<SelectionComponent>();
            typeof(SelectionComponent).GetMethod("SelectFromPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(selection, [pixel, shift, ctrl]);
        }

        [Test]
        public void ClickingNearAJointSelectsItsBoneAndTheLabelNamesIt()
        {
            var runner = VanillaRiders.Load();
            var editor = (AnimationKeyframeEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationKeyFrame_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            sceneObjectEditor.SetMesh(editor.Rider, runner.PackFileService.FindFile(RiderMesh)!);
            sceneObjectEditor.SetAnimation(editor.Rider, RiderAnimation);
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo("bone selection is off, click Select bones"));

            var controls = runner.GetRequiredServiceInCurrentEditorScope<AnimationPlayerViewModel>();
            Assert.That(controls.IsEnabled.Value, Is.False, "precondition: the controls under the viewport start off");
            editor.EnterSelectMode();
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo("no bones"));
            Assert.That(controls.IsEnabled.Value, Is.True, "Select bones turns them on: there are no joints to pick without a frame");
            Assert.That(editor.Rider.Player.IsPlaying, Is.False);
            var state = editor.SelectionManager.GetState<BoneSelectionState>()!;
            var mesh = (Rmv2MeshNode)state.RenderObject;

            // Each joint's pixel, as the viewport draws it
            var camera = runner.GetRequiredServiceInCurrentEditorScope<ArcBallCamera>();
            // The test device is 20 x 20 pixels; give it a window's worth
            var device = runner.GetRequiredServiceInCurrentEditorScope<IDeviceResolver>().Device;
            device.Viewport = new Microsoft.Xna.Framework.Graphics.Viewport(0, 0, 1200, 800);
            var viewport = device.Viewport;
            var viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
            mesh.AnimationPlayer.Refresh(); // the game loop does this every tick
            var frame = mesh.AnimationPlayer.GetCurrentAnimationFrame();
            Vector2 Pixel(int bone)
            {
                var world = Vector3.Transform(frame.GetSkeletonAnimatedWorld(state.Skeleton, bone).Translation, mesh.RenderMatrix);
                var clip = Vector4.Transform(new Vector4(world, 1), viewProjection);
                return new Vector2((clip.X / clip.W + 1) * 0.5f * viewport.Width, (1 - clip.Y / clip.W) * 0.5f * viewport.Height);
            }
            var bones = Enumerable.Range(1, state.Skeleton.BoneCount - 1).ToList();
            var top = bones.MinBy(x => Pixel(x).Y);
            var bottom = bones.MaxBy(x => Pixel(x).Y);
            Assert.That(Pixel(bottom).Y - Pixel(top).Y, Is.GreaterThan(100), "precondition: the joints are far apart on screen");

            Click(runner, Pixel(top));
            Assert.That(state.SelectedBones, Has.Count.EqualTo(1));
            Assert.That(Vector2.Distance(Pixel(state.SelectedBones[0]), Pixel(top)), Is.LessThan(1), "the bone at the clicked joint");
            var topName = state.Skeleton.BoneNames[state.SelectedBones[0]];
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo(topName));

            Click(runner, Pixel(bottom), shift: true);
            Assert.That(state.SelectedBones, Has.Count.EqualTo(2), "Shift adds");
            Assert.That(editor.SelectedBonesText.Value, Does.StartWith(topName + ", ").Or.EndWith(", " + topName));

            Click(runner, Pixel(bottom), ctrl: true);
            Assert.That(state.SelectedBones, Has.Count.EqualTo(1), "Ctrl removes");

            Click(runner, Pixel(top) + new Vector2(0, -200));
            Assert.That(editor.SelectionManager.GetState(), Is.SameAs(state), "a click on nothing stays in bone mode");
            Assert.That(state.SelectedBones, Is.Empty, "and clears the bones");
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo("no bones"));

            Click(runner, Pixel(top));
            editor.UndoPose();
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo("no bones"), "the label follows undo");
        }

        [Test]
        public void MovingABoneWorksAfterSelectBonesIsClickedAgain()
        {
            var runner = VanillaRiders.Load();
            var editor = (AnimationKeyframeEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationKeyFrame_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            sceneObjectEditor.SetMesh(editor.Rider, runner.PackFileService.FindFile(RiderMesh)!);
            sceneObjectEditor.SetAnimation(editor.Rider, RiderAnimation);

            // The first Select bones, then back out of bone mode, then a second one: a new selection state
            editor.EnterSelectMode();
            var first = editor.SelectionManager.GetState<BoneSelectionState>();
            editor.UndoPose();
            editor.UndoPose();
            Assert.That(editor.SelectionManager.GetState().Mode, Is.EqualTo(GeometrySelectionMode.Object), "precondition: undo left bone mode");
            editor.EnterSelectMode();
            var state = editor.SelectionManager.GetState<BoneSelectionState>()!;
            Assert.That(state, Is.Not.SameAs(first), "precondition: a new bone selection state");

            const int bone = 7;
            editor.CommandFactory.CreateWithBuilder<BoneSelectionCommand>().Configure(x => x.Configure([bone], false, false)).BuildAndExecute();
            var before = editor.Rider.AnimationClip.DynamicFrames[state.CurrentFrame].Rotation[bone];
            ((Rmv2MeshNode)state.RenderObject).AnimationPlayer.Refresh(); // the game loop does this every tick

            // What the gizmo does on a rotate and a move
            foreach (var (mode, transform) in new[] { (GizmoMode.Rotate, Microsoft.Xna.Framework.Matrix.CreateRotationY(0.3f)), (GizmoMode.Translate, Microsoft.Xna.Framework.Matrix.CreateTranslation(0.1f, 0, 0)) })
            {
                var command = editor.CommandFactory.CreateWithBuilder<TransformBoneCommand>().Configure(x => x.Configure(state.SelectedBones, state)).Build();
                // The gizmo sends a stream of transforms; the first only sets where the drag starts
                Assert.DoesNotThrow(() => command.ApplyTransformation(Microsoft.Xna.Framework.Matrix.CreateTranslation(0.001f, 0, 0), mode), mode.ToString());
                Assert.DoesNotThrow(() => command.ApplyTransformation(transform, mode), mode.ToString());
            }

            Assert.That(editor.Rider.AnimationClip.DynamicFrames[state.CurrentFrame].Rotation[bone], Is.Not.EqualTo(before));
            Assert.That(editor.GetModifiedBones(), Does.Contain(bone), "the editor heard about the edit");
        }

        [Test]
        public void EditingIsGreyedOutUntilThereIsAnAnimation()
        {
            var runner = VanillaRiders.Load();
            var editor = (AnimationKeyframeEditorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationKeyFrame_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            sceneObjectEditor.SetMesh(editor.Rider, runner.PackFileService.FindFile(RiderMesh)!);
            Assert.That(editor.HasAnimation.Value, Is.False);

            editor.EnterSelectMode();
            Assert.That(editor.SelectionManager.GetState().Mode, Is.EqualTo(GeometrySelectionMode.Object), "Select bones does nothing without an animation");

            sceneObjectEditor.SetAnimation(editor.Rider, RiderAnimation);
            Assert.That(editor.HasAnimation.Value, Is.True);
            editor.EnterSelectMode();
            editor.CommandFactory.CreateWithBuilder<BoneSelectionCommand>().Configure(x => x.Configure([7], false, false)).BuildAndExecute();

            // Another animation while selecting bones: the selection moves to it
            sceneObjectEditor.SetAnimation(editor.Rider, IdleAnimation);
            var state = editor.SelectionManager.GetState<BoneSelectionState>()!;
            Assert.That(state.CurrentAnimation, Is.SameAs(editor.Rider.AnimationClip));
            Assert.That(state.SelectedBones, Is.Empty);
            Assert.That(editor.SelectedBonesText.Value, Is.EqualTo("no bones"));
        }
    }
}
