using Editors.AnimationMeta.Presentation;
using Shared.Core.Events.Global;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    [TestFixture]
    public class MetaDataEditorMultiSelectTests
    {
        [Test]
        public void DeleteAction_MultiSelectedTags_RemovesAllSelectedTags()
        {
            // Arrange
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
            Assert.That(initialCount, Is.GreaterThan(2));

            // Act: Select multiple tags using IsSelected flag (multi-selection mode)
            var tag1 = editor.Tags[0];
            var tag2 = editor.Tags[1];
            tag1.IsSelected = true;
            tag2.IsSelected = true;

            editor.DeleteActionCommand.Execute(null);

            // Assert: Both selected tags should be removed
            Assert.That(editor.Tags.Count, Is.EqualTo(initialCount - 2));
            Assert.That(editor.Tags, Does.Not.Contain(tag1));
            Assert.That(editor.Tags, Does.Not.Contain(tag2));

            // Verify persistence by saving and reloading
            editor.SaveActionCommand.Execute(null);
            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            Assert.That(savedFile, Is.Not.Null);

            var parser = runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
            var parsedFile = parser.ParseFile(savedFile);
            Assert.That(parsedFile.Attributes.Count, Is.EqualTo(initialCount - 2));
        }

        [Test]
        public void DeleteAction_SelectedTagOnly_RemovesSingleTag()
        {
            // Arrange
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

            // Act: Use SelectedTag instead of IsSelected (single-selection mode)
            // This verifies backward compatibility with existing behavior
            editor.SelectedTag = editor.Tags.Last();
            var tagToDelete = editor.SelectedTag;

            editor.DeleteActionCommand.Execute(null);

            // Assert: Only the selected tag should be removed
            Assert.That(editor.Tags.Count, Is.EqualTo(initialCount - 1));
            Assert.That(editor.Tags, Does.Not.Contain(tagToDelete));

            // Verify default selection behavior
            Assert.That(editor.SelectedTag, Is.Not.Null);
            Assert.That(editor.SelectedTag, Is.EqualTo(editor.Tags[0]));
        }

        [Test]
        public void DeleteAction_NoSelection_DoesNothing()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);
            var initialCount = editor.Tags.Count;

            // Clear all selection states
            foreach (var tag in editor.Tags)
                tag.IsSelected = false;
            editor.SelectedTag = null;

            // Act: Delete with no selection
            editor.DeleteActionCommand.Execute(null);

            // Assert: No tags should be deleted
            Assert.That(editor.Tags.Count, Is.EqualTo(initialCount));
        }

        [Test]
        public void MoveUpAction_MultiSelectedTags_MovesAllSelectedAsBlock()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);
            Assert.That(editor.Tags.Count, Is.GreaterThan(3));

            // Select consecutive tags to test block movement
            var tag1 = editor.Tags[2];
            var tag2 = editor.Tags[3];
            tag1.IsSelected = true;
            tag2.IsSelected = true;

            var type1 = tag1._input.GetType();
            var type2 = tag2._input.GetType();

            // Act: Move selected block up
            editor.MoveUpActionCommand.Execute(null);

            // Assert: Both tags should move up together, maintaining their order
            Assert.That(editor.Tags[1]._input, Is.InstanceOf(type1));
            Assert.That(editor.Tags[2]._input, Is.InstanceOf(type2));
            Assert.That(editor.Tags[1].IsSelected, Is.True);
            Assert.That(editor.Tags[2].IsSelected, Is.True);
        }

        [Test]
        public void MoveDownAction_MultiSelectedTags_MovesAllSelectedAsBlock()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);
            Assert.That(editor.Tags.Count, Is.GreaterThan(3));

            // Select consecutive tags to test block movement
            var tag1 = editor.Tags[1];
            var tag2 = editor.Tags[2];
            tag1.IsSelected = true;
            tag2.IsSelected = true;

            var type1 = tag1._input.GetType();
            var type2 = tag2._input.GetType();

            // Act: Move selected block down
            editor.MoveDownActionCommand.Execute(null);

            // Assert: Both tags should move down together, maintaining their order
            Assert.That(editor.Tags[2]._input, Is.InstanceOf(type1));
            Assert.That(editor.Tags[3]._input, Is.InstanceOf(type2));
            Assert.That(editor.Tags[2].IsSelected, Is.True);
            Assert.That(editor.Tags[3].IsSelected, Is.True);
        }

        [Test]
        public void MoveUpAction_SelectedTagOnly_MovesSingleTag()
        {
            // Arrange
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

            // Act: Use SelectedTag instead of IsSelected (single-selection mode)
            // This verifies backward compatibility with existing tests
            editor.SelectedTag = editor.Tags[4];
            var originalType = editor.SelectedTag._input.GetType();

            editor.MoveUpActionCommand.Execute(null);

            // Assert: Tag should move up one position
            Assert.That(editor.Tags[3]._input, Is.InstanceOf(originalType));

            // Verify persistence
            editor.SaveActionCommand.Execute(null);
            var savedFile = runner.PackFileService.FindFile(filePath, outputPackFile);
            var parser = runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
            var parsedFile = parser.ParseFile(savedFile);
            Assert.That(parsedFile.Attributes[3], Is.InstanceOf(originalType));
        }

        [Test]
        public void MoveUpAction_TopTag_DoesNothing()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);

            // Act: Try to move the topmost tag up
            editor.SelectedTag = editor.Tags[0];
            var originalType = editor.Tags[0]._input.GetType();

            editor.MoveUpActionCommand.Execute(null);

            // Assert: Tag should remain at position 0 (boundary check)
            Assert.That(editor.Tags[0]._input, Is.InstanceOf(originalType));
        }

        [Test]
        public void MoveDownAction_BottomTag_DoesNothing()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);

            // Act: Try to move the bottommost tag down
            editor.SelectedTag = editor.Tags[^1];
            var originalType = editor.Tags[^1]._input.GetType();

            editor.MoveDownActionCommand.Execute(null);

            // Assert: Tag should remain at the last position (boundary check)
            Assert.That(editor.Tags[^1]._input, Is.InstanceOf(originalType));
        }

        [Test]
        public void MoveDownAction_NoSelection_DoesNothing()
        {
            // Arrange
            var packFile = PathHelper.GetDataFile("Throt.pack");
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(packFile, true);

            var filePath = @"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta";
            var metaPackFile = runner.PackFileService.FindFile(filePath);
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

            Assert.That(editor.ParsedFile, Is.Not.Null);

            var initialTypes = editor.Tags.Select(t => t._input.GetType()).ToList();

            // Clear all selection states
            foreach (var tag in editor.Tags)
                tag.IsSelected = false;
            editor.SelectedTag = null;

            // Act: Move with no selection
            editor.MoveDownActionCommand.Execute(null);

            // Assert: Order should remain unchanged
            var currentTypes = editor.Tags.Select(t => t._input.GetType()).ToList();
            Assert.That(currentTypes, Is.EqualTo(initialTypes));
        }
        // ❌ DELETE THIS ENTIRE METHOD
        //[Test]
        //public void DeleteAndMoveOperations_WithNullParsedFile_DoesNotCrash()
        //{
        // Arrange: Create editor without loading a file (ParsedFile will be null)
        //var runner = new AssetEditorTestRunner();
        //runner.CreateCaContainer();

        //var editor = runner.CommandFactory
        //.Create<OpenEditorCommand>()
        //.Execute<MetaDataEditorViewModel>(null!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);

        // Act & Assert: All operations should handle null gracefully without throwing
        //Assert.DoesNotThrow(() => editor.DeleteActionCommand.Execute(null));
        //Assert.DoesNotThrow(() => editor.MoveUpActionCommand.Execute(null));
        //Assert.DoesNotThrow(() => editor.MoveDownActionCommand.Execute(null));
    }
}
