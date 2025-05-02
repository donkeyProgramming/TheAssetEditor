namespace Editors.Audio.GameSettings.Warhammer3
{
    public class SoundBanks
    {
        public enum Wh3SoundBankSubtype
        {
            //Abilities,
            //CampaignAdvisor,
            DiplomacyLines,
            //EventNarration,
            //Magic,
            Movies,
            QuestBattles,
            //Rituals,
            //UI,
            //Vocalisation,
            FrontendMusic,
            //CampaignMusic,
            //BattleMusic,
            //LoadingScreenMusic,
            FrontendVO,
            CampaignVO,
            CampaignConversationalVO,
            BattleVO,
            BattleConversationalVO,
            //BattleIndividualMelee
        }

        public enum Wh3SoundBankType
        {
            ActionEventSoundBank,
            DialogueEventSoundBank
        }

        //public const string AbilitiesDisplayString = "Abilities";
        //public const string CampaignAdvisorDisplayString = "Campaign Advisor";
        public const string DiplomacyLinesDisplayString = "Diplomacy Lines";
        public const string DiplomacyLinesSoundBankName = "campaign_diplomacy";
        //public const string EventNarrationDisplayString = "Event Narration";
        //public const string MagicDisplayString = "Magic";
        public const string MoviesDisplayString = "Movies";
        public const string MoviesSoundBankName = "global_movies";
        public const string QuestBattlesDisplayString = "Quest Battles";
        public const string QuestBattlesSoundBankName = "battle_vo_generals_speech";
        //public const string RitualsDisplayString = "Rituals";
        //public const string UIdisplayString = "UI";
        //public const string VocalisationDisplayString = "Vocalisation";
        public const string FrontendMusicDisplayString = "Frontend Music";
        public const string FrontendMusicSoundBankName = "global_music_frontend";
        //public const string CampaignMusicDisplayString = "Campaign Music";
        //public const string CampaignMusicSoundBankName = "campaign_music";
        //public const string BattleMusicDisplayString = "Battle Music";
        //public const string LoadingScreenMusicDisplayString = "Loading Screen Music";
        public const string FrontendVODisplayString = "Frontend VO";
        public const string FrontendVOSoundBankName = "frontend_vo";
        public const string CampaignVODisplayString = "Campaign VO";
        public const string CampaignVOSoundBankName = "campaign_vo";
        public const string CampaignConversationalVODisplayString = "Campaign Conversational VO";
        public const string CampaignConversationalVOSoundBankName = "campaign_conversational_vo";
        public const string BattleVODisplayString = "Battle VO";
        public const string BattleVOSoundBankName = "battle_vo_orders";
        public const string BattleConversationalVODisplayString = "Battle Conversational VO";
        public const string BattleConversationalVOSoundBankName = "battle_vo_conversational";
        //public const string BattleIndividualMeleeDisplayString = "Battle Individual Melee";

        public static Wh3SoundBankType GetSoundBankSubType(Wh3SoundBankSubtype soundbank)
        {
            return soundbank switch
            {
                //Wh3SoundBankSubtype.Abilities => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.CampaignAdvisor => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubtype.DiplomacyLines => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.EventNarration => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.Magic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubtype.Movies => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubtype.QuestBattles => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.Rituals => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.UI => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.Vocalisation => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubtype.FrontendMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.CampaignMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.BattleMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubtype.LoadingScreenMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubtype.FrontendVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubtype.CampaignVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubtype.CampaignConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubtype.BattleVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubtype.BattleConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                //Wh3SoundBankSubtype.BattleIndividualMelee => Wh3SoundBankType.DialogueEventSoundBank
            };
        }

        public static string GetSoundBankSubTypeString(Wh3SoundBankSubtype soundbank)
        {
            return soundbank switch
            {
                //Wh3SoundBankSubtype.Abilities => AbilitiesDisplayString,
                //Wh3SoundBankSubtype.CampaignAdvisor => CampaignAdvisorDisplayString,
                Wh3SoundBankSubtype.DiplomacyLines => DiplomacyLinesDisplayString,
                //Wh3SoundBankSubtype.EventNarration => EventNarrationDisplayString,
                //Wh3SoundBankSubtype.Magic => MagicDisplayString,
                Wh3SoundBankSubtype.Movies => MoviesDisplayString,
                Wh3SoundBankSubtype.QuestBattles => QuestBattlesDisplayString,
                //Wh3SoundBankSubtype.Rituals => RitualsDisplayString,
                //Wh3SoundBankSubtype.UI => UIdisplayString,
                //Wh3SoundBankSubtype.Vocalisation => VocalisationDisplayString,
                Wh3SoundBankSubtype.FrontendMusic => FrontendMusicDisplayString,
                //Wh3SoundBankSubtype.CampaignMusic => CampaignMusicDisplayString,
                //Wh3SoundBankSubtype.BattleMusic => BattleMusicDisplayString,
                //Wh3SoundBankSubtype.LoadingScreenMusic => LoadingScreenMusicDisplayString,
                Wh3SoundBankSubtype.FrontendVO => FrontendVODisplayString,
                Wh3SoundBankSubtype.CampaignVO => CampaignVODisplayString,
                Wh3SoundBankSubtype.CampaignConversationalVO => CampaignConversationalVODisplayString,
                Wh3SoundBankSubtype.BattleVO => BattleVODisplayString,
                Wh3SoundBankSubtype.BattleConversationalVO => BattleConversationalVODisplayString,
                //Wh3SoundBankSubtype.BattleIndividualMelee => BattleIndividualMeleeDisplayString,
            };
        }

        public static Wh3SoundBankSubtype GetSoundBankSubtype(string soundBankString)
        {
            return soundBankString switch
            {
                //AbilitiesDisplayString => Wh3SoundBankSubtype.Abilities,
                //CampaignAdvisorDisplayString => Wh3SoundBankSubtype.CampaignAdvisor,
                DiplomacyLinesDisplayString => Wh3SoundBankSubtype.DiplomacyLines,
                //EventNarrationDisplayString => Wh3SoundBankSubtype.EventNarration,
                //MagicDisplayString => Wh3SoundBankSubtype.Magic,
                MoviesDisplayString => Wh3SoundBankSubtype.Movies,
                //QuestBattlesDisplayString => Wh3SoundBankSubtype.QuestBattles,
                //RitualsDisplayString => Wh3SoundBankSubtype.Rituals,
                //UIdisplayString => Wh3SoundBankSubtype.UI,
                //VocalisationDisplayString => Wh3SoundBankSubtype.Vocalisation,
                FrontendMusicDisplayString => Wh3SoundBankSubtype.FrontendMusic,
                //CampaignMusicDisplayString => Wh3SoundBankSubtype.CampaignMusic,
                //BattleMusicDisplayString => Wh3SoundBankSubtype.BattleMusic,
                //LoadingScreenMusicDisplayString => Wh3SoundBankSubtype.LoadingScreenMusic,
                FrontendVODisplayString => Wh3SoundBankSubtype.FrontendVO,
                CampaignVODisplayString => Wh3SoundBankSubtype.CampaignVO,
                CampaignConversationalVODisplayString => Wh3SoundBankSubtype.CampaignConversationalVO,
                BattleVODisplayString => Wh3SoundBankSubtype.BattleVO,
                BattleConversationalVODisplayString => Wh3SoundBankSubtype.BattleConversationalVO,
                //BattleIndividualMeleeDisplayString => Wh3SoundBankSubtype.BattleIndividualMelee,
            };
        }


        public static string GetSoundBankName(Wh3SoundBankSubtype soundbank)
        {
            return soundbank switch
            {
                Wh3SoundBankSubtype.DiplomacyLines => DiplomacyLinesSoundBankName,
                Wh3SoundBankSubtype.Movies => MoviesSoundBankName,
                Wh3SoundBankSubtype.QuestBattles => QuestBattlesSoundBankName,

                Wh3SoundBankSubtype.FrontendMusic => FrontendMusicSoundBankName,
                //Wh3SoundBankSubtype.CampaignMusic => CampaignMusicSoundBankName,

                Wh3SoundBankSubtype.FrontendVO => FrontendVOSoundBankName,
                Wh3SoundBankSubtype.CampaignVO => CampaignVOSoundBankName,
                Wh3SoundBankSubtype.CampaignConversationalVO => CampaignConversationalVOSoundBankName,
                Wh3SoundBankSubtype.BattleVO => BattleVOSoundBankName,
                Wh3SoundBankSubtype.BattleConversationalVO => BattleConversationalVOSoundBankName,
            };
        }
    }
}
