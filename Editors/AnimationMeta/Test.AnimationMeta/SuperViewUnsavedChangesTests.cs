using Editors.AnimationMeta.SuperView;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class SuperViewUnsavedChangesTests
    {
        AssetEditorTestRunner _runner = null!;
        IPackFileContainer _output = null!;
        SuperViewViewModel _superView = null!;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.CreateCaContainer();
            _output = _runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), true)!;

            _superView = (SuperViewViewModel)_runner.ServiceProvider.GetRequiredService<IEditorManager>().Create(EditorEnums.SuperView_Editor)!;
            _superView.Load(new AnimationToolInput
            {
                Mesh = _runner.PackFileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition")!,
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5"),
            });
        }

        const string PersistentPath = @"animations\battle\persistent\hu17_dlc16_throt_persistent_metadata_alive_0.anm.meta";
        const string AnimationPath = @"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anm.meta";

        bool InOutput(string path) => _runner.PackFileService.FindFile(path, _output) != null;

        [Test]
        public void DeletingATag_IsAnUnsavedChange()
        {
            _superView.MetaEditor.Tags.Last().IsSelected = true;
            _superView.MetaEditor.DeleteActionCommand.Execute(null);

            Assert.That(_superView.MetaEditor.HasUnsavedChanges, Is.True);
            Assert.That(_superView.PersistentMetaEditor.HasUnsavedChanges, Is.False);
            Assert.That(_superView.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void AddingAndMovingTags_AreUnsavedChanges()
        {
            var editor = _superView.PersistentMetaEditor;
            editor.Tags[0].IsSelected = true;
            editor.CopyActionCommand.Execute(null);
            editor.PasteActionCommand.Execute(null);
            Assert.That(editor.HasUnsavedChanges, Is.True, "paste");

            editor.HasUnsavedChanges = false;
            Assert.That(editor.HasUnsavedChanges, Is.False, "after saving");
            editor.Tags.Last().IsSelected = true;
            editor.MoveUpActionCommand.Execute(null);
            Assert.That(editor.HasUnsavedChanges, Is.True, "move");
        }

        [Test]
        public void Save_WritesOnlyTheFileThatChanged()
        {
            _superView.MetaEditor.Tags[0].Variables.Single(x => x.FieldName == "Start Time").ValueAsString = "0.5";

            Assert.That(_superView.Save(), Is.True);

            Assert.That(InOutput(AnimationPath), Is.True);
            Assert.That(InOutput(PersistentPath), Is.False);
            Assert.That(_superView.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Save_WithNothingChanged_WritesNothing()
        {
            Assert.That(_superView.Save(), Is.True);
            Assert.That(InOutput(AnimationPath), Is.False);
            Assert.That(InOutput(PersistentPath), Is.False);
        }

        // A tab's own Save button still writes its file, changed or not.
        [Test]
        public void ATabsSaveButton_WritesItsFile()
        {
            _superView.PersistentMetaEditor.SaveActionCommand.Execute(null);
            Assert.That(InOutput(PersistentPath), Is.True);
            Assert.That(InOutput(AnimationPath), Is.False);
        }
    }
}
