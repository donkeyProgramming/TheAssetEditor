using System;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    /// <summary>
    /// Maps a music event name onto the State Group its SetState action targets.
    ///
    /// Vanilla names these events as a fixed prefix plus the culture: music_b_faction_empire sets
    /// Battle_Music_WH3_Culture to Empire, music_c_ams_pulse_perc_cathay sets
    /// WH3_AMS_Pulse_Percussion_Options to Cathay, and so on. The music scripts post them by that
    /// bare name, which is why they carry no Play_ prefix.
    /// </summary>
    public static class Wh3MusicEventInformation
    {
        // Ordered longest prefix first, because the pulse prefixes all start with the ambient
        // one - matching "music_c_ams_" against "music_c_ams_pulse_perc_cathay" would otherwise
        // win and leave a State called "pulse_perc_cathay".
        private static readonly (string Prefix, string StateGroup)[] s_stateGroupByEventPrefix =
        [
            ("music_c_ams_pulse_ethnic_", "WH3_AMS_Pulse_Pitched_Ethnic_Options"),
            ("music_c_ams_pulse_orch_", "WH3_AMS_Pulse_Pitched_Orchestral_Options"),
            ("music_c_ams_pulse_perc_", "WH3_AMS_Pulse_Percussion_Options"),
            ("music_c_subculture_", "WH3_Campaign_Subcultures"),
            ("music_c_ams_", "WH3_Campaign_Music_AMS_Fragments_Faction"),
            ("music_b_faction_", "Battle_Music_WH3_Culture")
        ];

        /// <summary>
        /// Works out which State Group an event sets and which State it sets it to. Returns false
        /// for a name that follows none of the vanilla patterns, since guessing a State Group for
        /// it would produce an event that silently does nothing.
        /// </summary>
        public static bool TryResolveStateTarget(string eventName, out string stateGroupName, out string stateName)
        {
            stateGroupName = null;
            stateName = null;

            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            foreach (var (prefix, stateGroup) in s_stateGroupByEventPrefix)
            {
                if (!eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var culture = eventName[prefix.Length..];
                if (string.IsNullOrWhiteSpace(culture))
                    return false;

                stateGroupName = stateGroup;
                stateName = culture;
                return true;
            }

            return false;
        }

        /// <summary>The State Group every culture's event with this prefix sets, or null if the
        /// prefix matches no vanilla pattern.</summary>
        public static string GetStateGroupForPrefix(string eventPrefix) =>
            s_stateGroupByEventPrefix.FirstOrDefault(entry => eventPrefix?.StartsWith(entry.Prefix, StringComparison.OrdinalIgnoreCase) == true).StateGroup;

        public static IReadOnlyList<string> KnownEventPrefixes =>
            s_stateGroupByEventPrefix.Select(entry => entry.Prefix).ToList();
    }
}
