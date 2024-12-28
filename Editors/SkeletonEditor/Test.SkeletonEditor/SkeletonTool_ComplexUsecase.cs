using Editor.VisualSkeletonEditor.SkeletonEditor;
using Editors.Shared.Core.Common.ReferenceModel;
using Moq;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.Ui.Events.UiCommands;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.SkeletonEditor
{
    internal class SkeletonTool_ComplexUsecase
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void IntegrationTest_0()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(_inputPackFileKarl, true);

            // Load the a rmv2 and open the kitbash editor
            var originalRmv2File = runner.PackFileService.FindFile("animations\\skeletons\\humanoid01.anim");
            runner.CommandFactory.Create<OpenEditorCommand>().Execute(originalRmv2File, EditorEnums.VisualSkeletonEditor);

            var skeletonTool = runner.GetRequiredServiceInCurrentEditorScope<SkeletonEditorViewModel>();

            LoadReferenceMesh(runner, skeletonTool);
            EditBone(skeletonTool);
            DuplicateBone(skeletonTool);
            DeleteBone(skeletonTool);
            Save(runner, skeletonTool);
        }


        void LoadReferenceMesh(AssetEditorTestRunner runner, SkeletonEditorViewModel skeletonEditor)
        {
            var meshName = @"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_body_02.rigid_model_v2";
            var meshRmv = runner.PackFileService.FindFile(meshName);
            runner.Dialogs.Setup(x => x.DisplayBrowseDialog(It.IsAny<List<string>>())).Returns(new BrowseDialogResultFile(true, meshRmv));
            skeletonEditor.LoadRefMeshAction();

            Assert.That(skeletonEditor.SkeletonName.Contains("humanoid01.anim"));   // Skeleton not changed
            Assert.That(skeletonEditor.RefMeshName, Is.EqualTo(meshName));
            Assert.That(skeletonEditor.SceneObjects.First().Data.ModelNode, Is.Not.Null);
        }

        void EditBone(SkeletonEditorViewModel skeletonEditor)
        {
            var skeleton = skeletonEditor.SceneObjects.First().Data.Skeleton;
            Assert.That(skeleton, Is.Not.Null);

            // Select a bone
            var bone = SkeletonToolHelpers.GetSkeletonNodeFromName("upperleg_left", skeletonEditor.Bones);
            skeletonEditor.SelectedBone = bone;
            Assert.That(bone, Is.Not.Null);
            Assert.That(skeletonEditor.SelectedBoneName, Is.EqualTo("upperleg_left"));
            var bonePosZ = skeletonEditor.SelectedBoneTranslationOffset.Z.Value;
            Assert.That(bonePosZ, Is.EqualTo(-0.008484));

            // Rename
            var newBoneName = "newbonename_12";
            skeletonEditor.SelectedBoneName = newBoneName;
            Assert.That(skeleton.GetBoneIndexByName(newBoneName), Is.EqualTo(bone.BoneIndex)); // Ensure we updated the core data structure

            // Move a bone
            skeletonEditor.SelectedBoneTranslationOffset.Z.Value = 10;
            var boneLocalTranslation = skeleton.Translation[bone.BoneIndex];   // Ensure we updated the core data structure
            Assert.That(boneLocalTranslation.X, Is.EqualTo(0.0144924521f));
            Assert.That(boneLocalTranslation.Y, Is.EqualTo(10.025753f));
            Assert.That(boneLocalTranslation.Z, Is.EqualTo(-0.09648923f));
        }

        void DeleteBone(SkeletonEditorViewModel skeletonEditor)
        {
            var skeleton = skeletonEditor.SceneObjects.First().Data.Skeleton;
            var c = skeleton.BoneCount;

            // Delete a bone 
            var bone = SkeletonToolHelpers.GetSkeletonNodeFromName("clav_right", skeletonEditor.Bones);
            skeletonEditor.SelectedBone = bone;
            skeletonEditor.DeleteBoneAction();

            Assert.That(skeleton.BoneCount, Is.EqualTo(56));
        }

        void DuplicateBone(SkeletonEditorViewModel skeletonEditor)
        {


        }

        void Save(AssetEditorTestRunner runner, SkeletonEditorViewModel skeletonEditor)
        {
            skeletonEditor.SaveSkeletonAction();

            var moddingPack = runner.PackFileService.GetEditablePack();
            var invFile = runner.PackFileService.FindFile(@"animations\skeletons\humanoid01.bone_inv_trans_mats", moddingPack);
            var skeletonFile = runner.PackFileService.FindFile(@"animations\skeletons\humanoid01.anim", moddingPack);

            var skeleton = AnimationFile.Create(skeletonFile.DataSource.ReadDataAsChunk());

            Assert.That(skeleton.Bones.Length, Is.EqualTo(56));
            Assert.That(invFile, Is.Not.Null);
        }
    }

    static class SkeletonToolHelpers
    {
        public static SkeletonBoneNode GetSkeletonNodeFromName(string name, IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                if (bone.BoneName == name)
                    return bone;

                var found = GetSkeletonNodeFromName(name, bone.Children);
                if (found != null)
                    return found; ;
            }

            return null;
        }
    }
}
