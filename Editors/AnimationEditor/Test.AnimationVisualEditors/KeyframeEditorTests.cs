using Editors.AnimationVisualEditors.AnimationKeyframeEditor;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Shared.Core.Commands;
using Shared.Core.ToolCreation;
using static Test.AnimationVisualEditors.VanillaRiders;

namespace Test.AnimationVisualEditors
{
    public class KeyframeEditorTests
    {
        const string RiderAnimation = @"animations\battle\humanoid01\rider\horse01\spear_and_shield\stand\hu1_hr1_sps_rider1_stand_01.anim";

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
            Assert.That(controls.PlayerItems.Select(x => x.SlotName.Value), Is.EqualTo(new[] { "Rider", "Mount" }), "one row each, named before the mount has an animation");

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
    }
}
