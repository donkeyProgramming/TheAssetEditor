using Editors.AnimationMeta.SuperView;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class SuperViewMissingMetaTests
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

        // ATTACK_3's META is not in the test packs; ATTACK_5's is.
        const string Attack3Meta = "animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_03.anm.meta";

        BinAnimationViewModel Slots => _superView.SceneObjects[0].FragAndSlotSelection;
        void SelectSlot(string name) => Slots.FragmentSlotList.SelectedItem = Slots.FragmentSlotList.PossibleValues.First(x => x.SlotName == name);

        [Test]
        public void ASlotWhoseMetaIsThere_OffersNothing()
        {
            Assert.That(_superView.CanCreateAnimationMetaFile, Is.False);
            Assert.That(_superView.IsAnimationMetaFileNotNamed, Is.False);
            Assert.That(_superView.CanCreatePersistentMetaFile, Is.False);
            Assert.That(_superView.IsPersistentMetaFileNotNamed, Is.False);
        }

        [Test]
        public void ASlotWhoseMetaIsNotInThePacks_OffersToCreateIt()
        {
            SelectSlot("ATTACK_3");
            Assert.That(_superView.MetaEditor.CurrentFile, Is.Null);
            Assert.That(_superView.CanCreateAnimationMetaFile, Is.True);
            Assert.That(_superView.AnimationMetaFilePath, Is.EqualTo(Attack3Meta));
            Assert.That(_superView.IsAnimationMetaFileNotNamed, Is.False);
        }

        [Test]
        public void CreatingIt_WritesAnEmptyMetaAtThatPathInTheEditablePack_AndOpensIt()
        {
            SelectSlot("ATTACK_3");
            _superView.CreateAnimationMetaFile();

            var file = _runner.PackFileService.FindFile(Attack3Meta, _output);
            Assert.That(file, Is.Not.Null);
            var parsed = _runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>().ParseFile(file!);
            Assert.That(parsed.Version, Is.EqualTo(2));
            Assert.That(parsed.Attributes, Is.Empty);

            Assert.That(_superView.MetaEditor.CurrentFile, Is.SameAs(file));
            Assert.That(_superView.MetaEditor.ParsedFile, Is.Not.Null);
            Assert.That(_superView.CanCreateAnimationMetaFile, Is.False);
            Assert.That(_superView.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void AnAnimationPickedWithoutASlot_HasNoMetaNamed()
        {
            Slots.SetAnimationFile(_runner.PackFileService.FindFile(@"animations\battle\humanoid17\throt_whip_catcher\attacks\hu17_whip_catcher_attack_05.anim")!);
            Assert.That(_superView.IsAnimationMetaFileNotNamed, Is.True);
            Assert.That(_superView.CanCreateAnimationMetaFile, Is.False);
        }
    }
}
