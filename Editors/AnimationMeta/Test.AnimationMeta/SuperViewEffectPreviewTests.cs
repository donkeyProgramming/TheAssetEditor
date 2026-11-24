using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.SuperView.Visualisation;
using Editors.AnimationMeta.SuperView.Visualisation.Instances;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    // Throt's ATTACK_5 EFFECT_12 is marked Distance Culled, has type normal (0), and names a vfx file the test packs lack, so
    // the vfx it plays shows in its problem.
    public class SuperViewEffectPreviewTests
    {
        AssetEditorTestRunner _runner = null!;
        SuperViewViewModel _superView = null!;
        IMetaDataBuilder _builder = null!;
        MetaDataEntry _effect = null!;

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
            _effect = _superView.MetaEditor.Tags.Single(x => x.DisplayName == "EFFECT_12");
        }

        int EffectsDrawn => _superView.SceneObjects[0].Data.MetaDataItems.OfType<VfxEffectInstance>().Count();
        string PlayedVfx => _builder.Problems.Single(x => x.Source == _effect._input).Detail;
        void SetType(int type) => _effect.Variables.Single(x => x.FieldName == "Effect Type").ValueAsString = type.ToString();

        [Test]
        public void AsADistantUnit_DistanceCulledEffectsAreNotDrawn()
        {
            Assert.That(EffectsDrawn, Is.EqualTo(1));
            _superView.HideDistanceCulledEffects = true;
            Assert.That(EffectsDrawn, Is.EqualTo(0));
            Assert.That(_superView.Problems, Is.Empty, "a tag not drawn has nothing missing");
            _superView.HideDistanceCulledEffects = false;
            Assert.That(EffectsDrawn, Is.EqualTo(1));
        }

        [Test]
        public void AsADistantUnit_EffectsNotMarkedDistanceCulledAreStillDrawn()
        {
            _effect.Variables.Single(x => x.FieldName == "Distance Culled").ValueAsString = "False";
            _superView.HideDistanceCulledEffects = true;
            Assert.That(EffectsDrawn, Is.EqualTo(1));
        }

        [Test]
        public void TheAuraVfx_PlaysAtAbilityAuraTagsOnly()
        {
            _superView.AuraVfx = "test_aura";
            Assert.That(PlayedVfx, Is.EqualTo("vfx/wh2_dlc16_entity_weapon_sparks_360.xml"), "type normal");

            SetType(1);
            Assert.That(PlayedVfx, Is.EqualTo("vfx/test_aura.xml"), "type ability_aura");

            SetType(2);
            Assert.That(PlayedVfx, Is.EqualTo("vfx/wh2_dlc16_entity_weapon_sparks_360.xml"), "type ability_weapon");
        }

        [Test]
        public void TheWeaponVfx_PlaysAtAbilityWeaponTags_AndClearingItPutsTheTagsOwnBack()
        {
            SetType(2);
            _superView.WeaponVfx = "test_weapon";
            Assert.That(PlayedVfx, Is.EqualTo("vfx/test_weapon.xml"));

            _superView.ClearWeaponVfxCommand.Execute(null);
            Assert.That(PlayedVfx, Is.EqualTo("vfx/wh2_dlc16_entity_weapon_sparks_360.xml"));
        }

        [Test]
        public void TheOverrides_AreNotWrittenIntoTheMeta()
        {
            SetType(1);
            _superView.MetaEditor.HasUnsavedChanges = false;
            _superView.AuraVfx = "test_aura";
            Assert.That(((Shared.GameFormats.AnimationMeta.Definitions.IEffectMeta)_effect._input).VfxName, Is.EqualTo("wh2_dlc16_entity_weapon_sparks_360"));
            Assert.That(_superView.HasUnsavedChanges, Is.False);
        }
    }
}
