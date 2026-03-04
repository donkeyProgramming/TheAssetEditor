using Editors.AnimationMeta.Presentation;
using Shared.Core.Events.Global;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class MetaDataEditorViewModelTests
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
        [Test]
        public void MetaDataEditor_OpenModifyAndSave()
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


            //SplashAttack_v10 - Filter - string
            var strValue = "customstr";
            editor.Tags[4].Variables[3].ValueAsString = strValue;

            //SplashAttack_v10 - AoeShape - int
            var intValue = 8;
            editor.Tags[4].Variables[5].ValueAsString = intValue.ToString();

            //SplashAttack_v10 - EndPosition - Vector 3
            var vectorValue = 120;
            (editor.Tags[4].Variables[7] as VectorAttributeViewModel)!.Value.X.Value = vectorValue;

            // HasUnsavedChanges should be signaled via events in superview, but editor itself doesn't track it automatically.
            // Ensure saving works and the saved file exists in output pack
            editor.SaveActionCommand.Execute(null);

            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            Assert.That(savedFile, Is.Not.Null);

            // Reload the file and verify
            var parser = runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
            var parsedFile = parser.ParseFile(savedFile);
            Assert.That(parsedFile, Is.Not.Null);

            var splashAttack = parsedFile.Attributes[4] as SplashAttack_v10;
            Assert.That(splashAttack, Is.Not.Null);
            Assert.That(splashAttack.Filter, Is.EqualTo(strValue));
            Assert.That(splashAttack.AoeShape, Is.EqualTo(intValue));
            Assert.That(splashAttack.EndPosition.X, Is.EqualTo(vectorValue));
        }

        [Test]
        public void MetaDataEditor_DeleteAndSave()
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
            var initialCount = editor.Tags.Count;
            Assert.That(initialCount, Is.GreaterThan(0));

            // Select last tag and delete
            editor.SelectedTag = editor.Tags.Last();
            editor.DeleteActionCommand.Execute(null);

            Assert.That(editor.Tags.Count, Is.EqualTo(initialCount - 1));

            editor.SaveActionCommand.Execute(null);

            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            Assert.That(savedFile, Is.Not.Null);
           
            // Reload the file and verify
            var parser = runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
            var parsedFile = parser.ParseFile(savedFile);
            Assert.That(parsedFile, Is.Not.Null);
            Assert.That(parsedFile.Attributes.Count, Is.EqualTo(initialCount - 1));
        }

        [Test]
        public void MetaDataEditor_MoveUpAndSave()
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

            // Move second entry up
            editor.SelectedTag = editor.Tags[4];
            editor.MoveUpActionCommand.Execute(null);

            editor.SaveActionCommand.Execute(null);

            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            Assert.That(savedFile, Is.Not.Null);

            // Reload the file and verify
            var parser = runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
            var parsedFile = parser.ParseFile(savedFile);
            Assert.That(parsedFile, Is.Not.Null);
            Assert.That(parsedFile.Attributes[3], Is.InstanceOf<SplashAttack_v10>());
        }
    }
}
