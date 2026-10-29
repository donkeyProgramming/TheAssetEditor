using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise.Generators.Hirc.V136;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Action = Editors.Audio.Shared.AudioProject.Models.Action;

namespace Test.Audio
{
    // The shape asserted here was read out of the shipped banks:
    // every culture music Action Event in battle_music__core.bnk / campaign_music__core.bnk holds
    // exactly one SetState action, with the state id in IdExt as well as in the state params, both
    // property bundles empty, and a section size of 21.
    internal class SetStateActionGenerationTests
    {
        // music_b_faction_empire -> Battle_Music_WH3_Culture = Empire
        const string StateGroupName = "Battle_Music_WH3_Culture";
        const string StateName = "Empire";
        const uint VanillaStateGroupId = 964666289;
        const uint VanillaStateId = 1228200377;
        const uint VanillaSectionSize = 21;

        [Test]
        public void StateGroupAndStateNamesHashToTheVanillaIds()
        {
            Assert.Multiple(() =>
            {
                Assert.That(WwiseHash.Compute(StateGroupName), Is.EqualTo(VanillaStateGroupId));
                Assert.That(WwiseHash.Compute(StateName), Is.EqualTo(VanillaStateId));
            });
        }

        [Test]
        public void GeneratedSetStateActionMatchesTheVanillaShape()
        {
            var hirc = GenerateSetStateAction();

            Assert.Multiple(() =>
            {
                Assert.That(hirc.ActionType, Is.EqualTo(AkActionType.SetState));
                Assert.That(hirc.IdExt, Is.EqualTo(VanillaStateId), "vanilla repeats the state id in IdExt");
                Assert.That(hirc.IdExt4, Is.EqualTo(0));
                Assert.That(hirc.AkPropBundle0.PropsList, Is.Empty);
                Assert.That(hirc.AkPropBundle1.PropsList, Is.Empty);
                Assert.That(hirc.StateActionParams!.StateGroupId, Is.EqualTo(VanillaStateGroupId));
                Assert.That(hirc.StateActionParams.TargetStateId, Is.EqualTo(VanillaStateId));
                Assert.That(hirc.SectionSize, Is.EqualTo(VanillaSectionSize));
            });
        }

        [Test]
        public void GeneratedSetStateActionSurvivesAWriteReadRoundTrip()
        {
            var hirc = GenerateSetStateAction();
            var written = hirc.WriteData();

            var reloaded = new CAkAction_V136();
            reloaded.ReadHirc(new Shared.ByteParsing.ByteChunk(written));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(hirc.Id));
                Assert.That(reloaded.ActionType, Is.EqualTo(AkActionType.SetState));
                Assert.That(reloaded.StateActionParams!.StateGroupId, Is.EqualTo(VanillaStateGroupId));
                Assert.That(reloaded.StateActionParams.TargetStateId, Is.EqualTo(VanillaStateId));

                // The written bytes are the header plus the section, and the section size the
                // generator computed has to account for exactly the rest.
                Assert.That(written, Has.Length.EqualTo(Shared.GameFormats.Wwise.Hirc.HircHeader.PrefixSize + VanillaSectionSize));
            });
        }

        static CAkAction_V136 GenerateSetStateAction()
        {
            var action = Action.CreateSetState(id: 12345, StateGroupName, StateName);

            // Global music is the sound bank the music events live in, and is also the one case
            // that adds a transition-time property - SetState actions have to stay clear of it.
            var soundBank = new SoundBank("music_test", Wh3SoundBank.GlobalMusic, Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx));

            return (CAkAction_V136)new ActionHircGenerator_V136().GenerateHirc(action, soundBank);
        }
    }
}
