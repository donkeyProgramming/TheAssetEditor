namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>Every status name seen across 661 vanilla WH3 cam_*.bin files. Member names match
    /// the on-disk strings exactly. Documents what vanilla ships - it is not an enforced whitelist,
    /// and modded files may use other names.</summary>
    public enum KnownCampaignStatus
    {
        /// <summary>Not an animation status. Holds equipment docks, persistent metadata and hand
        /// poses shared across every other status on this skeleton. In 428/661 files.</summary>
        global,

        /// <summary>The default set used on the campaign map. In 655/661 files.</summary>
        status_normal,

        /// <summary>Used on the pre-battle deployment/loading screen. In 561/661 files.</summary>
        status_battle,

        /// <summary>In 194/661 files, always alongside status_battle and the stance statuses. Since
        /// it duplicates status_normal the way the stance statuses do, it's more likely used in the
        /// pre-battle UI on the campaign layer than in the battle deployment screen - unconfirmed.</summary>
        status_deploy,

        /// <summary>In 21/661 files, always alongside status_battle. Likely an attacker-specific
        /// variant of status_battle - unconfirmed.</summary>
        status_battle_attacker,

        /// <summary>In 13/661 files, always alongside status_battle. Likely a defender-specific
        /// variant of status_battle - unconfirmed.</summary>
        status_battle_defender,

        /// <summary>In 10/661 files. Likely used when the army has no action points left this
        /// turn - unconfirmed.</summary>
        status_out_of_ap_normal,

        // The status_stance_* block overrides status_normal for the matching campaign stance. In
        // vanilla these almost always just repeat status_normal's own Idle entry.
        status_stance_march,
        status_stance_ambush,
        status_stance_raid,
        status_stance_camp,
        status_stance_muster,
        status_stance_siege,
        status_stance_blockade,
        status_stance_channeling,
        status_stance_set_camp_raiding,
        status_stance_tunneling,
        status_stance_raise_dead,
    }

    public static class KnownCampaignStatusExtensions
    {
        const string StancePrefix = "status_stance_";

        /// <summary>Tooltip text for the editor's status list, mirroring the survey findings
        /// documented on each <see cref="KnownCampaignStatus"/> member.</summary>
        public static string Describe(this KnownCampaignStatus status)
        {
            var name = status.ToString();
            if (name.StartsWith(StancePrefix))
            {
                var stance = name[StancePrefix.Length..].Replace('_', ' ');
                return $"Overrides status_normal while the army's campaign stance is set to '{stance}'. " +
                       "In vanilla files this almost always just repeats status_normal's own Idle entry.";
            }

            return status switch
            {
                KnownCampaignStatus.global => "Not an animation status - holds equipment docks, persistent metadata and hand poses shared across every other status on this skeleton.",
                KnownCampaignStatus.status_normal => "The default set used on the campaign map. Present in almost every vanilla file (655/661 surveyed).",
                KnownCampaignStatus.status_battle => "Used on the pre-battle deployment/loading screen, before the fight itself starts. Present in most vanilla files (561/661 surveyed).",
                KnownCampaignStatus.status_deploy => "Seen in 194/661 surveyed files, always alongside status_battle and the stance statuses. Since it duplicates status_normal's animations the way the stance statuses do, it's more likely used in the pre-battle UI while still on the campaign layer than inside the battle deployment screen itself - not confirmed.",
                KnownCampaignStatus.status_battle_attacker => "Seen in 21/661 surveyed files, always alongside status_battle. Likely an attacker-specific variant of status_battle for the pre-battle screen - not confirmed.",
                KnownCampaignStatus.status_battle_defender => "Seen in 13/661 surveyed files, always alongside status_battle. Likely a defender-specific variant of status_battle for the pre-battle screen - not confirmed.",
                KnownCampaignStatus.status_out_of_ap_normal => "Seen in 10/661 surveyed files. Likely used when the army/lord has no action points left on the campaign map this turn - not confirmed.",
                _ => UnknownStatusDescription,
            };
        }

        public const string UnknownStatusDescription = "Non-standard status name - not one of the patterns seen across the surveyed vanilla files.";
    }
}
