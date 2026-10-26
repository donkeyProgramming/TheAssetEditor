using System.Collections.Generic;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    public static class Wh3StateGroupInformation
    {
        public static readonly List<string> ModdableStateGroups = ["VO_Actor", "VO_Culture", "VO_Faction_Leader", "VO_Battle_Selection", "VO_Battle_Special_Ability"];

        /// <summary>
        /// The State Groups vanilla music Action Events set. Every culture-specific music event in
        /// battle_music__core.bnk / campaign_music__core.bnk is a SetState against one of these -
        /// music_b_faction_empire sets Battle_Music_WH3_Culture to Empire, and so on. There are no
        /// Play_ music events, which is why music Action Events skip that prefix.
        /// </summary>
        public static readonly List<string> MusicStateGroups =
        [
            "Battle_Music_WH3_Culture",
            "WH3_Campaign_Subcultures",
            "WH3_Campaign_Music_AMS_Fragments_Faction",
            "WH3_AMS_Pulse_Percussion_Options",
            "WH3_AMS_Pulse_Pitched_Orchestral_Options",
            "WH3_AMS_Pulse_Pitched_Ethnic_Options"
        ];

        /// <summary>
        /// The State Group the game turns a culture's audio_state back into a name through. Its States
        /// are the audio_state values of cultures_tables (Tomb_Kings, Undead, ...), and the battle reads
        /// the strongest alliance's culture by looking its hash up here (the call that fills
        /// Feedback_Highest_BOP_Alliance_Subculture in Warhammer3.exe). A culture whose audio_state is
        /// not registered here reaches battle_music.dat as an empty string, so the script never posts
        /// its music_b_faction_ event and the in-battle music has nothing to play - the script's
        /// "no culture has been set" path. Measured in game in October 2026: prebattle played a new
        /// culture's music and the battle was silent until it was registered here. The script's
        /// LogMessage output was not seen; no game log holds it.
        /// </summary>
        public const string CultureAudioStateGroup = "Generic_Faction_Player";

        /// <summary>
        /// The State Groups a State has to be registered in when an Action Event sets it in
        /// <paramref name="stateGroupName"/>: that Group, and for a battle culture also
        /// <see cref="CultureAudioStateGroup"/>, since a new battle culture's State is also its
        /// culture's audio_state - the wizard builds both from the one Audio State field.
        /// </summary>
        public static IReadOnlyList<string> GetStateGroupsToRegisterIn(string stateGroupName) => stateGroupName switch
        {
            "Battle_Music_WH3_Culture" => [stateGroupName, CultureAudioStateGroup],
            _ => [stateGroupName]
        };

        /// <summary>The State Groups a music project adds its own States to. Adding a State only
        /// registers a name against its Wwise hash, so what makes a new State do anything is a SetState
        /// Action Event pointing at it and a music hierarchy that branches on it.</summary>
        public static bool IsMusicProjectStateGroup(string stateGroupName) =>
            MusicStateGroups.Contains(stateGroupName) || stateGroupName == CultureAudioStateGroup;
    }
}
