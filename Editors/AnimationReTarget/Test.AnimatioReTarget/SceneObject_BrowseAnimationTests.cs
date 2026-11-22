using Editors.AnimatioReTarget.Editor;
using Editors.Shared.Core.Common;
using Moq;
using Shared.Core.Commands;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimatioReTarget
{
    // The scene object panel can load any animation of its skeleton, not only one in a bin slot
    public class SceneObject_BrowseAnimationTests
    {
        const string KarlMesh = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";
        const string KarlAnimation = @"animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim";

        readonly string _inputPackFileKarl = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");

        [Test]
        public void BrowseAnimation_LoadsPickedFile_AndOffersOnlyTheSkeletonsAnimations()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(_inputPackFileKarl, true);

            var editor = runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationRetarget_Editor) as AnimationRetargetEditor;
            Assert.That(editor, Is.Not.Null);

            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            var source = editor!.GetSceneObjectFromId(AnimationRetargetIds.Source)!;
            sceneObjectEditor.SetMesh(source.Data, runner.PackFileService.FindFile(KarlMesh)!);

            var persistMeta = new PackFile("persist.meta", null);
            sceneObjectEditor.SetMetaFile(source.Data, new PackFile("slot.anm.meta", null), persistMeta);

            Func<string, bool>? offeredFilter = null;
            var animationFile = runner.PackFileService.FindFile(KarlAnimation)!;
            runner.Dialogs
                .Setup(x => x.DisplayBrowseDialog(It.IsAny<List<string>>(), It.IsAny<Func<string, bool>?>()))
                .Callback<List<string>, Func<string, bool>?>((_, filter) => offeredFilter = filter)
                .Returns(new BrowseDialogResultFile(true, animationFile));

            source.BrowseAnimation();

            Assert.That(source.Data.AnimationClip, Is.Not.Null);
            Assert.That(source.Data.AnimationName.Value, Is.EqualTo(KarlAnimation));
            Assert.That(source.FragAndSlotSelection.AnimationFileName, Does.EndWith(KarlAnimation));

            // The slot's meta belongs to the slot's animation; the fragment's persistent meta stays.
            Assert.That(source.Data.MetaData, Is.Null);
            Assert.That(source.Data.PersistMetaData, Is.SameAs(persistMeta));

            Assert.That(offeredFilter, Is.Not.Null);
            Assert.That(offeredFilter!(KarlAnimation), Is.True);
            Assert.That(offeredFilter(KarlAnimation.ToUpperInvariant()), Is.True);
            Assert.That(offeredFilter(@"animations\battle\humanoid01e\2handed_hammer\stand\hu1e_2hh_stand_idle_01.anim"), Is.False);
        }

        [Test]
        public void BrowseAnimation_WithoutSkeleton_DoesNotOpenTheBrowser()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(_inputPackFileKarl, true);

            var editor = runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationRetarget_Editor) as AnimationRetargetEditor;
            var source = editor!.GetSceneObjectFromId(AnimationRetargetIds.Source)!;

            source.BrowseAnimation();

            runner.Dialogs.Verify(x => x.DisplayBrowseDialog(It.IsAny<List<string>>(), It.IsAny<Func<string, bool>?>()), Times.Never);
            runner.Dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
