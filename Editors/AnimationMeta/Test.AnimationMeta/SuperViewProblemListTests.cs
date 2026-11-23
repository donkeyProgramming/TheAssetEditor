using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.SuperView.Timeline;
using Editors.AnimationMeta.SuperView.Visualisation;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using GameWorld.Core.Vfx;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    // Throt's ATTACK_5 META: three ANIMATED_PROPs, a docking, a splash attack, an impact position and an EFFECT whose vfx file
    // is not in the test packs.
    public class SuperViewProblemListTests
    {
        AssetEditorTestRunner _runner = null!;
        SuperViewViewModel _superView = null!;
        IMetaDataBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.CreateCaContainer();
            _runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), true);

            _superView = (SuperViewViewModel)_runner.ServiceProvider.GetRequiredService<IEditorManager>().Create(EditorEnums.SuperView_Editor)!;
            _superView.Load(new AnimationToolInput
            {
                Mesh = _runner.PackFileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition")!,
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5"),
            });
            _builder = _runner.GetRequiredServiceInCurrentEditorScope<IMetaDataBuilder>();
        }

        MetaDataEntry Tag(string name, int index = 0) => _superView.MetaEditor.Tags.Where(x => x.DisplayName == name).ElementAt(index);
        static void Set(MetaDataEntry tag, string field, string value) => tag.Variables.Single(x => x.FieldName == field).ValueAsString = value;
        List<IMetaDataInstance> Drawn => _superView.SceneObjects[0].Data.MetaDataItems;

        [Test]
        public void AVfxFileNotInThePacks_IsAProblemOnItsTag()
        {
            var effect = Tag("EFFECT_12");
            var problem = _builder.Problems.Single();
            Assert.That(problem.Source, Is.SameAs(effect._input));
            Assert.That(problem.ReasonKey, Is.EqualTo("SuperView.Problems.MissingEffect"));
            Assert.That(problem.Detail, Is.EqualTo(VfxResolver.PathOf("wh2_dlc16_entity_weapon_sparks_360")));

            var listed = _superView.Problems.Single();
            Assert.That(listed.Source, Is.SameAs(effect._input));
            Assert.That(listed.Owner, Is.EqualTo(MetaDataDocumentOwner.Animation));
            Assert.That(listed.IsError, Is.False);
        }

        [Test]
        public void APropModelNotInThePacks_IsAProblem_AndTheRestIsStillDrawn()
        {
            var before = Drawn.Count;
            var prop = Tag("ANIMATED_PROP_14", 1);
            Set(prop, "Model Name", @"variantmeshes\nothing\missing.wsmodel");

            Assert.That(_builder.Problems.Select(x => (x.Source, x.ReasonKey, x.Detail)),
                Does.Contain(((object)prop._input, "SuperView.Problems.MissingModel", @"variantmeshes\nothing\missing.wsmodel")));
            Assert.That(Drawn, Has.Count.EqualTo(before - 1));
            Assert.That(_superView.Problems.Select(x => x.Source), Does.Contain(prop._input));
        }

        [Test]
        public void APropAnimationNotInThePacks_IsAProblem_AndThePropIsStillDrawn()
        {
            var before = Drawn.Count;
            var prop = Tag("ANIMATED_PROP_14", 1);
            Set(prop, "Animation Name", @"animations\nothing\missing.anim");

            Assert.That(_builder.Problems.Select(x => (x.Source, x.ReasonKey, x.Detail)),
                Does.Contain(((object)prop._input, "SuperView.Problems.MissingPropAnimation", @"animations\nothing\missing.anim")));
            Assert.That(Drawn, Has.Count.EqualTo(before));
        }

        // A corridor 0 wide used to throw out of the rebuild and leave the scene half built.
        [Test]
        public void ASplashAttackThatCannotBeDrawn_IsAProblem_NotAnException()
        {
            var before = Drawn.Count;
            var splash = Tag("SPLASH_ATTACK_10");
            Assert.DoesNotThrow(() => Set(splash, "Width For Corridor", "0"));

            Assert.That(_builder.Problems.Where(x => x.Source == splash._input).Select(x => x.ReasonKey), Is.EqualTo(new[] { "SuperView.Problems.PreviewFailed" }));
            Assert.That(Drawn, Has.Count.EqualTo(before - 1));
        }

        [Test]
        public void AFieldThatIsNotAValidValue_IsAnError_UntilItIsPutRight()
        {
            var impact = Tag("IMPACT_POS_10");
            var startTime = impact.Variables.Single(x => x.FieldName == "Start Time").ValueAsString;

            Set(impact, "Start Time", "abc");
            var error = _superView.Problems.Single(x => x.Source == impact._input);
            Assert.That(error.IsError, Is.True);
            Assert.That(_superView.HasProblems, Is.True);

            Set(impact, "Start Time", startTime);
            Assert.That(_superView.Problems.Select(x => x.Source), Does.Not.Contain(impact._input));
        }

        [Test]
        public void AStartTimeThatIsNotANumber_IsAProblem()
        {
            var impact = Tag("IMPACT_POS_10");
            Set(impact, "Start Time", "NaN");
            var problem = _superView.Problems.Single(x => x.Source == impact._input);
            Assert.That(problem.IsError, Is.False);
        }

        // Opening the tag rebuilds the scene; the effect is kept as it was built, and its problem with it, so the line stays picked.
        [Test]
        public void PickingAProblem_OpensItsTag_AndStaysPicked()
        {
            _superView.SelectedTabControllerIndex = 0;
            var problem = _superView.Problems.Single();

            _superView.SelectedProblem = problem;

            Assert.That(_superView.SelectedTabControllerIndex, Is.EqualTo(1));
            Assert.That(_superView.MetaEditor.SelectedTag!._input, Is.SameAs(problem.Source));
            Assert.That(_superView.Problems, Is.EqualTo(new[] { problem }));
            Assert.That(_superView.SelectedProblem, Is.SameAs(problem));
        }
    }
}
