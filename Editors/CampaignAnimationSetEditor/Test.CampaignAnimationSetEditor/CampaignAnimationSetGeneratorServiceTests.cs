using System.Collections.Generic;
using Editors.CampaignAnimationSetEditor.Services;
using Shared.GameFormats.AnimationPack;

namespace Test.CampaignAnimationSetEditor
{
    [TestFixture]
    public class CampaignAnimationSetGeneratorServiceTests
    {
        static AnimationBinEntryGenericFormat Entry(string slotName, string animationFile) => new()
        {
            SlotName = slotName,
            AnimationFile = animationFile,
        };

        [Test]
        public void FindEntry_PrefersExactNameOverPrefixMatch()
        {
            var entries = new List<AnimationBinEntryGenericFormat>
            {
                Entry("STAND_IDLE_3", "idle3.anim"),
                Entry("STAND_IDLE_1", "idle1.anim"),
            };

            var result = CampaignAnimationSetGeneratorService.FindEntry(entries, ["STAND_IDLE_1"], ["STAND_IDLE_"]);

            Assert.That(result!.AnimationFile, Is.EqualTo("idle1.anim"));
        }

        [Test]
        public void FindEntry_FallsBackToPrefixWhenNoExactMatch()
        {
            var entries = new List<AnimationBinEntryGenericFormat>
            {
                Entry("STAND_IDLE_7", "idle7.anim"),
            };

            var result = CampaignAnimationSetGeneratorService.FindEntry(entries, ["STAND_IDLE_1"], ["STAND_IDLE_"]);

            Assert.That(result!.AnimationFile, Is.EqualTo("idle7.anim"));
        }

        [Test]
        public void FindEntry_ReturnsNullWhenNothingMatches()
        {
            var entries = new List<AnimationBinEntryGenericFormat> { Entry("RUN_1", "run.anim") };

            var result = CampaignAnimationSetGeneratorService.FindEntry(entries, ["STAND_IDLE_1"], ["STAND_IDLE_"]);

            Assert.That(result, Is.Null);
        }

        [TestCase("animations/battle/bird02/locomotion/bi2_walk_01.anim", "animations/battle/bird02/locomotion/campaign/cam_bi2_walk_01.anim")]
        [TestCase("animations/battle/humanoid01d/spear_and_shield/hu1d_sps_run_01.anim", "animations/battle/humanoid01d/spear_and_shield/campaign/cam_hu1d_sps_run_01.anim")]
        public void BuildCampaignAnimationPath_InsertsCampaignFolderAndCamPrefix(string source, string expected)
        {
            var result = CampaignAnimationSetGeneratorService.BuildCampaignAnimationPath(source);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildCampaignAnimationPath_DoesNotDoublePrefixAnAlreadyCamNamedFile()
        {
            var result = CampaignAnimationSetGeneratorService.BuildCampaignAnimationPath("animations/battle/bird02/locomotion/cam_bi2_walk_01.anim");

            Assert.That(result, Is.EqualTo("animations/battle/bird02/locomotion/campaign/cam_bi2_walk_01.anim"));
        }

        [Test]
        public void IsFlyingMoveset_TrueWhenGroundFlyStandPresent()
        {
            var entries = new List<AnimationBinEntryGenericFormat> { Entry("FLY_STAND", "fly_stand.anim") };

            Assert.That(CampaignAnimationSetGeneratorService.IsFlyingMoveset(entries, ""), Is.True);
        }

        [Test]
        public void IsFlyingMoveset_TrueWhenRiderFlyStandPresent()
        {
            var entries = new List<AnimationBinEntryGenericFormat> { Entry("RIDER_FLY_STAND", "rider_fly_stand.anim") };

            Assert.That(CampaignAnimationSetGeneratorService.IsFlyingMoveset(entries, "RIDER_"), Is.True);
        }

        [Test]
        public void IsFlyingMoveset_FalseWhenOnlyOtherFlySlotsPresentWithoutFlyStand()
        {
            // FLY_COMBAT_IDLE_1/FLY_WALK_1 without FLY_STAND itself shouldn't count - FLY_STAND is
            // the one slot the fallback logic depends on actually existing.
            var entries = new List<AnimationBinEntryGenericFormat>
            {
                Entry("FLY_COMBAT_IDLE_1", "fly_combat_idle.anim"),
                Entry("FLY_WALK_1", "fly_walk.anim"),
            };

            Assert.That(CampaignAnimationSetGeneratorService.IsFlyingMoveset(entries, ""), Is.False);
        }

        [Test]
        public void IsFlyingMoveset_FalseForGroundFlyStandWhenCheckingRiderPrefix()
        {
            // A ground FLY_STAND shouldn't count when checking under the rider prefix - detection
            // must be scoped to the correct skeleton context.
            var entries = new List<AnimationBinEntryGenericFormat> { Entry("FLY_STAND", "fly_stand.anim") };

            Assert.That(CampaignAnimationSetGeneratorService.IsFlyingMoveset(entries, "RIDER_"), Is.False);
        }
    }
}
