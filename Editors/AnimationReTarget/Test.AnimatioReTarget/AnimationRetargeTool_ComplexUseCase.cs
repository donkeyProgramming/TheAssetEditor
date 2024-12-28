using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.Shared.Core.Common;
using GameWorld.Core.SceneNodes;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimatioReTarget
{
    public class AnimationRetargeTool_ComplexUseCase
    {
        readonly string _inputPackFileKarl = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");

        [Test]
        public void IntegrationTest_0()
        {
            // Target (The one that gets the animation)  = celestialgeneral
            // Source (The one we take animation from)   = Karl

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Open the tool
            var originalRmv2File = runner.PackFileService.FindFile("animations\\skeletons\\humanoid01.anim");
            var editorInterface = runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationRetarget_Editor);

            var editor = editorInterface as AnimationRetargetEditor;
            Assert.That(editor, Is.Not.Null);
           
            Step0_SelectSource_NoGeneratedSet(runner, editor!);
            Step1_SelectTarget_GeneratedSet(runner, editor!);
            Step2_TryGeneratingAnimation(runner, editor!);
            Step3_ApplyDefaultMapping(runner, editor!);
            Step4_GenerateAnimation(runner, editor!);
            Step5_UpdateAnimationSettingsAndGenerateAnimation(runner, editor!, outputPackFile);
            // UpdateSouceAndValidate(runner, editor!);
            // UpdateTargetAndValidate(runner, editor!);
            // Close
        }

        private void Step0_SelectSource_NoGeneratedSet(AssetEditorTestRunner runner, AnimationRetargetEditor editor)
        {
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            var meshFile = runner.PackFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel");

            var sourceNode = editor.GetSceneObjectFromId(AnimationRetargetIds.Source);
            sceneObjectEditor.SetMesh(sourceNode.Data, meshFile);
            sceneObjectEditor.SetAnimation(sourceNode.Data, "animations\\battle\\humanoid01\\2handed_hammer\\stand\\hu1_2hh_stand_idle_01.anim");

            // Check that the scource node is created correctly
            Assert.That(sourceNode.Data.Skeleton, Is.Not.Null);
            Assert.That(sourceNode.Data.SkeletonName.Value, Is.EqualTo(@"animations\skeletons\humanoid01.anim"));
            Assert.That(sourceNode.Data.AnimationName.Value, Is.EqualTo("animations\\battle\\humanoid01\\2handed_hammer\\stand\\hu1_2hh_stand_idle_01.anim"));


            // Validate that generated is not set
            var generated = editor.GetSceneObjectFromId(AnimationRetargetIds.Generated);
            Assert.That(generated.Data.Skeleton, Is.Null);
        }

        private void Step1_SelectTarget_GeneratedSet(AssetEditorTestRunner runner, AnimationRetargetEditor editor)
        {
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            var meshFile = runner.PackFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_body_02.wsmodel");

            var targetNode = editor.GetSceneObjectFromId(AnimationRetargetIds.Target);
            sceneObjectEditor.SetMesh(targetNode.Data, meshFile);

            // Check that the target node is created correctly
            Assert.That(targetNode.Data.Skeleton, Is.Not.Null);
            Assert.That(targetNode.Data.SkeletonName.Value, Is.EqualTo(@"animations\skeletons\humanoid01e.anim"));

            // Validate that generated node is created
            var generated = editor.GetSceneObjectFromId(AnimationRetargetIds.Generated);
            Assert.That(generated.Data.Skeleton, Is.Not.Null);
            Assert.That(generated.Data.SkeletonName.Value, Is.EqualTo(@"animations\skeletons\humanoid01e.anim"));
            var meshCount = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(generated.Data.ModelNode);
            Assert.That(meshCount, Is.Not.Zero);
        }

        private void Step2_TryGeneratingAnimation(AssetEditorTestRunner runner, AnimationRetargetEditor editor)
        {
            var result = editor.CanUpdateAnimation(out var text);
            Assert.That(result, Is.True);
            Assert.That(text, Is.EqualTo(""));
        }


        private void Step3_ApplyDefaultMapping(AssetEditorTestRunner runner, AnimationRetargetEditor editor)
        {
            var count0 = SkeletonBoneNodeHelper.CountMappedBones(editor.BoneManager.Bones);
            editor.BoneManager.ApplyDefaultMapping();
            var count1 = SkeletonBoneNodeHelper.CountMappedBones(editor.BoneManager.Bones);

            Assert.That(count0, Is.EqualTo(0));
            Assert.That(count1, Is.EqualTo(57));
        }

        private void Step4_GenerateAnimation(AssetEditorTestRunner runner, AnimationRetargetEditor editor)
        {
            editor.UpdateAnimation();

            // Check anim click
            // Check player
        }

        private void Step5_UpdateAnimationSettingsAndGenerateAnimation(AssetEditorTestRunner runner, AnimationRetargetEditor editor, PackFileContainer outputPackFile)
        {
            Assert.That(outputPackFile.FileList.Count, Is.EqualTo(0));

            var bone = SkeletonBoneNodeHelper.GetNodeFromName("skirt_back_0", editor.BoneManager.Bones);
            bone.TranslationOffset.X.Value = 10;

            editor.UpdateAnimation();
            editor.SaveManager.SaveAnimation(false);

            // Ensure new file is created in pfs
            Assert.That(outputPackFile.FileList.Count, Is.EqualTo(1));
            Assert.That(outputPackFile.FileList.First().Key, Is.EqualTo("animations\\battle\\humanoid01e\\2handed_hammer\\stand\\prefix_hu1_2hh_stand_idle_01.anim"));
        }

    }
}

