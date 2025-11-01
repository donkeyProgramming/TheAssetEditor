using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3DialogueEventType;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3DialogueEventUnitProfile;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3SoundBank;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    // DialogueEventType is an interpretation of what part of the game a Dialogue Event relates to,
    // so Dialogue Events with the CharacterMovement type relate to when a character moves.
    public enum Wh3DialogueEventType
    {
        [Display(Name = "Show All")] TypeShowAll,
        [Display(Name = "Character Selection")] CharacterSelection,
        [Display(Name = "Character Movement")] CharacterMovement,
        [Display(Name = "Character Skill Tree")] CharacterSkillTree,
        [Display(Name = "Character Confidence")] CharacterConfidence,
        [Display(Name = "Character Environment")] CharacterEnvironment,
        [Display(Name = "Character Details")] CharacterDetails,
        [Display(Name = "Character Misc")] CharacterMisc,
        [Display(Name = "Agent Actions")] AgentActions,
        [Display(Name = "Army Actions")] ArmyActions,
        [Display(Name = "Army Stances")] ArmyStances,
        [Display(Name = "Pre Battle Action")] BattleAction,
        [Display(Name = "Post Battle Result")] PostBattleResult,
        [Display(Name = "Post Battle Settlement Action")] PostBattleSettlementAction,
        [Display(Name = "Post Battle Captives Action")] PostBattleCaptivesAction,
        [Display(Name = "Technology Tree")] TechnologyTree,
        [Display(Name = "Diplomacy Barks")] DiplomacyBarks,
        [Display(Name = "Gotrek and Felix")] GotrekAndFelix,
        [Display(Name = "Creature")] Creature,
        [Display(Name = "City Details")] CityDetails,
        [Display(Name = "Mechanic - Plagues of Nurgle")] PlaguesOfNurgleMechanic,
        [Display(Name = "Mechanic - Forbidden Workshop")] ForbiddenWorkshopMechanic,
        [Display(Name = "Mechanic - Hell-Forge")] HellforgeMechanic,
        [Display(Name = "Mechanic - TzArkan")] TzArkanMechanic,
        [Display(Name = "Mechanic - Sacrifices To Sotek")] SacrificesToSotekMechanic,
        [Display(Name = "Mechanic - Summon the Elector Counts")] SummonTheElectorCountsMechanic,
        [Display(Name = "Mechanic - Daith's Forge")] DaithsForgeMechanic,
        [Display(Name = "Mechanic - Waagh")] WaaghMechanic,
        [Display(Name = "Mechanic - Wrath of Khorne")] WrathOfKhorneMechanic,
        [Display(Name = "Mechanic - Mercenary Contracts")] MercenaryContractsMechanic,
        [Display(Name = "Mechanic - Meat")] MeatMechanic,
        [Display(Name = "Mechanic - Gork and Mork Dedications")] GorkAndMorkDedicationsMechanic,
        [Display(Name = "Mechanic - Scrap")] ScrapMechanic,
        [Display(Name = "Mechanic - Skull Throne")] SkullThroneMechanic,
        [Display(Name = "Mechanic - Monster Pens")] MonsterPensMechanic,
        [Display(Name = "Unit Movement")] UnitMovement,
        [Display(Name = "Unit Attack")] UnitAttack,
        [Display(Name = "Unit Selection")] UnitSelection,
        [Display(Name = "Unit Generic")] UnitGeneric,
        [Display(Name = "Unit Misc")] UnitMisc,
        [Display(Name = "Battle Orders")] BattleOrders,
        [Display(Name = "Battle Mode")] BattleMode,
        [Display(Name = "Battle Controls")] BattleControls,
        [Display(Name = "Unit Environment")] UnitEnvironment,
        [Display(Name = "Unit Events")] UnitEvents,
        [Display(Name = "Own Unit Events")] OwnUnitEvents,
        [Display(Name = "Own Unit Events (Skaven)")] SkavenOwnUnitEvents,
        [Display(Name = "Allied Unit Events")] AlliedUnitEvents,
        [Display(Name = "Enemy Unit Events")] EnemyUnitEvents,
        [Display(Name = "Own Army Events")] OwnArmyEvents,
        [Display(Name = "Own Army Events (High Elves)")] HighElvesOwnArmyEvents,
        [Display(Name = "Own Army Events (Dark Elves)")] DarkElvesOwnArmyEvents,
        [Display(Name = "Own Army Events (Lizardmen)")] LizardmenOwnArmyEvents,
        [Display(Name = "Enemy Army Events")] EnemyArmyEvents,
        [Display(Name = "Battle Type")] BattleType
    }

    // DialogueEventUnitProfile is an interpretation of how much a Dialogue Event is needed in order to have a 'functioning' unit
    // Minimum: The Dialogue Events a unit should use in order to have basic functioning audio
    // Recommended: The Dialogue Events a unit should use in order to have a good level of audio
    // Complete: All the Dialogue Events a unit can use
    public enum Wh3DialogueEventUnitProfile
    {
        [Display(Name = "Show All")] ProfileShowAll,
        [Display(Name = "Culture - Minimum")] CultureRecommended,
        [Display(Name = "Lord - Complete")] LordComplete,
        [Display(Name = "Lord - Recommended")] LordRecommended,
        [Display(Name = "Lord - Minimum")] LordMinimum,
        [Display(Name = "Hero - Complete")] HeroComplete,
        [Display(Name = "Hero - Recommended")] HeroRecommended,
        [Display(Name = "Hero - Minimum")] HeroMinimum,
        [Display(Name = "Unit - Complete")] UnitComplete,
        [Display(Name = "Unit - Recommended")] UnitRecommended,
        [Display(Name = "Unit - Minimum")] UnitMinimum,
    }

    public record Wh3DialogueEventDefinition(string Name, Wh3SoundBank SoundBank, Wh3DialogueEventType[] DialogueEventTypes, Wh3DialogueEventUnitProfile[] UnitProfiles, uint ActorMixerId);

    public record Wh3FrontendVODefinition(
        string Name,
        Wh3SoundBank SoundBank,
        Wh3DialogueEventType[] DialogueEventTypes,
        Wh3DialogueEventUnitProfile[] UnitProfiles,
        uint ActorMixerId = 745637913) : Wh3DialogueEventDefinition(Name, SoundBank, DialogueEventTypes, UnitProfiles, ActorMixerId);

    public record Wh3CampaignVODefinition(
        string Name,
        Wh3SoundBank SoundBank,
        Wh3DialogueEventType[] DialogueEventTypes,
        Wh3DialogueEventUnitProfile[] UnitProfiles,
        uint ActorMixerId = 306196174) : Wh3DialogueEventDefinition(Name, SoundBank, DialogueEventTypes, UnitProfiles, ActorMixerId);

    public record Wh3CampaignVOConversationalDefinition(
        string Name,
        Wh3SoundBank SoundBank,
        Wh3DialogueEventType[] DialogueEventTypes,
        Wh3DialogueEventUnitProfile[] UnitProfiles,
        uint ActorMixerId = 652491101) : Wh3DialogueEventDefinition(Name, SoundBank, DialogueEventTypes, UnitProfiles, ActorMixerId);

    public record Wh3BattleVOOrdersDefinition(
        string Name,
        Wh3SoundBank SoundBank,
        Wh3DialogueEventType[] DialogueEventTypes,
        Wh3DialogueEventUnitProfile[] UnitProfiles,
        uint ActorMixerId = 1009314120) : Wh3DialogueEventDefinition(Name, SoundBank, DialogueEventTypes, UnitProfiles, ActorMixerId);

    public record Wh3BattleVOConversationalDefinition(
        string Name,
        Wh3SoundBank SoundBank,
        Wh3DialogueEventType[] DialogueEventTypes,
        Wh3DialogueEventUnitProfile[] UnitProfiles,
        uint ActorMixerId = 600762068) : Wh3DialogueEventDefinition(Name, SoundBank, DialogueEventTypes, UnitProfiles, ActorMixerId);

    public static class Wh3DialogueEventInformation
    {
        private static List<Wh3DialogueEventUnitProfile> CompleteProfiles { get; } =
        [
            LordComplete,
            HeroComplete,
            UnitComplete,
        ];

        private static List<Wh3DialogueEventUnitProfile> RecommendedProfiles { get; } =
        [
            LordRecommended,
            HeroRecommended,
            UnitRecommended,
            LordComplete,
            HeroComplete,
            UnitComplete,
        ];

        private static List<Wh3DialogueEventUnitProfile> MinimumProfiles { get; } =
        [
            LordMinimum,
            HeroMinimum,
            UnitMinimum,
            LordRecommended,
            HeroRecommended,
            UnitRecommended,
            LordComplete,
            HeroComplete,
            UnitComplete
        ];

        private static List<Wh3DialogueEventUnitProfile> LordHeroCompleteProfiles { get; } =
        [
            LordComplete,
            HeroComplete
        ];

        private static List<Wh3DialogueEventUnitProfile> LordHeroRecommendedProfiles { get; } =
        [
            LordRecommended,
            HeroRecommended,
            LordComplete,
            HeroComplete,
        ];

        private static List<Wh3DialogueEventUnitProfile> LordHeroMinimumProfiles { get; } =
        [
            LordMinimum,
            HeroMinimum,
            LordRecommended,
            HeroRecommended,
            LordComplete,
            HeroComplete
        ];

        public static List<Wh3FrontendVODefinition> Frontend { get; } =
        [
            new("frontend_vo_character_select", FrontendVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, LordMinimum])
        ];

        public static List<Wh3CampaignVODefinition> Campaign { get; } =
        [
            new("campaign_vo_selected_first_time", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            // triggered when a character is initially selected
            new("campaign_vo_selected", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            // triggered when a character is selected again while already selected
            new("campaign_vo_selected_short", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_selected_fail", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_selected_neutral", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_selected_allied", CampaignVO, [TypeShowAll, CharacterSelection], [ProfileShowAll, ..LordHeroRecommendedProfiles]),

            new("campaign_vo_move", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_move_next_turn", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_move_garrisoning", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_ship_dock", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_attack", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, LordComplete, LordRecommended, LordMinimum]),
            new("campaign_vo_retreat", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, LordComplete, LordRecommended, LordMinimum]),
            new("campaign_vo_no", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_no_short", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_yes", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_yes_short", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            new("campaign_vo_yes_short_aggressive", CampaignVO, [TypeShowAll, CharacterMovement], [ProfileShowAll, ..LordHeroMinimumProfiles]),
            
            new("campaign_vo_cam_skill_weapon_tree", CampaignVO, [TypeShowAll, CharacterSkillTree], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cam_skill_weapon_tree_response", CampaignVO, [TypeShowAll, CharacterSkillTree], [ProfileShowAll, ..LordHeroCompleteProfiles]),

            new("campaign_vo_created", CampaignVO, [TypeShowAll, CharacterMisc], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_level_up", CampaignVO, [TypeShowAll, CharacterMisc], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_special_ability", CampaignVO, [TypeShowAll, CharacterMisc], [ProfileShowAll, ..LordHeroCompleteProfiles]),

            new("campaign_vo_agent_action_failed", CampaignVO, [TypeShowAll, AgentActions], [ProfileShowAll, HeroComplete, HeroRecommended]),
            new("campaign_vo_agent_action_success", CampaignVO, [TypeShowAll, AgentActions], [ProfileShowAll, HeroComplete, HeroRecommended]),
            
            new("campaign_vo_new_commander", CampaignVO, [TypeShowAll, ArmyActions], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_recruit_units", CampaignVO, [TypeShowAll, ArmyActions], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cam_disband", CampaignVO, [TypeShowAll, ArmyActions], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cam_disbanded_neg", CampaignVO, [TypeShowAll, ArmyActions], [ProfileShowAll, ..CompleteProfiles]),
            new("campaign_vo_cam_disbanded_pos", CampaignVO, [TypeShowAll, ArmyActions], [ProfileShowAll, ..CompleteProfiles]),

            new("campaign_vo_stance_ambush", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_astromancy", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_channeling", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_default", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_double_time", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_land_raid", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_march", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_muster", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_patrol", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_raise_dead", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_set_camp", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_set_camp_raiding", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_settle", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_stalking", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_stance_tunneling", CampaignVO, [TypeShowAll, ArmyStances], [ProfileShowAll, LordComplete, LordRecommended]),
            
            new("campaign_vo_post_battle_defeat", CampaignVO, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_post_battle_victory", CampaignVO, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),

            new("campaign_vo_cam_tech_tree", CampaignVO, [TypeShowAll, TechnologyTree], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cam_tech_tree_response", CampaignVO, [TypeShowAll, TechnologyTree], [ProfileShowAll, LordComplete]),
            
            new("campaign_vo_diplomacy_negative", CampaignVO, [TypeShowAll, DiplomacyBarks], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_diplomacy_positive", CampaignVO, [TypeShowAll, DiplomacyBarks], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_diplomacy_selected", CampaignVO, [TypeShowAll, DiplomacyBarks], [ProfileShowAll, LordComplete, LordRecommended]),
            
            new("gotrek_felix_arrival", CampaignVO, [TypeShowAll, GotrekAndFelix], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("gotrek_felix_departure", CampaignVO, [TypeShowAll, GotrekAndFelix], [ProfileShowAll, ..LordHeroCompleteProfiles]),

            new("campaign_vo_grn_dedicate_to_gork", CampaignVO, [GorkAndMorkDedicationsMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_grn_dedicate_to_mork", CampaignVO, [GorkAndMorkDedicationsMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_grn_scrap_upgrade", CampaignVO, [ScrapMechanic], [ProfileShowAll, ..CompleteProfiles]),

            new("campaign_vo_skull_throne_tier1", CampaignVO, [SkullThroneMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_skull_throne_tier2", CampaignVO, [SkullThroneMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_skull_throne_tier3", CampaignVO, [SkullThroneMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_skull_throne_tier4", CampaignVO, [SkullThroneMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_def_recruit_monster_pens", CampaignVO, [MonsterPensMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_mounted_creature", CampaignVO, [TypeShowAll, Creature], [ProfileShowAll]),
        ];

        public static List<Wh3CampaignVOConversationalDefinition> CampaignConversational { get; } =
        [
            new("campaign_vo_cs_intimidated", CampaignVOConversational, [TypeShowAll, CharacterConfidence], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_cs_neutral", CampaignVOConversational, [TypeShowAll, CharacterConfidence], [ProfileShowAll, ..LordHeroRecommendedProfiles]),
            new("campaign_vo_cs_confident", CampaignVOConversational, [TypeShowAll, CharacterConfidence], [ProfileShowAll, ..LordHeroRecommendedProfiles]),

            new("campaign_vo_cs_enemy_region_generic", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_in_forest", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_in_mountains", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_in_rain", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_in_snow", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_near_sea", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_on_sea", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_sea_storm", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_weather_hot", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_weather_cold", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_proximity", CampaignVOConversational, [TypeShowAll, CharacterEnvironment], [ProfileShowAll, ..LordHeroCompleteProfiles]),
            new("campaign_vo_cs_spam_click", CampaignVOConversational, [TypeShowAll, CharacterMisc], [ProfileShowAll, ..LordHeroCompleteProfiles]),

            new("campaign_vo_cs_city_own_generic", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_other_generic", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_buildings_damaged", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_high_corruption", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_public_order_low", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_riot", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),
            new("campaign_vo_cs_city_under_siege", CampaignVOConversational, [TypeShowAll, CityDetails], [ProfileShowAll, ..LordHeroCompleteProfiles, CultureRecommended]),

            new("campaign_vo_cs_pre_battle_fight_battle", CampaignVOConversational, [TypeShowAll, BattleAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_pre_battle_siege_continue", CampaignVOConversational, [TypeShowAll, BattleAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_pre_battle_siege_break", CampaignVOConversational, [TypeShowAll, BattleAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_pre_battle_retreat", CampaignVOConversational, [TypeShowAll, BattleAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_auto_resolve", CampaignVOConversational, [TypeShowAll, BattleAction], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_post_battle_victory", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cs_post_battle_close_victory", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cs_post_battle_great_victory", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cs_post_battle_defeat", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cs_post_battle_close_defeat", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),
            new("campaign_vo_cs_post_battle_great_defeat", CampaignVOConversational, [TypeShowAll, PostBattleResult], [ProfileShowAll, LordComplete, LordRecommended]),

            new("campaign_vo_cs_post_battle_captives_enslave", CampaignVOConversational, [TypeShowAll, PostBattleCaptivesAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_captives_execute", CampaignVOConversational, [TypeShowAll, PostBattleCaptivesAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_captives_release", CampaignVOConversational, [TypeShowAll, PostBattleCaptivesAction], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_post_battle_settlement_do_nothing", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_establish_foreign_slot" , CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_loot", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_occupy", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_occupy_factory", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_occupy_outpost", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_occupy_tower", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_raze", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_reinstate_elector_count", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_sack", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_vassal_enlist", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_gift_to_faction", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_kho_skulls_for_the_skull_throne", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_kho_blood_for_the_blood_god", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_tze_parasitic", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_post_battle_settlement_tze_symbiotic", CampaignVOConversational, [TypeShowAll, PostBattleSettlementAction], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_other_character_details_panel_low_loyalty", CampaignVOConversational, [TypeShowAll, CharacterDetails], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_other_character_details_panel_neutral", CampaignVOConversational, [TypeShowAll, CharacterDetails], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_other_character_details_panel_positive", CampaignVOConversational, [TypeShowAll, CharacterDetails], [ProfileShowAll, LordComplete]),

            new("Campaign_CS_Nur_Plague_Infect", CampaignVOConversational, [TypeShowAll, PlaguesOfNurgleMechanic], [ProfileShowAll, LordComplete]),
            new("Campaign_CS_Nur_Plague_Summon_Cultist", CampaignVOConversational, [TypeShowAll, PlaguesOfNurgleMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_forbidden_workshop_purchase_doomrocket", CampaignVOConversational, [TypeShowAll, ForbiddenWorkshopMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_forbidden_workshop_upgrade_doomflayer", CampaignVOConversational, [TypeShowAll, ForbiddenWorkshopMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_forbidden_workshop_upgrade_doomwheel", CampaignVOConversational, [TypeShowAll, ForbiddenWorkshopMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams", CampaignVOConversational, [TypeShowAll, ForbiddenWorkshopMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_hellforge_accept", CampaignVOConversational, [TypeShowAll, HellforgeMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_hellforge_customisation_category",  CampaignVOConversational, [TypeShowAll, HellforgeMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_hellforge_customisation_unit", CampaignVOConversational, [TypeShowAll, HellforgeMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_monster_pens_dilemma_ghrond", CampaignVOConversational, [TypeShowAll, MonsterPensMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_monster_pens_dilemma_lustria", CampaignVOConversational, [TypeShowAll, MonsterPensMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_monster_pens_dilemma_naggaroth", CampaignVOConversational, [TypeShowAll, MonsterPensMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_monster_pens_dilemma_old_world", CampaignVOConversational, [TypeShowAll, MonsterPensMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_monster_pens_event", CampaignVOConversational, [TypeShowAll, MonsterPensMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_tzarkan_calls_and_taunts", CampaignVOConversational, [TypeShowAll, TzArkanMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_tzarkan_whispers", CampaignVOConversational, [TypeShowAll, TzArkanMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_sacrifice_to_sotek", CampaignVOConversational, [TypeShowAll, SacrificesToSotekMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_summon_elector_counts_panel_open_vo", CampaignVOConversational, [TypeShowAll, SummonTheElectorCountsMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_wef_daiths_forge", CampaignVOConversational, [TypeShowAll, DaithsForgeMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_grn_occupy_waaagh_target", CampaignVOConversational, [TypeShowAll, WaaghMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_grn_waaagh_ready", CampaignVOConversational, [TypeShowAll, WaaghMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_grn_waaagh_in_progress", CampaignVOConversational, [TypeShowAll, WaaghMechanic], [ProfileShowAll, LordComplete]),
            new("campaign_vo_cs_grn_waaagh_complete", CampaignVOConversational, [TypeShowAll, WaaghMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_kor_wrath_of_khorne", CampaignVOConversational, [TypeShowAll, WrathOfKhorneMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_ogr_mercenary_contracts", CampaignVOConversational, [TypeShowAll, MercenaryContractsMechanic], [ProfileShowAll, LordComplete]),

            new("campaign_vo_cs_ogr_meat_status", CampaignVOConversational, [TypeShowAll, MeatMechanic], [ProfileShowAll, LordComplete]),
        ];

        public static List<Wh3BattleVOOrdersDefinition> Battle { get; } =
        [
            new("battle_vo_order_move", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..MinimumProfiles]),
            // Does alternative work like short?
            new("battle_vo_order_move_alternative", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_move_ram", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_move_siege_tower", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_man_siege_tower", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_pick_up_engine", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_climb", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_halt", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..MinimumProfiles]),
            new("battle_vo_order_withdraw", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..MinimumProfiles]),
            new("battle_vo_order_withdraw_tactical", BattleVO, [TypeShowAll, UnitMovement], [ProfileShowAll, ..MinimumProfiles]),

            new("battle_vo_order_attack", BattleVO, [TypeShowAll, UnitAttack], [ProfileShowAll, ..MinimumProfiles]),
            new("battle_vo_order_attack_alternative", BattleVO, [TypeShowAll, UnitAttack], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_flying_charge", BattleVO, [TypeShowAll, UnitAttack], [ProfileShowAll]),

            new("battle_vo_order_select", BattleVO, [TypeShowAll, UnitSelection], [ProfileShowAll, ..MinimumProfiles]),

            new("battle_vo_order_generic_response", BattleVO, [TypeShowAll, UnitGeneric, UnitMovement, UnitAttack, UnitSelection], [ProfileShowAll, ..MinimumProfiles]),
            // Does this work like the campaign short ones, where it triggers as a backup, in this case to generic?
            new("battle_vo_order_short_order", BattleVO, [TypeShowAll, UnitGeneric, UnitMovement, UnitAttack, UnitSelection], [ProfileShowAll, ..RecommendedProfiles]),

            new("battle_vo_order_special_ability", BattleVO, [TypeShowAll, UnitMisc], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_change_ammo", BattleVO, [TypeShowAll, UnitMisc], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_bat_speeches", BattleVO, [TypeShowAll, UnitMisc], [ProfileShowAll, LordComplete, LordRecommended]),

            new("battle_vo_order_melee_off", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_melee_on", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_group_created", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_group_disbanded", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_order_guard_off", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_guard_on", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_change_formation", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_formation_lock", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_formation_unlock", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_fire_at_will_off", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_fire_at_will_on", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_skirmish_off", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_order_skirmish_on", BattleVO, [TypeShowAll, BattleOrders], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_order_bat_mode_capture_neg", BattleVO, [TypeShowAll, BattleMode], [ProfileShowAll, LordComplete, LordRecommended]),
            new("battle_vo_order_bat_mode_capture_pos", BattleVO, [TypeShowAll, BattleMode], [ProfileShowAll, LordComplete, LordRecommended]),
            new("battle_vo_order_bat_mode_survival", BattleVO, [TypeShowAll, BattleMode], [ProfileShowAll, LordComplete, LordRecommended]),

            new("battle_vo_order_battle_continue_battle", BattleVO, [TypeShowAll, BattleControls], [ProfileShowAll, LordComplete]),
            new("battle_vo_order_battle_quit_battle", BattleVO, [TypeShowAll, BattleControls], [ProfileShowAll, LordComplete]),
        ];

        public static List<Wh3BattleVOConversationalDefinition> BattleConversational { get; } =
        [
            new("battle_vo_conversation_environment_ground_type_forest", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_ground_type_mud", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_in_cave", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_in_water", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_weather_cold", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_weather_desert", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_weather_rain", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_environment_weather_snow", BattleVOConversational, [TypeShowAll, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_clash", BattleVOConversational, [TypeShowAll, UnitEvents], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_conversation_dissapointment", BattleVOConversational, [TypeShowAll, UnitEvents], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_conversation_encouragement", BattleVOConversational, [TypeShowAll, UnitEvents], [ProfileShowAll, ..RecommendedProfiles]),
            new("battle_vo_conversation_proximity", BattleVOConversational, [TypeShowAll, UnitEvents, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_storm_of_magic", BattleVOConversational, [TypeShowAll, UnitEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_own_unit_artillery_fire", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_artillery_firing", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_artillery_reload", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_fearful", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_moving", BattleVOConversational, [TypeShowAll, OwnUnitEvents, UnitMovement], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_routing", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_wavering", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_under_dragon_firebreath_attack", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_unit_under_ranged_attack", BattleVOConversational, [TypeShowAll, OwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_skv_own_unit_spawn_units", BattleVOConversational, [TypeShowAll, OwnUnitEvents, SkavenOwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_skv_own_unit_tactical_withdraw", BattleVOConversational, [TypeShowAll, OwnUnitEvents, SkavenOwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_skv_own_unit_warpfire_artillery", BattleVOConversational, [TypeShowAll, OwnUnitEvents, SkavenOwnUnitEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_allied_unit_routing", BattleVOConversational, [TypeShowAll, AlliedUnitEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_enemy_skaven_unit_revealed", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_charging", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_chariot_charge", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_dragon", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_flanking", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_flying", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_large_creature", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_revealed", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_spell_cast", BattleVOConversational, [TypeShowAll, EnemyUnitEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_unit_at_rear", BattleVOConversational, [TypeShowAll, EnemyUnitEvents, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_own_army_black_arks_triggered", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_caused_damage", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_missile_amount_inferior", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_missile_amount_superior", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_peasants_fleeing", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_spell_cast", BattleVOConversational, [TypeShowAll, OwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_own_army_at_chokepoint", BattleVOConversational, [TypeShowAll, OwnArmyEvents, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_hef_own_army_air_units", BattleVOConversational, [TypeShowAll, OwnArmyEvents, HighElvesOwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_hef_own_army_low_stength", BattleVOConversational, [TypeShowAll, OwnArmyEvents, HighElvesOwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_def_own_army_murderous_prowess_100_percent", BattleVOConversational, [TypeShowAll, OwnArmyEvents, DarkElvesOwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_def_own_army_murderous_prowess_75_percent", BattleVOConversational, [TypeShowAll, OwnArmyEvents, DarkElvesOwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_lzd_own_army_dino_rampage", BattleVOConversational, [TypeShowAll, OwnArmyEvents, LizardmenOwnArmyEvents], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_enemy_army_black_arks_triggered", BattleVOConversational, [TypeShowAll, EnemyArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_army_has_many_cannons", BattleVOConversational, [TypeShowAll, EnemyArmyEvents], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_enemy_army_at_chokepoint", BattleVOConversational, [TypeShowAll, EnemyArmyEvents, UnitEnvironment], [ProfileShowAll, ..CompleteProfiles]),

            new("battle_vo_conversation_siege_attack", BattleVOConversational, [TypeShowAll, BattleType], [ProfileShowAll, ..CompleteProfiles]),
            new("battle_vo_conversation_siege_defence", BattleVOConversational, [TypeShowAll, BattleType], [ProfileShowAll, ..CompleteProfiles]),
        ];

        // TODO: Figure out what to do with this
        public static List<Wh3DialogueEventDefinition> Foley { get; } =
        [
            // Battle Individual Melee
            //new("Battle_Individual_Melee_Weapon_Hit", BattleIndividualMelee, [ShowAll])
        ];

        public static List<Wh3DialogueEventDefinition> Information { get; } =
        [
            ..Frontend,
            ..Campaign,
            ..CampaignConversational,
            ..Battle,
            ..BattleConversational,
        ];

        public static string GetDialogueEventTypeDisplayName(this Wh3DialogueEventType? dialogueEventType)
        {
            if (dialogueEventType == null)
                return null;

            var field = typeof(Wh3DialogueEventType).GetField(dialogueEventType.Value.ToString());
            var display = field?.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
                return display.GetName();
            return dialogueEventType.Value.ToString();
        }

        public static string GetDialogueEventProfileDisplayName(this Wh3DialogueEventUnitProfile? dialogueEventUnitProfile)
        {
            if (dialogueEventUnitProfile == null)
                return null;

            var field = typeof(Wh3DialogueEventUnitProfile).GetField(dialogueEventUnitProfile.Value.ToString());
            var display = field?.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
                return display.GetName();
            return dialogueEventUnitProfile.Value.ToString();
        }

        public static Wh3SoundBank GetSoundBank(string dialogueEventName) => Information.First(definition => definition.Name == dialogueEventName).SoundBank;

        public static uint GetActorMixerId(string dialogueEventName) => Information.First(definition => definition.Name == dialogueEventName).ActorMixerId;

        public static bool Contains(Wh3SoundBank soundBank) => Information.Any(definition => definition.SoundBank == soundBank);
    }
}
