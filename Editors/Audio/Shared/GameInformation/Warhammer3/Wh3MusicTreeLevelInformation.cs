using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    /// <summary>
    /// What to call the levels of the vanilla culture music containers, and their values, for a
    /// modder. Read off the containers in the October 2026 banks:
    ///
    ///   26264058  Battle_Result_State > Battle_Music_WH3_Culture
    ///   67383790  Battle_Music_Intensity > Battle_Music_Cindy_Scenes > Battle_Music_WH3_Culture
    ///             > Battle_Music_Key > Battle_Music_Segment > Battle_Music_Dynamic_State
    ///   698158058 WH3_Campaign_Subcultures > WH3_Campaign_Factions
    ///   145953291 WH3_Campaign_Subcultures > WH3_Campaign_Factions > WH3_Campaign_Resolution_State
    ///
    /// and for the two layers that are not decision trees, the levels <see cref="AudioProject.AmsMusicLayout"/>
    /// reads: the key for both, then Call or Response for the fragments and the variation for the pulses.
    ///
    /// The levels themselves are always read from the game; this only names them.
    /// </summary>
    public static partial class Wh3MusicTreeLevelInformation
    {
        /// <summary>
        /// The level a <see cref="AudioProject.Models.MusicTree"/> uses for which vanilla container
        /// music plays in - pre-battle and results, or in battle - for a State Group that has more
        /// than one. It is not a State Group: the game picks it by what is happening, not by a State.
        /// Values are container ids.
        /// </summary>
        public const string ContainerLevel = "Container";

        /// <summary>The musical key the campaign score is in, for the pulses and the fragments, which
        /// follow it - C, Eb or Gb. Not the State Group itself: its values come in threes vanilla never
        /// tells apart, see <see cref="AudioProject.AmsMusicLayout"/>.</summary>
        public const string AmsKeyLevel = "AmsKey";

        /// <summary>Which of a key's three pulse segments a pulse plays under, "1" to "3".</summary>
        public const string PulseVariationLevel = "PulseVariation";

        /// <summary>The vanilla containers a culture's music plays in, as a modder knows them.</summary>
        public static string GetContainerName(uint containerId) => containerId switch
        {
            26264058 => "Pre-battle and results",
            67383790 => "In battle",
            698158058 => "Campaign map",
            145953291 => "Campaign resolution",
            _ => containerId.ToString()
        };

        private static readonly Dictionary<string, string> s_levelNames = new()
        {
            [ContainerLevel] = "Part of the game",
            ["Battle_Result_State"] = "Battle result",
            ["Battle_Music_Intensity"] = "Battle phase",
            ["Battle_Music_Cindy_Scenes"] = "Cutscene",
            ["Battle_Music_Key"] = "Key",
            ["Battle_Music_Segment"] = "Segment",
            ["Battle_Music_Dynamic_State"] = "Battle situation",
            ["WH3_Campaign_Factions"] = "Faction",
            ["WH3_Campaign_Resolution_State"] = "Campaign result",
            [AmsKeyLevel] = "Key",
            [PulseVariationLevel] = "Variation",
            ["WH3_Campaign_Music_AMS_Fragments_CallResponse"] = "Call or response",
        };

        public static string GetLevelName(string level) => s_levelNames.TryGetValue(level, out var name) ? name : level;

        /// <summary>A value as a modder would say it: the battle phases are stored as
        /// "_5_Battle_Music_Action", ordered by their prefix, and that prefix means nothing to read.</summary>
        public static string GetValueName(string level, string value)
        {
            if (level == ContainerLevel && uint.TryParse(value, out var containerId))
                return GetContainerName(containerId);

            return value == null ? "Any other" : BattlePhasePrefix().Replace(value, "").TrimStart('_');
        }

        [GeneratedRegex(@"^_\d+_Battle_Music_")]
        private static partial Regex BattlePhasePrefix();
    }
}
