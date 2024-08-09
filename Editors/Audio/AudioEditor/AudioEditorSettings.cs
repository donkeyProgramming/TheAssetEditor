using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorSettings
    {
        public enum EventType
        {
            FrontendVO,
            CampaignVO,
            CampaignConversationalVO,
            BattleVO,
            BattleConversationalVO,
            BattleIndividualMelee,
            NonVO
        }

        public enum EventSubtype
        {
            Dummy,
            Lord,
            Hero,
            Creature,
            LordMelee,
            LordSkirmisher,
            LordCaster,
            HeroMelee,
            HeroSkirmisher,
            HeroCaster,
            UnitInfantry,
            UnitSkirmisher,
            UnitCavalry,
            UnitSEM,
            UnitArtillery,
            Ability,
            CampaignAdvisor,
            DiplomacyLine,
            EventNarration,
            Magic,
            Movie,
            QuestBattle,
            Ritual,
            UI,
            Vocalisation
        }

        public static readonly Dictionary<EventType, string> EventTypeMappings = new()
        {
            { EventType.FrontendVO, "Frontend VO" },
            { EventType.CampaignVO, "Campaign VO" },
            { EventType.CampaignConversationalVO, "Campaign Conversational VO" },
            { EventType.BattleVO, "Battle VO" },
            { EventType.BattleConversationalVO, "Battle Conversational VO" },
            { EventType.BattleIndividualMelee, "Battle Individual Melee" },
            { EventType.NonVO, "Non-VO" }
        };

        public static readonly Dictionary<EventSubtype, string> EventSubtypeMappings = new()
        {
            { EventSubtype.Dummy, "Dummy" },
            { EventSubtype.Lord, "Lord" },
            { EventSubtype.Hero, "Hero" },
            { EventSubtype.Creature, "Creature" },
            { EventSubtype.LordMelee, "Lord - Melee" },
            { EventSubtype.LordSkirmisher, "Lord - Skirmisher" },
            { EventSubtype.LordCaster, "Lord - Caster" },
            { EventSubtype.HeroMelee, "Hero - Melee" },
            { EventSubtype.HeroSkirmisher, "Hero - Skirmisher" },
            { EventSubtype.HeroCaster, "Hero - Caster" },
            { EventSubtype.UnitInfantry, "Unit - Infantry" },
            { EventSubtype.UnitSkirmisher, "Unit - Skirmisher" },
            { EventSubtype.UnitCavalry, "Unit - Cavalry" },
            { EventSubtype.UnitSEM, "Unit - SEM" },
            { EventSubtype.UnitArtillery, "Unit - Artillery" },
            { EventSubtype.Ability, "Ability" },
            { EventSubtype.CampaignAdvisor, "Campaign Advisor" },
            { EventSubtype.DiplomacyLine, "Diplomacy Line" },
            { EventSubtype.EventNarration, "Event Narration" },
            { EventSubtype.Magic, "Magic" },
            { EventSubtype.Movie, "Movie" },
            { EventSubtype.QuestBattle, "Quest Battle" },
            { EventSubtype.Ritual, "Ritual" },
            { EventSubtype.UI, "UI" },
            { EventSubtype.Vocalisation, "Vocalisation" }
        };

        public static Dictionary<EventType, List<EventSubtype>> EventTypeToSubtypes { get; } = new Dictionary<EventType, List<EventSubtype>>
        {
            {
                EventType.FrontendVO, new List<EventSubtype>
                {
                    EventSubtype.Lord
                }
            },

            {
                EventType.CampaignVO, new List<EventSubtype>
                {
                    EventSubtype.Lord,
                    EventSubtype.Hero,
                    EventSubtype.Creature
                }
            },

            {
                EventType.CampaignConversationalVO, new List<EventSubtype>
                {
                    EventSubtype.Dummy,
                }
            },

            {
                EventType.BattleVO, new List<EventSubtype>
                {
                    EventSubtype.LordMelee,
                    EventSubtype.LordSkirmisher,
                    EventSubtype.LordCaster,
                    EventSubtype.HeroMelee,
                    EventSubtype.HeroSkirmisher,
                    EventSubtype.HeroCaster,
                    EventSubtype.UnitInfantry,
                    EventSubtype.UnitSkirmisher,
                    EventSubtype.UnitCavalry,
                    EventSubtype.UnitSEM,
                    EventSubtype.UnitArtillery
                }
            },

            {
                EventType.BattleConversationalVO, new List<EventSubtype>
                {
                    EventSubtype.Dummy,
                }
            },

            {
                EventType.BattleIndividualMelee, new List<EventSubtype>
                {
                    EventSubtype.Dummy,
                }
            },

            {
                EventType.NonVO, new List<EventSubtype>
                {
                    EventSubtype.Ability,
                    EventSubtype.CampaignAdvisor,
                    EventSubtype.DiplomacyLine,
                    EventSubtype.EventNarration,
                    EventSubtype.Magic,
                    EventSubtype.Movie,
                    EventSubtype.QuestBattle,
                    EventSubtype.Ritual,
                    EventSubtype.UI,
                    EventSubtype.Vocalisation
                }
            }
        };

        // The lists of Dialogue Events have to be defined directly rather than dynamically as there's no programatic way to determine them, I just know what is essential.
        public static List<(string EventName, EventType Type, EventSubtype[] Subtype, bool Recommended)> DialogueEvents { get; } = new List<(string, EventType, EventSubtype[], bool)>
        {
            // Frontend VO
            ("frontend_vo_character_select", EventType.FrontendVO, new[] { EventSubtype.Lord }, true),

            // Campaign VO
            ("campaign_vo_agent_action_failed", EventType.CampaignVO, new[] { EventSubtype.Hero }, true),
            ("campaign_vo_agent_action_success", EventType.CampaignVO, new[] { EventSubtype.Hero }, true),
            ("campaign_vo_attack", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_cam_disband", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_neg", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_pos", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree_response", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree_response", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_created", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_diplomacy_negative", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_diplomacy_positive", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_diplomacy_selected", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_level_up", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_mounted_creature", EventType.CampaignVO, new[] { EventSubtype.Creature }, true),
            ("campaign_vo_move", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_move_garrisoning", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_move_next_turn", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_new_commander", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_no", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_no_short", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_post_battle_defeat", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_post_battle_victory", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_recruit_units", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_retreat", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected_allied", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected_fail", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected_first_time", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected_neutral", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_selected_short", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_ship_dock", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_special_ability", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_stance_ambush", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_astromancy", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_channeling", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_default", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_double_time", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_land_raid", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_march", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_muster", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_patrol", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_raise_dead", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp_raiding", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_settle", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_stalking", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_stance_tunneling", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("campaign_vo_yes", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_yes_short", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("campaign_vo_yes_short_aggressive", EventType.CampaignVO, new[] { EventSubtype.Lord, EventSubtype.Hero }, true),
            ("gotrek_felix_arrival", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),
            ("gotrek_felix_departure", EventType.CampaignVO, new[] { EventSubtype.Lord }, true),

            // Campaign Conversational VO
            ("Campaign_CS_Nur_Plague_Infect", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("Campaign_CS_Nur_Plague_Summon_Cultist", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_buildings_damaged", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_high_corruption", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_other_generic", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_own_generic", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_public_order_low", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_riot", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_under_siege", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_confident", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_enemy_region_generic", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_purchase_doomrocket", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomflayer", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomwheel", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_accept", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_category", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_unit", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_forest", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_mountains", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_rain", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_snow", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_intimidated", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_ghrond", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_lustria", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_naggaroth", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_old_world", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_event", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_near_sea", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_neutral", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_on_sea", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_low_loyalty", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_neutral", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_positive", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_enslave", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_execute", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_release", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_defeat", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_victory", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_defeat", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_defeat", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_victory", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_do_nothing", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_establish_foreign_slot", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_loot", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_factory", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_outpost", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_tower", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_raze", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_reinstate_elector_count", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_sack", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_vassal_enlist", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_victory", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_fight_battle", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_retreat", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_break", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_continue", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_proximity", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_sacrifice_to_sotek", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_sea_storm", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_spam_click", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_summon_elector_counts_panel_open_vo", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_calls_and_taunts", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_whispers", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_cold", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_hot", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("campaign_vo_cs_wef_daiths_forge", EventType.CampaignConversationalVO, new[] { EventSubtype.Dummy }, true),

            // Battle VO
            ("battle_vo_order_attack", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_attack_alternative", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_neg", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_pos", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_survival", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_bat_speeches", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_battle_continue_battle", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_battle_quit_battle", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_change_ammo", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_change_formation", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_climb", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_off", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_on", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_flying_charge", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_formation_lock", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_formation_unlock", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_generic_response", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_group_created", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_group_disbanded", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_guard_off", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_guard_on", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_halt", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_man_siege_tower", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_melee_off", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_melee_on", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_move", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_move_alternative", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_move_ram", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_move_siege_tower", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_pick_up_engine", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_select", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_short_order", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_off", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_on", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_special_ability", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_withdraw", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_order_withdraw_tactical", EventType.BattleVO, new[] { EventSubtype.Dummy }, true),

            // Battle Conversational VO
            ("battle_vo_conversation_allied_unit_routing", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_clash", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_100_percent", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_75_percent", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_dissapointment", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_encouragement", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_at_chokepoint", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_black_arks_triggered", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_has_many_cannons", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_skaven_unit_revealed", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_at_rear", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_charging", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_chariot_charge", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_dragon", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flanking", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flying", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_large_creature", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_revealed", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_spell_cast", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_forest", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_mud", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_cave", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_water", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_cold", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_desert", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_rain", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_snow", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_air_units", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_low_stength", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_lzd_own_army_dino_rampage", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_at_chokepoint", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_black_arks_triggered", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_caused_damage", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_inferior", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_superior", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_peasants_fleeing", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_spell_cast", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_fire", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_firing", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_reload", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_fearful", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_moving", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_routing", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_dragon_firebreath_attack", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_ranged_attack", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_wavering", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_proximity", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_attack", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_defence", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_spawn_units", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_tactical_withdraw", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_warpfire_artillery", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),
            ("battle_vo_conversation_storm_of_magic", EventType.BattleConversationalVO, new[] { EventSubtype.Dummy }, true),

            // Battle Individual Melee
            ("Battle_Individual_Melee_Weapon_Hit", EventType.BattleIndividualMelee, new[] { EventSubtype.Dummy }, true),
        };
    }
}
