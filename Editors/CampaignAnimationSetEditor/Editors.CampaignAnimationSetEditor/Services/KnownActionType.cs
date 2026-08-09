namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>Every ActionEntry.ActionType value seen across 661 vanilla WH3 cam_*.bin files
    /// (~4160 Action rows) - not defined in any game data table. Modded files may use others.
    ///
    /// ActionEntry.ActionId deliberately has no equivalent enum: the survey shows it is not a
    /// per-type code (300+ of ~3000 distinct ids are reused across different ActionTypes, and each
    /// type spans ids from single digits into the thousands). It behaves like a row counter, so the
    /// editor auto-assigns it rather than exposing it - see StatusItemViewModel.AddAction.</summary>
    public enum KnownActionType
    {
        /// <summary>521/4160 rows.</summary>
        battle_fatality,

        /// <summary>508/4160 rows.</summary>
        battle_quick_fatality,

        /// <summary>486/4160 rows.</summary>
        battle_draw,

        /// <summary>495/4160 rows.</summary>
        battle_quick_draw,

        /// <summary>527/4160 rows - the single most common value.</summary>
        response_battle_fatality,

        /// <summary>526/4160 rows.</summary>
        response_battle_quick_fatality,

        /// <summary>479/4160 rows.</summary>
        response_battle_draw,

        /// <summary>471/4160 rows.</summary>
        response_battle_quick_draw,

        /// <summary>39/4160 rows.</summary>
        death_generic,

        /// <summary>17/4160 rows.</summary>
        response_character_assassination_success,

        /// <summary>3/4160 rows.</summary>
        character_assassination_success,

        /// <summary>5/4160 rows.</summary>
        battle_attacker_killed_by_defender,

        /// <summary>5/4160 rows.</summary>
        battle_defender_killed_by_attacker,

        /// <summary>1/4160 rows.</summary>
        agent_manipulation_failure,

        /// <summary>1/4160 rows.</summary>
        level_up,
    }

    /// <summary>Action type names as a string array for XAML binding - a ComboBox's ItemsSource
    /// can't reach <see cref="System.Enum.GetNames{TEnum}"/> via x:Static.</summary>
    public static class KnownActionTypeNames
    {
        public static readonly string[] All = System.Enum.GetNames<KnownActionType>();
    }
}
