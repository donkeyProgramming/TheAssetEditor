using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Test.Audio
{
    // Pins the event-name to State Group mapping the wizard depends on. The pairings were read
    // out of the shipped banks, not inferred from the names.
    internal class MusicAudioProjectServiceTests
    {
        [TestCase("music_b_faction_empire", "Battle_Music_WH3_Culture", "empire")]
        [TestCase("music_c_subculture_empire", "WH3_Campaign_Subcultures", "empire")]
        [TestCase("music_c_ams_empire", "WH3_Campaign_Music_AMS_Fragments_Faction", "empire")]
        [TestCase("music_c_ams_pulse_perc_cathay", "WH3_AMS_Pulse_Percussion_Options", "cathay")]
        [TestCase("music_c_ams_pulse_orch_cathay", "WH3_AMS_Pulse_Pitched_Orchestral_Options", "cathay")]
        [TestCase("music_c_ams_pulse_ethnic_cathay", "WH3_AMS_Pulse_Pitched_Ethnic_Options", "cathay")]
        public void VanillaEventNamesResolveToTheStateGroupTheyActuallySet(string eventName, string expectedStateGroup, string expectedState)
        {
            var resolved = Wh3MusicEventInformation.TryResolveStateTarget(eventName, out var stateGroupName, out var stateName);

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.True);
                Assert.That(stateGroupName, Is.EqualTo(expectedStateGroup));
                Assert.That(stateName, Is.EqualTo(expectedState));
            });
        }

        [Test]
        public void PulsePrefixesWinOverTheAmbientPrefixTheyStartWith()
        {
            // "music_c_ams_" is a prefix of "music_c_ams_pulse_perc_", so a naive first match
            // would resolve this to the ambient group with a State called "pulse_perc_my_culture".
            Wh3MusicEventInformation.TryResolveStateTarget("music_c_ams_pulse_perc_my_culture", out var stateGroupName, out var stateName);

            Assert.Multiple(() =>
            {
                Assert.That(stateGroupName, Is.EqualTo("WH3_AMS_Pulse_Percussion_Options"));
                Assert.That(stateName, Is.EqualTo("my_culture"));
            });
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Play_something_else")]
        [TestCase("music_b_faction_")] // prefix with no culture after it
        public void NamesThatFollowNoVanillaPatternAreRejectedRatherThanGuessedAt(string eventName)
        {
            Assert.That(Wh3MusicEventInformation.TryResolveStateTarget(eventName, out _, out _), Is.False);
        }

        [Test]
        public void EveryStateGroupTheMappingTargetsIsModdable()
        {
            foreach (var prefix in Wh3MusicEventInformation.KnownEventPrefixes)
            {
                Wh3MusicEventInformation.TryResolveStateTarget($"{prefix}test_culture", out var stateGroupName, out _);

                // A State can only be added to a State Group the project knows about, so a mapping
                // pointing at one that isn't moddable would produce an event selecting a State that
                // never gets registered.
                Assert.That(Wh3StateGroupInformation.MusicStateGroups, Does.Contain(stateGroupName),
                    $"'{prefix}' maps to '{stateGroupName}', which is not in MusicStateGroups");
            }
        }

        [Test]
        public void ABattleCultureIsAlsoRegisteredAsACultureAudioState()
        {
            // The battle hands battle_music.dat the strongest side's culture by turning its audio_state
            // hash back into a name through Generic_Faction_Player. A new culture missing from that
            // Group arrives as an empty string and the in-battle music goes silent.
            Wh3MusicEventInformation.TryResolveStateTarget("music_b_faction_test_culture", out var stateGroupName, out _);

            var stateGroups = Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName);

            Assert.Multiple(() =>
            {
                Assert.That(stateGroups, Is.EqualTo(new[] { "Battle_Music_WH3_Culture", "Generic_Faction_Player" }));
                Assert.That(Wh3StateGroupInformation.IsMusicProjectStateGroup("Generic_Faction_Player"),
                    "a project would have no Group to register the culture in");
            });
        }

        [TestCase("music_c_subculture_test_culture")]
        [TestCase("music_c_ams_pulse_perc_test_culture")]
        public void OtherMusicStatesAreRegisteredInTheirOwnGroupOnly(string eventName)
        {
            Wh3MusicEventInformation.TryResolveStateTarget(eventName, out var stateGroupName, out _);

            Assert.That(Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName), Is.EqualTo(new[] { stateGroupName }));
        }
    }
}
