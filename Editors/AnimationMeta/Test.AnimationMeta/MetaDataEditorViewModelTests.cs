using Editors.AnimationMeta.Presentation;
using Shared.Core.Events.Global;
using Shared.GameFormats.AnimationMeta.Definitions;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class Tests
    {

        [Test]
        public void MetaDataEditor_OpenAndVerify()
        {
            var packFile = PathHelper.GetDataFile("Throt.pack");

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);
          
            Assert.That(editor.ParsedFile, Is.Not.Null);
            Assert.That(editor.ParsedFile.Attributes.Count, Is.EqualTo(7));
            Assert.That(editor.ParsedFile.Attributes[0], Is.InstanceOf<AnimatedProp_v14>());
            Assert.That(editor.ParsedFile.Attributes[4], Is.InstanceOf<SplashAttack_v10>());

            editor.SaveActionCommand.Execute(null);

            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            Assert.That(savedFile, Is.Not.Null);
        }


        // MetaDataEditor_OpenAndSave metadata file without changes
        // MetaDataEditor_OpenModifyAndSave - Open the metadata file and modify the content. Ensure the HasUnsavedChanges is set. Save the file and verify that the file is saved correctly and the HasUnsavedChanges is changed to false again.
        // MetaDataEditor_DeleteAndSave - delete an item in the matadata file and save
        // MetaDataEditor_MoveUpAndSave
    }
}
