using Shared.GameFormats.AnimationPack;

namespace GameWorld.Core.Test.Utility
{
    // WH3 fragment .bin files store only the slot id, so the id-to-name table has to match the game's.
    // The values are from RPFM's anim_ids_warhammer_3.csv (patch 9.0).
    public class AnimationSlotsWh3Tests
    {
        [TestCase(0, "MISSING_ANIM")]
        [TestCase(1182, "RIDER_REBIRTH_FLYING")]
        [TestCase(1183, "TELEPORT_ARRIVAL")]
        [TestCase(1184, "TELEPORT_DEPARTURE")]
        [TestCase(1185, "WAVERING_STAND_LOOK_FORWARD_1")]
        [TestCase(2291, "BEAM_TEST_LOOPED")]
        public void SlotIdMapsToPatch9Name(int id, string expectedName)
        {
            Assert.That(AnimationSlotTypeHelperWh3.GetFromId(id).Value, Is.EqualTo(expectedName));
        }

        [Test]
        public void SlotTableHasEveryPatch9Id()
        {
            Assert.That(AnimationSlotTypeHelperWh3.Values, Has.Count.EqualTo(2292));
        }
    }
}
