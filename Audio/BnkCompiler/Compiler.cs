using System.IO;
using System.Linq;
using Audio.BnkCompiler.ObjectGeneration;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using CommunityToolkit.Diagnostics;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Audio.Utility;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Audio.FileFormats.WWise.Hirc.Shared;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Audio.Storage;

namespace Audio.BnkCompiler
{
    public class CompileResult
    {
        public CompilerData Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputDatFile { get; set; }
        public PackFile OutputStatesDatFile { get; set; }
    }

    internal static class CompilerConstants
    {
        public static readonly string Game_Warhammer3 = "Warhammer3";

        public static string MatchDialogueEventToBnk(string dialogueEvent)
        {
            dialogueEvent = dialogueEvent.ToLower();

            if (dialogueEvent.Contains("battle_vo_conversation"))
                return "battle_vo_conversational";
            else if (dialogueEvent.Contains("battle_vo_order"))
                return "battle_vo_orders";
            else if (dialogueEvent.Contains("campaign_vo_cs"))
                return "campaign_vo_conversational";
            else if (dialogueEvent.Contains("campaign_vo") || dialogueEvent == "gotrek_felix_arrival" || dialogueEvent == "gotrek_felix_departure")
                return "campaign_vo";
            else if (dialogueEvent.Contains("frontend_vo"))
                return "frontend_vo";
            else if (dialogueEvent == "battle_individual_melee_weapon_hit")
                return "battle_individual_melee";
            else
                throw new Exception("Dialogue_Event could not be matched to a bnk.");
        }

        public static Dictionary<string, List<string>> dialogueEventStates = new Dictionary<string, List<string>>()
        {
            {"Battle_Individual_Melee_Weapon_Hit", new List<string>{"Generic_Melee_Hit_Type", "Generic_Melee_Weapon_Defend_Type"}},
            {"battle_vo_conversation_own_unit_under_ranged_attack", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_unit_chariot_charge", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_hef_own_army_low_stength", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_army_missile_amount_inferior", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_unit_flanking", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_dissapointment", new List<string>{"VO_Culture", "VO_Actor", "VO_Culture"}},
            {"battle_vo_conversation_enemy_unit_flying", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_skv_own_unit_warpfire_artillery", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_unit_charging", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_environment_weather_snow", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_army_has_many_cannons", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_environment_in_water", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_fearful", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_hef_own_army_air_units", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_under_dragon_firebreath_attack", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_own_unit_artillery_reload", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_enemy_unit_revealed", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_own_army_caused_damage", new List<string>{"VO_Culture", "VO_Battle_Selection", "VO_Actor"}},
            {"battle_vo_conversation_own_army_peasants_fleeing", new List<string>{"VO_Actor"}},
            {"battle_vo_conversation_enemy_army_black_arks_triggered", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_skv_own_unit_tactical_withdraw", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_clash", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Conversation_Clash_Type", "VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_environment_in_cave", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_artillery_firing", new List<string>{"VO_Culture", "VO_Battle_Selection"}},
            {"battle_vo_conversation_siege_defence", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Conversation_Clash_Type"}},
            {"battle_vo_conversation_environment_weather_cold", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_storm_of_magic", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_lzd_own_army_dino_rampage", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_encouragement", new List<string>{"VO_Culture", "VO_Actor", "VO_Culture"}},
            {"battle_vo_conversation_own_unit_routing", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_environment_ground_type_forest", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_def_own_army_murderous_prowess_75_percent", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_allied_unit_routing", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_enemy_skaven_unit_revealed", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_skv_own_unit_spawn_units", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_army_spell_cast", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_siege_attack", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Conversation_Clash_Type"}},
            {"battle_vo_conversation_def_own_army_murderous_prowess_100_percent", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_moving", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_environment_weather_desert", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_unit_at_rear", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_enemy_unit_large_creature", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_own_army_black_arks_triggered", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_environment_weather_rain", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_army_at_chokepoint", new List<string>{"VO_Culture", "VO_Battle_Selection", "VO_Actor"}},
            {"battle_vo_conversation_enemy_unit_spell_cast", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_own_army_missile_amount_superior", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_artillery_fire", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_environment_ground_type_mud", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_conversation_enemy_army_at_chokepoint", new List<string>{"VO_Culture", "VO_Battle_Selection", "VO_Actor"}},
            {"battle_vo_conversation_own_unit_wavering", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_enemy_unit_dragon", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_conversation_proximity", new List<string>{"VO_Actor", "VO_Actor"}},
            {"battle_vo_order_bat_mode_survival", new List<string>{"VO_Actor"}},
            {"battle_vo_order_guard_on", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_bat_mode_capture_pos", new List<string>{"VO_Actor", "Battle_Type"}},
            {"battle_vo_order_halt", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_special_ability", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Special_Ability", "VO_Battle_Selection", "VO_Battle_Order_Urgency"}},
            {"battle_vo_order_battle_continue_battle", new List<string>{"VO_Actor"}},
            {"battle_vo_order_climb", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_withdraw_tactical", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_pick_up_engine", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_move_alternative", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_withdraw", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Order_Urgency"}},
            {"battle_vo_order_move_siege_tower", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_change_ammo", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Vehicle_Ammo_Type"}},
            {"battle_vo_order_attack", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Unit_Type", "VO_Battle_Order_Urgency", "VO_Faction_Leader", "VO_Battle_Order_Artillery_Range", "VO_Battle_Order_Target_Attacking_Character"}},
            {"battle_vo_order_skirmish_off", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_generic_response", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Unit_Type", "VO_Battle_Order_Urgency"}},
            {"battle_vo_order_fire_at_will_on", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Order_Urgency", "VO_Battle_Selection"}},
            {"battle_vo_order_short_order", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_change_formation", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_formation_lock", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_move", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Order_Speed", "VO_Battle_Order_Urgency", "VO_Battle_Order_Air_Unit", "VO_Faction_Leader"}},
            {"battle_vo_order_fire_at_will_off", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Order_Urgency", "VO_Battle_Selection"}},
            {"battle_vo_order_man_siege_tower", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_move_ram", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_group_created", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_group_disbanded", new List<string>{"VO_Culture", "VO_Actor"}},
            {"battle_vo_order_skirmish_on", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_melee_off", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_flying_charge", new List<string>{"VO_Culture"}},
            {"battle_vo_order_attack_alternative", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Unit_Type", "VO_Battle_Order_Urgency"}},
            {"battle_vo_order_melee_on", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_battle_quit_battle", new List<string>{"VO_Actor"}},
            {"battle_vo_order_bat_speeches", new List<string>{"VO_Actor"}},
            {"battle_vo_order_formation_unlock", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_guard_off", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection"}},
            {"battle_vo_order_bat_mode_capture_neg", new List<string>{"VO_Actor"}},
            {"battle_vo_order_select", new List<string>{"VO_Culture", "VO_Actor", "VO_Battle_Selection", "VO_Battle_Order_Urgency", "VO_Faction_Leader"}},
            {"campaign_vo_cs_enemy_region_generic", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_hellforge_customisation_category", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_intimidated", new List<string>{"VO_Culture", "VO_Actor", "VO_Culture"}},
            {"campaign_vo_cs_hellforge_customisation_unit", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_tzarkan_calls_and_taunts", new List<string>{"VO_Actor", "Generic_Actor_TzArkan_Sanity"}},
            {"campaign_vo_cs_near_sea", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_victory", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_vassal_enlist", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_forbidden_workshop_purchase_doomrocket", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_in_forest", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_summon_elector_counts_panel_open_vo", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_monster_pens_dilemma_lustria", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_weather_hot", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_pre_battle_siege_break", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_city_public_order_low", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_great_victory", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_captives_release", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_wef_daiths_forge", new List<string>{"VO_Actor", "VO_Campaign_Daiths_Forge"}},
            {"campaign_vo_cs_other_character_details_panel_low_loyalty", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_spam_click", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_in_mountains", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_raze", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_pre_battle_siege_continue", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_other_character_details_panel_neutral", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_city_under_siege", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_proximity", new List<string>{"VO_Actor", "VO_Actor"}},
            {"campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_city_riot", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_loot", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_occupy", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_do_nothing", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_weather_cold", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_great_defeat", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_city_other_generic", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_close_defeat", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_captives_enslave", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_sea_storm", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_in_snow", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_city_buildings_damaged", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_defeat", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_neutral", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_forbidden_workshop_upgrade_doomwheel", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_tzarkan_whispers", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_forbidden_workshop_upgrade_doomflayer", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_occupy_factory", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_monster_pens_event", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_monster_pens_dilemma_ghrond", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_pre_battle_fight_battle", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_occupy_outpost", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_close_victory", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_confident", new List<string>{"VO_Culture", "VO_Actor", "VO_Culture"}},
            {"campaign_vo_cs_post_battle_captives_execute", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_pre_battle_retreat", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_sacrifice_to_sotek", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_on_sea", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_sack", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_city_high_corruption", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_other_character_details_panel_positive", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_monster_pens_dilemma_naggaroth", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_monster_pens_dilemma_old_world", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_reinstate_elector_count", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_occupy_tower", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_hellforge_accept", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_in_rain", new List<string>{"VO_Actor"}},
            {"campaign_vo_cs_city_own_generic", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_cs_post_battle_settlement_establish_foreign_slot", new List<string>{"VO_Culture", "VO_Actor"}},
            {"frontend_vo_character_select", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_default", new List<string>{"VO_Actor"}},
            {"campaign_vo_no", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_stalking", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected_allied", new List<string>{"VO_Actor"}},
            {"campaign_vo_post_battle_defeat", new List<string>{"VO_Culture", "VO_Actor"}},
            {"campaign_vo_level_up", new List<string>{"VO_Actor"}},
            {"campaign_vo_yes", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_settle", new List<string>{"VO_Actor"}},
            {"campaign_vo_retreat", new List<string>{"VO_Actor"}},
            {"campaign_vo_recruit_units", new List<string>{"VO_Actor"}},
            {"campaign_vo_yes_short", new List<string>{"VO_Actor"}},
            {"campaign_vo_agent_action_success", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_astromancy", new List<string>{"VO_Actor"}},
            {"campaign_vo_ship_dock", new List<string>{"VO_Actor", "VO_Campaign_Order_Move_Type"}},
            {"campaign_vo_post_battle_victory", new List<string>{"VO_Actor"}},
            {"campaign_vo_mounted_creature", new List<string>{"VO_Culture"}},
            {"campaign_vo_stance_patrol", new List<string>{"VO_Actor"}},
            {"gotrek_felix_departure", new List<string>{"VO_Actor"}},
            {"campaign_vo_move_garrisoning", new List<string>{"VO_Actor"}},
            {"campaign_vo_move_next_turn", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_tunneling", new List<string>{"VO_Actor"}},
            {"campaign_vo_new_commander", new List<string>{"VO_Actor"}},
            {"campaign_vo_diplomacy_negative", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_channeling", new List<string>{"VO_Actor"}},
            {"campaign_vo_created", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected_fail", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_land_raid", new List<string>{"VO_Actor"}},
            {"campaign_vo_agent_action_failed", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected_first_time", new List<string>{"VO_Actor"}},
            {"campaign_vo_move", new List<string>{"VO_Actor", "VO_Campaign_Order_Move_Type"}},
            {"campaign_vo_attack", new List<string>{"VO_Actor", "VO_Campaign_Order_Move_Type"}},
            {"campaign_vo_cam_tech_tree_response", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_disband", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_muster", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_double_time", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_disbanded_pos", new List<string>{"VO_Actor"}},
            {"campaign_vo_special_ability", new List<string>{"VO_Actor", "VO_Campaign_Special_Ability"}},
            {"campaign_vo_stance_ambush", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_raise_dead", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected_neutral", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_march", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_disbanded_neg", new List<string>{"VO_Actor"}},
            {"gotrek_felix_arrival", new List<string>{"VO_Actor"}},
            {"campaign_vo_no_short", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_skill_weapon_tree", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_set_camp_raiding", new List<string>{"VO_Actor"}},
            {"campaign_vo_diplomacy_positive", new List<string>{"VO_Actor"}},
            {"campaign_vo_diplomacy_selected", new List<string>{"VO_Actor"}},
            {"campaign_vo_yes_short_aggressive", new List<string>{"VO_Actor"}},
            {"campaign_vo_stance_set_camp", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_skill_weapon_tree_response", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected_short", new List<string>{"VO_Actor"}},
            {"campaign_vo_cam_tech_tree", new List<string>{"VO_Actor"}},
            {"campaign_vo_selected", new List<string>{"VO_Actor"}}
        };
    }

    public class Compiler
    {
        private readonly HircBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;
        private readonly RepositoryProvider _provider;

        public Compiler(HircBuilder hircBuilder, BnkHeaderBuilder headerBuilder, RepositoryProvider provider)
        {
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
            _provider = provider;
        }

        public Result<CompileResult> CompileProject(CompilerData audioProject)
        {
            // Load audio repository to access dat dump.
            var audioRepository = new AudioRepository(_provider, false);

            // Build the wwise object graph 
            var header = _headerBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            //Ensure all write ids are not causing conflicts. Todo, this will cause issues with reuse of sounds
            var allIds = hircChunk.Hircs.Select(x => x.Id).ToList();
            var originalCount = allIds.Count();
            var uniqueCount = allIds.Distinct().Count();
            Guard.IsEqualTo(originalCount, uniqueCount);

            var eventDat = (audioProject.Events.Count == 0) ? null : BuildDat(audioProject);
            var statesDat = (audioProject.DialogueEvents.Count == 0) ? null : BuildStatesDat(audioProject);

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = eventDat,
                OutputStatesDatFile = statesDat,
            };

            return Result<CompileResult>.FromOk(compileResult);
        }

        PackFile ConvertToPackFile(BkhdHeader header, HircChunk hircChunk, string outputFile)
        {
            var outputName = $"{outputFile}.bnk";
            var headerBytes = BkhdParser.GetAsByteArray(header);
            var hircBytes = new HircParser().GetAsBytes(hircChunk);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(headerBytes);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile(outputName, new MemorySource(bytes));
            var parser = new BnkParser();
            var result = parser.Parse(bnkPackFile, "test\\fakefilename.bnk");

            return bnkPackFile;
        }

        PackFile BuildDat(CompilerData projectFile)
        {
            var outputName = $"event_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Name, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }

        PackFile BuildStatesDat(CompilerData projectFile)
        {
            var outputName = $"states_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var state in projectFile.DatStates)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = state, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }
    }
}
