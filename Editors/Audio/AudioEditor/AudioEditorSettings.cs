using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorSettings
    {
        public static readonly List<string> ModdedStateGroups = ["VO_Actor", "VO_Culture", "VO_Faction_Leader", "VO_Battle_Selection", "VO_Battle_Special_Ability"];

        public static Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroups { get; set; } = [];

        public enum SoundBankType
        {
            ActionEventBnk,
            DialogueEventBnk,
            MusicEventBnk
        }

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
            BattleIndividualMelee
        }

        public enum AudioType
        {
            // Action Events
            Abilities,
            CampaignAdvisor,
            DiplomacyLines,
            EventNarration,
            Magic,
            Movies,
            QuestBattles,
            Rituals,
            UI,
            Vocalisation,

            // Dialogue Events
            BattleIndividualMelee,
            BattleConversationalVO,
            BattleVO,
            CampaignConversationalVO,
            CampaignVO,
            FrontendVO,

            // Music Events
            BattleMusic,
            CampaignMusic,
            FrontendMusic
        }

        public enum AudioSubtype
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

        public static string GetStringFromLanguage(Language language)
        {
            return language switch
            {
                Language.Chinese => "chinese",
                Language.EnglishUK => "english(uk)",
                Language.FrenchFrance => "french(france)",
                Language.German => "german",
                Language.Italian => "italian",
                Language.Polish => "polish",
                Language.Russian => "russian",
                Language.SpanishSpain => "spanish(spain)",
            };
        }


        public static string GetStringFromAudioType(AudioType audioType)
        {
            return audioType switch
            {
                // Action Events
                AudioType.Abilities => "Abilities",
                AudioType.CampaignAdvisor => "Campaign Advisor",
                AudioType.DiplomacyLines => "Diplomacy Lines",
                AudioType.EventNarration => "Event Narration",
                AudioType.Magic => "Magic",
                AudioType.Movies => "Movies",
                AudioType.QuestBattles => "Quest Battles",
                AudioType.Rituals => "Rituals",
                AudioType.UI => "UI",
                AudioType.Vocalisation => "Vocalisation",

                // Dialogue Events
                AudioType.FrontendVO => "Frontend VO",
                AudioType.CampaignVO => "Campaign VO",
                AudioType.CampaignConversationalVO => "Campaign Conversational VO",
                AudioType.BattleVO => "Battle VO",
                AudioType.BattleConversationalVO => "Battle Conversational VO",
                AudioType.BattleIndividualMelee => "Battle Individual Melee",

                // Music Events
                AudioType.BattleMusic => "Battle Music",
                AudioType.CampaignMusic => "Campaign Music",
                AudioType.FrontendMusic => "Frontend Music",
            };
        }

        public static AudioType GetAudioTypeFromString(string audioTypeString)
        {
            return audioTypeString switch
            {
                // Action Events
                "Abilities (Action Events)" => AudioType.Abilities,
                "Campaign Advisor (Action Events)" => AudioType.CampaignAdvisor,
                "Diplomacy Lines (Action Events)" => AudioType.DiplomacyLines,
                "Event Narration (Action Events)" => AudioType.EventNarration,
                "Magic (Action Events)" => AudioType.Magic,
                "Movies (Action Events)" => AudioType.Movies,
                "Quest Battles (Action Events)" => AudioType.QuestBattles,
                "Rituals (Action Events)" => AudioType.Rituals,
                "UI (Action Events)" => AudioType.UI,
                "Vocalisation (Action Events)" => AudioType.Vocalisation,

                // Dialogue Events
                "Battle Conversational VO (Dialogue Events)" => AudioType.BattleConversationalVO,
                "Battle Individual Melee (Dialogue Events)" => AudioType.BattleIndividualMelee,
                "Battle VO (Dialogue Events)" => AudioType.BattleVO,
                "Campaign Conversational VO (Dialogue Events)" => AudioType.CampaignConversationalVO,
                "Campaign VO (Dialogue Events)" => AudioType.CampaignVO,
                "Frontend VO (Dialogue Events)" => AudioType.FrontendVO,

                // Music Events
                "Battle Music (Music Events)" => AudioType.BattleMusic,
                "Campaign Music (Music Events)" => AudioType.CampaignMusic,
                "Frontend Music (Music Events)" => AudioType.FrontendMusic
            };
        }

        public static string GetStringFromAudioSubtype(AudioSubtype audioSubtype)
        {
            return audioSubtype switch
            {
                AudioSubtype.Dummy => "Dummy",
                AudioSubtype.Lord => "Lord",
                AudioSubtype.Hero => "Hero",
                AudioSubtype.Creature => "Creature",
                AudioSubtype.LordMelee => "Lord - Melee",
                AudioSubtype.LordSkirmisher => "Lord - Skirmisher",
                AudioSubtype.LordCaster => "Lord - Caster",
                AudioSubtype.HeroMelee => "Hero - Melee",
                AudioSubtype.HeroSkirmisher => "Hero - Skirmisher",
                AudioSubtype.HeroCaster => "Hero - Caster",
                AudioSubtype.UnitInfantry => "Unit - Infantry",
                AudioSubtype.UnitSkirmisher => "Unit - Skirmisher",
                AudioSubtype.UnitCavalry => "Unit - Cavalry",
                AudioSubtype.UnitSEM => "Unit - SEM",
                AudioSubtype.UnitArtillery => "Unit - Artillery",
            };
        }

        public static SoundBankType GetSoundBankTypeFromAudioType(AudioType audioType)
        {
            return audioType switch
            {
                // Action Events
                AudioType.Abilities => SoundBankType.ActionEventBnk,
                AudioType.CampaignAdvisor => SoundBankType.ActionEventBnk,
                AudioType.DiplomacyLines => SoundBankType.ActionEventBnk,
                AudioType.EventNarration => SoundBankType.ActionEventBnk,
                AudioType.Magic => SoundBankType.ActionEventBnk,
                AudioType.Movies => SoundBankType.MusicEventBnk,
                AudioType.QuestBattles => SoundBankType.ActionEventBnk,
                AudioType.Rituals => SoundBankType.ActionEventBnk,
                AudioType.UI => SoundBankType.ActionEventBnk,
                AudioType.Vocalisation => SoundBankType.ActionEventBnk,

                // Dialogue Events
                AudioType.FrontendVO => SoundBankType.DialogueEventBnk,
                AudioType.CampaignVO => SoundBankType.DialogueEventBnk,
                AudioType.CampaignConversationalVO => SoundBankType.DialogueEventBnk,
                AudioType.BattleVO => SoundBankType.DialogueEventBnk,
                AudioType.BattleConversationalVO => SoundBankType.DialogueEventBnk,
                AudioType.BattleIndividualMelee => SoundBankType.DialogueEventBnk,

                // Music Events
                AudioType.BattleMusic => SoundBankType.MusicEventBnk,
                AudioType.CampaignMusic => SoundBankType.MusicEventBnk,
                AudioType.FrontendMusic => SoundBankType.MusicEventBnk,
            };
        }

        public static List<AudioSubtype> GetAudioSubtypesFromAudioType(AudioType audioType)
        {
            return audioType switch
            {
                AudioType.FrontendVO => new List<AudioSubtype> 
                { 
                    AudioSubtype.Lord 
                },

                AudioType.CampaignVO => new List<AudioSubtype>
                {
                    AudioSubtype.Lord,
                    AudioSubtype.Hero,
                    AudioSubtype.Creature
                },

                AudioType.CampaignConversationalVO => new List<AudioSubtype>
                {
                    AudioSubtype.Dummy
                },

                AudioType.BattleVO => new List<AudioSubtype>
                {
                    AudioSubtype.LordMelee,
                    AudioSubtype.LordSkirmisher,
                    AudioSubtype.LordCaster,
                    AudioSubtype.HeroMelee,
                    AudioSubtype.HeroSkirmisher,
                    AudioSubtype.HeroCaster,
                    AudioSubtype.UnitInfantry,
                    AudioSubtype.UnitSkirmisher,
                    AudioSubtype.UnitCavalry,
                    AudioSubtype.UnitSEM,
                    AudioSubtype.UnitArtillery
                },

                AudioType.BattleConversationalVO => new List<AudioSubtype>
                {
                    AudioSubtype.Dummy
                },

                AudioType.BattleIndividualMelee => new List<AudioSubtype>
                {
                    AudioSubtype.Dummy
                },
            };
        }

        // The lists of Dialogue Events have to be defined directly rather than dynamically as there's no programatic way to determine them, I just know what is essential.
        public static List<(string EventName, AudioType Type, AudioSubtype[] Subtype, bool Recommended)> DialogueEvents { get; } = new List<(string, AudioType, AudioSubtype[], bool)>
        {
            // Frontend VO
            ("frontend_vo_character_select", AudioType.FrontendVO, new[] { AudioSubtype.Lord }, true),

            // Campaign VO
            ("campaign_vo_agent_action_failed", AudioType.CampaignVO, new[] { AudioSubtype.Hero }, true),
            ("campaign_vo_agent_action_success", AudioType.CampaignVO, new[] { AudioSubtype.Hero }, true),
            ("campaign_vo_attack", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_cam_disband", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_neg", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_disbanded_pos", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_skill_weapon_tree_response", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_cam_tech_tree_response", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_created", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_diplomacy_negative", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_diplomacy_positive", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_diplomacy_selected", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_level_up", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_mounted_creature", AudioType.CampaignVO, new[] { AudioSubtype.Creature }, true),
            ("campaign_vo_move", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_move_garrisoning", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_move_next_turn", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_new_commander", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_no", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_no_short", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_post_battle_defeat", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_post_battle_victory", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_recruit_units", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_retreat", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected_allied", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected_fail", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected_first_time", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected_neutral", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_selected_short", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_ship_dock", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_special_ability", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_stance_ambush", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_astromancy", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_channeling", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_default", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_double_time", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_land_raid", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_march", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_muster", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_patrol", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_raise_dead", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_set_camp_raiding", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_settle", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_stalking", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_stance_tunneling", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("campaign_vo_yes", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_yes_short", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("campaign_vo_yes_short_aggressive", AudioType.CampaignVO, new[] { AudioSubtype.Lord, AudioSubtype.Hero }, true),
            ("gotrek_felix_arrival", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),
            ("gotrek_felix_departure", AudioType.CampaignVO, new[] { AudioSubtype.Lord }, true),

            // Campaign Conversational VO
            ("Campaign_CS_Nur_Plague_Infect", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("Campaign_CS_Nur_Plague_Summon_Cultist", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_buildings_damaged", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_high_corruption", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_other_generic", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_own_generic", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_public_order_low", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_riot", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_city_under_siege", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_confident", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_enemy_region_generic", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_purchase_doomrocket", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomflayer", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_doomwheel", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_forbidden_workshop_upgrade_weapon_teams", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_accept", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_category", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_hellforge_customisation_unit", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_in_forest", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_in_mountains", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_in_rain", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_in_snow", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_intimidated", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_ghrond", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_lustria", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_naggaroth", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_dilemma_old_world", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_monster_pens_event", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_near_sea", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_neutral", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_on_sea", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_low_loyalty", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_neutral", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_other_character_details_panel_positive", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_enslave", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_execute", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_captives_release", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_defeat", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_close_victory", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_defeat", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_defeat", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_great_victory", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_do_nothing", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_establish_foreign_slot", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_loot", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_factory", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_outpost", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_occupy_tower", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_raze", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_reinstate_elector_count", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_sack", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_settlement_vassal_enlist", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_post_battle_victory", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_fight_battle", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_retreat", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_break", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_pre_battle_siege_continue", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_proximity", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_sacrifice_to_sotek", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_sea_storm", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_spam_click", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_summon_elector_counts_panel_open_vo", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_calls_and_taunts", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_tzarkan_whispers", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_cold", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_weather_hot", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("campaign_vo_cs_wef_daiths_forge", AudioType.CampaignConversationalVO, new[] { AudioSubtype.Dummy }, true),

            // Battle VO
            ("battle_vo_order_attack", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_attack_alternative", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_neg", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_capture_pos", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_bat_mode_survival", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_bat_speeches", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_battle_continue_battle", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_battle_quit_battle", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_change_ammo", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_change_formation", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_climb", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_off", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_fire_at_will_on", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_flying_charge", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_formation_lock", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_formation_unlock", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_generic_response", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_group_created", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_group_disbanded", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_guard_off", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_guard_on", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_halt", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_man_siege_tower", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_melee_off", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_melee_on", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_move", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_move_alternative", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_move_ram", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_move_siege_tower", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_pick_up_engine", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_select", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_short_order", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_off", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_skirmish_on", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_special_ability", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_withdraw", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_order_withdraw_tactical", AudioType.BattleVO, new[] { AudioSubtype.Dummy }, true),

            // Battle Conversational VO
            ("battle_vo_conversation_allied_unit_routing", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_clash", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_100_percent", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_def_own_army_murderous_prowess_75_percent", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_dissapointment", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_encouragement", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_at_chokepoint", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_black_arks_triggered", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_army_has_many_cannons", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_skaven_unit_revealed", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_at_rear", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_charging", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_chariot_charge", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_dragon", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flanking", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_flying", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_large_creature", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_revealed", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_enemy_unit_spell_cast", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_forest", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_ground_type_mud", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_cave", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_in_water", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_cold", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_desert", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_rain", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_environment_weather_snow", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_air_units", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_hef_own_army_low_stength", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_lzd_own_army_dino_rampage", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_at_chokepoint", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_black_arks_triggered", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_caused_damage", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_inferior", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_missile_amount_superior", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_peasants_fleeing", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_army_spell_cast", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_fire", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_firing", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_artillery_reload", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_fearful", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_moving", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_routing", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_dragon_firebreath_attack", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_under_ranged_attack", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_own_unit_wavering", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_proximity", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_attack", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_siege_defence", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_spawn_units", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_tactical_withdraw", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_skv_own_unit_warpfire_artillery", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),
            ("battle_vo_conversation_storm_of_magic", AudioType.BattleConversationalVO, new[] { AudioSubtype.Dummy }, true),

            // Battle Individual Melee
            ("Battle_Individual_Melee_Weapon_Hit", AudioType.BattleIndividualMelee, new[] { AudioSubtype.Dummy }, true),
        };

        // Add qualifiers to State Groups so that dictionary keys are unique as some events have the same State Group twice e.g. VO_Actor
        public static void AddQualifiersToStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                var stateGroupsWithQualifiers = new Dictionary<string, string>();
                var stateGroups = dialogueEvent.Value;

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in stateGroups)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        var qualifier = voActorCount > 1 ? "VO_Actor (Reference)" : "VO_Actor (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Actor";
                    }

                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        var qualifier = voCultureCount > 1 ? "VO_Culture (Reference)" : "VO_Culture (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Culture";
                    }

                    else
                    {
                        // No qualifier needed, add the same state group as both original and qualified
                        stateGroupsWithQualifiers[stateGroup] = stateGroup;
                    }
                }

                DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Key] = stateGroupsWithQualifiers;
            }
        }
    }
}
