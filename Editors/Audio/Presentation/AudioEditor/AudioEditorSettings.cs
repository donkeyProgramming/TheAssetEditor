using System.Collections.Generic;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorSettings
    {
        public List<string> EventType { get; set; } =
        [
            "Frontend VO",
            "Campaign VO",
            "Campaign Conversational VO",
            "Battle VO",
            "Battle Conversational VO",
            "Non-VO"
        ];

        public List<string> FrontendVO { get; set; } =
        [
            "Lord"
        ];

        public List<string> CampaignVO { get; set; } =
        [
            "Lord",
            "Hero"
        ];

        public List<string> BattleVO { get; set; } =
        [
            "Lord - Melee",
            "Lord - Skirmisher",
            "Lord - Caster",
            "Hero - Melee",
            "Hero - Skirmisher",
            "Hero - Caster",
            "Unit - Infantry",
            "Unit - Skirmisher",
            "Unit - Cavalry",
            "Unit - SEM",
            "Unit - Artillery"
        ];

        public List<string> NonVO { get; set; } =
        [
            "Ability",
            "CampaignAdvisor",
            "DiplomacyLine",
            "EventNarration",
            "Magic",
            "Movie",
            "QuestBattle",
            "Ritual",
            "UI",
            "Vocalisation"
        ];

        public static List<string> BattleIndividualMeleeAll { get; set; } =
        [
            "Battle_Individual_Melee_Weapon_Hit"
        ];

        public static List<string> FrontendVODialogueEventsAll { get; set; } =
        [
            "frontend_vo_character_select"
        ];

        public static List<string> CampaignVODialogueEventsAll { get; set; } =
        [
            "campaign_vo_agent_action_failed",
            "campaign_vo_agent_action_success",
            "campaign_vo_attack",
            "campaign_vo_cam_disband",
            "campaign_vo_cam_disbanded_neg",
            "campaign_vo_cam_disbanded_pos",
            "campaign_vo_cam_skill_weapon_tree",
            "campaign_vo_cam_skill_weapon_tree_response",
            "campaign_vo_cam_tech_tree",
            "campaign_vo_cam_tech_tree_response",
            "campaign_vo_created",
            "campaign_vo_diplomacy_negative",
            "campaign_vo_diplomacy_positive",
            "campaign_vo_diplomacy_selected",
            "campaign_vo_level_up",
            "campaign_vo_mounted_creature",
            "campaign_vo_move",
            "campaign_vo_move_garrisoning",
            "campaign_vo_move_next_turn",
            "campaign_vo_new_commander",
            "campaign_vo_no",
            "campaign_vo_no_short",
            "campaign_vo_post_battle_defeat",
            "campaign_vo_post_battle_victory",
            "campaign_vo_recruit_units",
            "campaign_vo_retreat",
            "campaign_vo_selected",
            "campaign_vo_selected_allied",
            "campaign_vo_selected_fail",
            "campaign_vo_selected_first_time",
            "campaign_vo_selected_neutral",
            "campaign_vo_selected_short",
            "campaign_vo_ship_dock",
            "campaign_vo_special_ability",
            "campaign_vo_stance_ambush",
            "campaign_vo_stance_astromancy",
            "campaign_vo_stance_channeling",
            "campaign_vo_stance_default",
            "campaign_vo_stance_double_time",
            "campaign_vo_stance_land_raid",
            "campaign_vo_stance_march",
            "campaign_vo_stance_muster",
            "campaign_vo_stance_patrol",
            "campaign_vo_stance_raise_dead",
            "campaign_vo_stance_set_camp",
            "campaign_vo_stance_set_camp_raiding",
            "campaign_vo_stance_settle",
            "campaign_vo_stance_stalking",
            "campaign_vo_stance_tunneling",
            "campaign_vo_yes",
            "campaign_vo_yes_short",
            "campaign_vo_yes_short_aggressive",
            "gotrek_felix_arrival",
            "gotrek_felix_departure"
        ];

        public static List<string> CampaignVOConversationalAll { get; set; } =
        [
            "Campaign_CS_Nur_Plague_Infect",
            "Campaign_CS_Nur_Plague_Summon_Cultist",
            "campaign_vo_cs_city_buildings_damaged",
            "campaign_vo_cs_city_high_corruption",
            "campaign_vo_cs_city_other_generic",
            "campaign_vo_cs_city_own_generic",
            "campaign_vo_cs_city_public_order_low",
            "campaign_vo_cs_city_riot",
            "campaign_vo_cs_city_under_siege",
            "campaign_vo_cs_confident",
            "campaign_vo_cs_enemy_region_generic",
            "campaign_vo_cs_forbidden_workshop_purchase_doomrocket",
            "campaign_vo_cs_forbidden_workshop_upgrade_doomflayer",
            "campaign_vo_cs_forbidden_workshop_upgrade_doomwheel",
            "campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams",
            "campaign_vo_cs_hellforge_accept",
            "campaign_vo_cs_hellforge_customisation_category",
            "campaign_vo_cs_hellforge_customisation_unit",
            "campaign_vo_cs_in_forest",
            "campaign_vo_cs_in_mountains",
            "campaign_vo_cs_in_rain",
            "campaign_vo_cs_in_snow",
            "campaign_vo_cs_intimidated",
            "campaign_vo_cs_monster_pens_dilemma_ghrond",
            "campaign_vo_cs_monster_pens_dilemma_lustria",
            "campaign_vo_cs_monster_pens_dilemma_naggaroth",
            "campaign_vo_cs_monster_pens_dilemma_old_world",
            "campaign_vo_cs_monster_pens_event",
            "campaign_vo_cs_near_sea",
            "campaign_vo_cs_neutral",
            "campaign_vo_cs_on_sea",
            "campaign_vo_cs_other_character_details_panel_low_loyalty",
            "campaign_vo_cs_other_character_details_panel_neutral",
            "campaign_vo_cs_other_character_details_panel_positive",
            "campaign_vo_cs_post_battle_captives_enslave",
            "campaign_vo_cs_post_battle_captives_execute",
            "campaign_vo_cs_post_battle_captives_release",
            "campaign_vo_cs_post_battle_close_defeat",
            "campaign_vo_cs_post_battle_close_victory",
            "campaign_vo_cs_post_battle_defeat",
            "campaign_vo_cs_post_battle_great_defeat",
            "campaign_vo_cs_post_battle_great_victory",
            "campaign_vo_cs_post_battle_settlement_do_nothing",
            "campaign_vo_cs_post_battle_settlement_establish_foreign_slot",
            "campaign_vo_cs_post_battle_settlement_loot",
            "campaign_vo_cs_post_battle_settlement_occupy",
            "campaign_vo_cs_post_battle_settlement_occupy_factory",
            "campaign_vo_cs_post_battle_settlement_occupy_outpost",
            "campaign_vo_cs_post_battle_settlement_occupy_tower",
            "campaign_vo_cs_post_battle_settlement_raze",
            "campaign_vo_cs_post_battle_settlement_reinstate_elector_count",
            "campaign_vo_cs_post_battle_settlement_sack",
            "campaign_vo_cs_post_battle_settlement_vassal_enlist",
            "campaign_vo_cs_post_battle_victory",
            "campaign_vo_cs_pre_battle_fight_battle",
            "campaign_vo_cs_pre_battle_retreat",
            "campaign_vo_cs_pre_battle_siege_break",
            "campaign_vo_cs_pre_battle_siege_continue",
            "campaign_vo_cs_proximity",
            "campaign_vo_cs_sacrifice_to_sotek",
            "campaign_vo_cs_sea_storm",
            "campaign_vo_cs_spam_click",
            "campaign_vo_cs_summon_elector_counts_panel_open_vo",
            "campaign_vo_cs_tzarkan_calls_and_taunts",
            "campaign_vo_cs_tzarkan_whispers",
            "campaign_vo_cs_weather_cold",
            "campaign_vo_cs_weather_hot",
            "campaign_vo_cs_wef_daiths_forge"
        ];

        public static List<string> BattleVoDialogueEventsAll { get; set; } =
        [
            "battle_vo_order_attack",
            "battle_vo_order_attack_alternative",
            "battle_vo_order_bat_mode_capture_neg",
            "battle_vo_order_bat_mode_capture_pos",
            "battle_vo_order_bat_mode_survival",
            "battle_vo_order_bat_speeches",
            "battle_vo_order_battle_continue_battle",
            "battle_vo_order_battle_quit_battle",
            "battle_vo_order_change_ammo",
            "battle_vo_order_change_formation",
            "battle_vo_order_climb",
            "battle_vo_order_fire_at_will_off",
            "battle_vo_order_fire_at_will_on",
            "battle_vo_order_flying_charge",
            "battle_vo_order_formation_lock",
            "battle_vo_order_formation_unlock",
            "battle_vo_order_generic_response",
            "battle_vo_order_group_created",
            "battle_vo_order_group_disbanded",
            "battle_vo_order_guard_off",
            "battle_vo_order_guard_on",
            "battle_vo_order_halt",
            "battle_vo_order_man_siege_tower",
            "battle_vo_order_melee_off",
            "battle_vo_order_melee_on",
            "battle_vo_order_move",
            "battle_vo_order_move_alternative",
            "battle_vo_order_move_ram",
            "battle_vo_order_move_siege_tower",
            "battle_vo_order_pick_up_engine",
            "battle_vo_order_select",
            "battle_vo_order_short_order",
            "battle_vo_order_skirmish_off",
            "battle_vo_order_skirmish_on",
            "battle_vo_order_special_ability",
            "battle_vo_order_withdraw",
            "battle_vo_order_withdraw_tactical"
        ];

        public static List<string> BattleVOConversationalAll { get; set; } =
        [
            "battle_vo_conversation_allied_unit_routing",
            "battle_vo_conversation_clash",
            "battle_vo_conversation_def_own_army_murderous_prowess_100_percent",
            "battle_vo_conversation_def_own_army_murderous_prowess_75_percent",
            "battle_vo_conversation_dissapointment",
            "battle_vo_conversation_encouragement",
            "battle_vo_conversation_enemy_army_at_chokepoint",
            "battle_vo_conversation_enemy_army_black_arks_triggered",
            "battle_vo_conversation_enemy_army_has_many_cannons",
            "battle_vo_conversation_enemy_skaven_unit_revealed",
            "battle_vo_conversation_enemy_unit_at_rear",
            "battle_vo_conversation_enemy_unit_charging",
            "battle_vo_conversation_enemy_unit_chariot_charge",
            "battle_vo_conversation_enemy_unit_dragon",
            "battle_vo_conversation_enemy_unit_flanking",
            "battle_vo_conversation_enemy_unit_flying",
            "battle_vo_conversation_enemy_unit_large_creature",
            "battle_vo_conversation_enemy_unit_revealed",
            "battle_vo_conversation_enemy_unit_spell_cast",
            "battle_vo_conversation_environment_ground_type_forest",
            "battle_vo_conversation_environment_ground_type_mud",
            "battle_vo_conversation_environment_in_cave",
            "battle_vo_conversation_environment_in_water",
            "battle_vo_conversation_environment_weather_cold",
            "battle_vo_conversation_environment_weather_desert",
            "battle_vo_conversation_environment_weather_rain",
            "battle_vo_conversation_environment_weather_snow",
            "battle_vo_conversation_hef_own_army_air_units",
            "battle_vo_conversation_hef_own_army_low_stength",
            "battle_vo_conversation_lzd_own_army_dino_rampage",
            "battle_vo_conversation_own_army_at_chokepoint",
            "battle_vo_conversation_own_army_black_arks_triggered",
            "battle_vo_conversation_own_army_caused_damage",
            "battle_vo_conversation_own_army_missile_amount_inferior",
            "battle_vo_conversation_own_army_missile_amount_superior",
            "battle_vo_conversation_own_army_peasants_fleeing",
            "battle_vo_conversation_own_army_spell_cast",
            "battle_vo_conversation_own_unit_artillery_fire",
            "battle_vo_conversation_own_unit_artillery_firing",
            "battle_vo_conversation_own_unit_artillery_reload",
            "battle_vo_conversation_own_unit_fearful",
            "battle_vo_conversation_own_unit_moving",
            "battle_vo_conversation_own_unit_routing",
            "battle_vo_conversation_own_unit_under_dragon_firebreath_attack",
            "battle_vo_conversation_own_unit_under_ranged_attack",
            "battle_vo_conversation_own_unit_wavering",
            "battle_vo_conversation_proximity",
            "battle_vo_conversation_siege_attack",
            "battle_vo_conversation_siege_defence",
            "battle_vo_conversation_skv_own_unit_spawn_units",
            "battle_vo_conversation_skv_own_unit_tactical_withdraw",
            "battle_vo_conversation_skv_own_unit_warpfire_artillery",
            "battle_vo_conversation_storm_of_magic",
        ];
    }
}
