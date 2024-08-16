using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorSettings
    {
        public enum Language
        {
            Chinese,
            EnglishUK,
            FrenchFrance,
            German,
            Italian,
            Polish,
            Russian,
            SpanishSpain
        }

        public enum DialogueEventType
        {
            FrontendVO,
            CampaignVO,
            CampaignConversationalVO,
            BattleVO,
            BattleConversationalVO,
            BattleIndividualMelee,
        }

        public enum DialogueEventSubtype
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
        }

        public enum EventType
        {
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

        public static readonly Dictionary<Language, string> LanguageEnumToString = new()
        {
            { Language.Chinese, "chinese" },
            { Language.EnglishUK, "english(uk)" },
            { Language.FrenchFrance, "french(france)" },
            { Language.German, "german" },
            { Language.Italian, "italian" },
            { Language.Polish, "polish" },
            { Language.Russian, "russian" },
            { Language.SpanishSpain, "spanish(spain)" }
        };

        public static readonly Dictionary<DialogueEventType, string> DialogueEventTypeEnumToString = new()
        {
            { DialogueEventType.FrontendVO, "Frontend VO" },
            { DialogueEventType.CampaignVO, "Campaign VO" },
            { DialogueEventType.CampaignConversationalVO, "Campaign Conversational VO" },
            { DialogueEventType.BattleVO, "Battle VO" },
            { DialogueEventType.BattleConversationalVO, "Battle Conversational VO" },
            { DialogueEventType.BattleIndividualMelee, "Battle Individual Melee" },
        };

        public static readonly Dictionary<DialogueEventSubtype, string> DialogueEventSubtypeEnumToString = new()
        {
            { DialogueEventSubtype.Dummy, "Dummy" },
            { DialogueEventSubtype.Lord, "Lord" },
            { DialogueEventSubtype.Hero, "Hero" },
            { DialogueEventSubtype.Creature, "Creature" },
            { DialogueEventSubtype.LordMelee, "Lord - Melee" },
            { DialogueEventSubtype.LordSkirmisher, "Lord - Skirmisher" },
            { DialogueEventSubtype.LordCaster, "Lord - Caster" },
            { DialogueEventSubtype.HeroMelee, "Hero - Melee" },
            { DialogueEventSubtype.HeroSkirmisher, "Hero - Skirmisher" },
            { DialogueEventSubtype.HeroCaster, "Hero - Caster" },
            { DialogueEventSubtype.UnitInfantry, "Unit - Infantry" },
            { DialogueEventSubtype.UnitSkirmisher, "Unit - Skirmisher" },
            { DialogueEventSubtype.UnitCavalry, "Unit - Cavalry" },
            { DialogueEventSubtype.UnitSEM, "Unit - SEM" },
            { DialogueEventSubtype.UnitArtillery, "Unit - Artillery" },
        };

        public static readonly Dictionary<EventType, string> EventSubtypeEnumToString = new()
        {

            { EventType.Ability, "Ability" },
            { EventType.CampaignAdvisor, "Campaign Advisor" },
            { EventType.DiplomacyLine, "Diplomacy Line" },
            { EventType.EventNarration, "Event Narration" },
            { EventType.Magic, "Magic" },
            { EventType.Movie, "Movie" },
            { EventType.QuestBattle, "Quest Battle" },
            { EventType.Ritual, "Ritual" },
            { EventType.UI, "UI" },
            { EventType.Vocalisation, "Vocalisation" }
        };

        public static Dictionary<DialogueEventType, List<DialogueEventSubtype>> DialogueEventTypeToSubtypes { get; } = new Dictionary<DialogueEventType, List<DialogueEventSubtype>>
        {
            {
                DialogueEventType.FrontendVO, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.Lord
                }
            },

            {
                DialogueEventType.CampaignVO, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.Lord,
                    DialogueEventSubtype.Hero,
                    DialogueEventSubtype.Creature
                }
            },

            {
                DialogueEventType.CampaignConversationalVO, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.Dummy,
                }
            },

            {
                DialogueEventType.BattleVO, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.LordMelee,
                    DialogueEventSubtype.LordSkirmisher,
                    DialogueEventSubtype.LordCaster,
                    DialogueEventSubtype.HeroMelee,
                    DialogueEventSubtype.HeroSkirmisher,
                    DialogueEventSubtype.HeroCaster,
                    DialogueEventSubtype.UnitInfantry,
                    DialogueEventSubtype.UnitSkirmisher,
                    DialogueEventSubtype.UnitCavalry,
                    DialogueEventSubtype.UnitSEM,
                    DialogueEventSubtype.UnitArtillery
                }
            },

            {
                DialogueEventType.BattleConversationalVO, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.Dummy,
                }
            },

            {
                DialogueEventType.BattleIndividualMelee, new List<DialogueEventSubtype>
                {
                    DialogueEventSubtype.Dummy,
                }
            }
        };

        public static List<EventType> EventTypes { get; } = new List<EventType>
        {
            EventType.Ability,
            EventType.CampaignAdvisor,
            EventType.DiplomacyLine,
            EventType.EventNarration,
            EventType.Magic,
            EventType.Movie,
            EventType.QuestBattle,
            EventType.Ritual,
            EventType.UI,
            EventType.Vocalisation
        };

        // The lists of Dialogue Events have to be defined directly rather than dynamically as there's no programatic way to determine them, I just know what is essential.
        public static List<(string EventName, DialogueEventType Type, DialogueEventSubtype[] Subtype, bool Recommended)> DialogueEvents { get; } = new List<(string, DialogueEventType, DialogueEventSubtype[], bool)>
        {
            // Frontend VO
            ("frontend_vo_character_select", DialogueEventType.FrontendVO, new[] { DialogueEventSubtype.Lord }, true),

            // Campaign VO
            ("campaign_vo_agent_action_failed", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Hero }, true),
            ("campaign_vo_agent_action_success", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Hero }, true),
            ("campaign_vo_attack", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_cam_disband", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_neg", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_pos", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree_response", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree_response", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_created", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_diplomacy_negative", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_diplomacy_positive", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_diplomacy_selected", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_level_up", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_mounted_creature", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Creature }, true),
            ("campaign_vo_move", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_move_garrisoning", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_move_next_turn", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_new_commander", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_no", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_no_short", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_post_battle_defeat", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_post_battle_victory", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_recruit_units", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_retreat", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected_allied", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected_fail", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected_first_time", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected_neutral", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_selected_short", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_ship_dock", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_special_ability", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_stance_ambush", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_astromancy", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_channeling", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_default", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_double_time", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_land_raid", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_march", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_muster", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_patrol", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_raise_dead", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp_raiding", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_settle", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_stalking", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_stance_tunneling", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("campaign_vo_yes", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_yes_short", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("campaign_vo_yes_short_aggressive", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord, DialogueEventSubtype.Hero }, true),
            ("gotrek_felix_arrival", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),
            ("gotrek_felix_departure", DialogueEventType.CampaignVO, new[] { DialogueEventSubtype.Lord }, true),

            // Campaign Conversational VO
            ("Campaign_CS_Nur_Plague_Infect", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("Campaign_CS_Nur_Plague_Summon_Cultist", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_buildings_damaged", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_high_corruption", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_other_generic", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_own_generic", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_public_order_low", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_riot", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_city_under_siege", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_confident", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_enemy_region_generic", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_purchase_doomrocket", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomflayer", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomwheel", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_accept", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_category", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_unit", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_forest", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_mountains", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_rain", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_in_snow", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_intimidated", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_ghrond", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_lustria", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_naggaroth", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_old_world", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_event", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_near_sea", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_neutral", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_on_sea", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_low_loyalty", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_neutral", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_positive", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_enslave", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_execute", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_release", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_defeat", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_victory", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_defeat", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_defeat", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_victory", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_do_nothing", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_establish_foreign_slot", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_loot", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_factory", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_outpost", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_tower", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_raze", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_reinstate_elector_count", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_sack", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_vassal_enlist", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_victory", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_fight_battle", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_retreat", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_break", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_continue", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_proximity", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_sacrifice_to_sotek", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_sea_storm", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_spam_click", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_summon_elector_counts_panel_open_vo", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_calls_and_taunts", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_whispers", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_cold", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_hot", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("campaign_vo_cs_wef_daiths_forge", DialogueEventType.CampaignConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),

            // Battle VO
            ("battle_vo_order_attack", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_attack_alternative", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_neg", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_pos", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_survival", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_bat_speeches", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_battle_continue_battle", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_battle_quit_battle", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_change_ammo", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_change_formation", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_climb", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_off", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_on", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_flying_charge", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_formation_lock", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_formation_unlock", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_generic_response", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_group_created", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_group_disbanded", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_guard_off", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_guard_on", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_halt", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_man_siege_tower", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_melee_off", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_melee_on", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_move", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_move_alternative", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_move_ram", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_move_siege_tower", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_pick_up_engine", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_select", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_short_order", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_off", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_on", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_special_ability", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_withdraw", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_order_withdraw_tactical", DialogueEventType.BattleVO, new[] { DialogueEventSubtype.Dummy }, true),

            // Battle Conversational VO
            ("battle_vo_conversation_allied_unit_routing", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_clash", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_100_percent", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_75_percent", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_dissapointment", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_encouragement", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_at_chokepoint", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_black_arks_triggered", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_has_many_cannons", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_skaven_unit_revealed", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_at_rear", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_charging", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_chariot_charge", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_dragon", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flanking", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flying", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_large_creature", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_revealed", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_spell_cast", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_forest", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_mud", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_cave", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_water", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_cold", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_desert", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_rain", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_snow", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_air_units", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_low_stength", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_lzd_own_army_dino_rampage", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_at_chokepoint", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_black_arks_triggered", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_caused_damage", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_inferior", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_superior", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_peasants_fleeing", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_spell_cast", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_fire", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_firing", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_reload", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_fearful", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_moving", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_routing", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_dragon_firebreath_attack", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_ranged_attack", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_wavering", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_proximity", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_attack", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_defence", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_spawn_units", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_tactical_withdraw", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_warpfire_artillery", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),
            ("battle_vo_conversation_storm_of_magic", DialogueEventType.BattleConversationalVO, new[] { DialogueEventSubtype.Dummy }, true),

            // Battle Individual Melee
            ("Battle_Individual_Melee_Weapon_Hit", DialogueEventType.BattleIndividualMelee, new[] { DialogueEventSubtype.Dummy }, true),
        };
    }
}
