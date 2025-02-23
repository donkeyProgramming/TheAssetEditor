using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.GameSettings.Warhammer3
{
    public class SoundBanks
    {
        public enum Wh3SoundBankSubType
        {
            Abilities,
            CampaignAdvisor,
            DiplomacyLines,
            EventNarration,
            Magic,
            Movies,
            QuestBattles,
            //Rituals,
            //UI,
            //Vocalisation,
            //FrontendMusic,
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

        public static Wh3SoundBankType GetSoundBankSubType(Wh3SoundBankSubType soundbank)
        {
            return soundbank switch
            {
                Wh3SoundBankSubType.Abilities => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.CampaignAdvisor => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.DiplomacyLines => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.EventNarration => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.Magic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.Movies => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.QuestBattles => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.Rituals => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.UI => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.Vocalisation => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.FrontendMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.CampaignMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.BattleMusic => Wh3SoundBankType.ActionEventSoundBank,
                //Wh3SoundBankSubType.LoadingScreenMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBankSubType.FrontendVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubType.CampaignVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubType.CampaignConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubType.BattleVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBankSubType.BattleConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                //Wh3SoundBankSubType.BattleIndividualMelee => Wh3SoundBankType.DialogueEventSoundBank
            };
        }

        public const string AbilitiesDisplayString = "Abilities";
        public const string CampaignAdvisorDisplayString = "Campaign Advisor";
        public const string DiplomacyLinesDisplayString = "Diplomacy Lines";
        public const string EventNarrationDisplayString = "Event Narration";
        public const string MagicDisplayString = "Magic";
        public const string MoviesDisplayString = "Movies";
        public const string QuestBattlesDisplayString = "Quest Battles";
        public const string RitualsDisplayString = "Rituals";
        public const string UIDisplayString = "UI";
        public const string VocalisationDisplayString = "Vocalisation";
        public const string FrontendMusicDisplayString = "Frontend Music";
        public const string CampaignMusicDisplayString = "Campaign Music";
        public const string BattleMusicDisplayString = "Battle Music";
        public const string LoadingScreenMusicDisplayString = "Loading Screen Music";
        public const string FrontendVODisplayString = "Frontend VO";
        public const string CampaignVODisplayString = "Campaign VO";
        public const string CampaignConversationalVODisplayString = "Campaign Conversational VO";
        public const string BattleVODisplayString = "Battle VO";
        public const string BattleConversationalVODisplayString = "Battle Conversational VO";
        public const string BattleIndividualMeleeDisplayString = "Battle Individual Melee";

        public static string GetSoundBankSubTypeDisplayString(Wh3SoundBankSubType soundbank)
        {
            return soundbank switch
            {
                Wh3SoundBankSubType.Abilities => AbilitiesDisplayString,
                Wh3SoundBankSubType.CampaignAdvisor => CampaignAdvisorDisplayString,
                Wh3SoundBankSubType.DiplomacyLines => DiplomacyLinesDisplayString,
                Wh3SoundBankSubType.EventNarration => EventNarrationDisplayString,
                Wh3SoundBankSubType.Magic => MagicDisplayString,
                Wh3SoundBankSubType.Movies => MoviesDisplayString,
                Wh3SoundBankSubType.QuestBattles => QuestBattlesDisplayString,
                //Wh3SoundBankSubType.Rituals => RitualsDisplayString,
                //Wh3SoundBankSubType.UI => UIDisplayString,
                //Wh3SoundBankSubType.Vocalisation => VocalisationDisplayString,
                //Wh3SoundBankSubType.FrontendMusic => FrontendMusicDisplayString,
                //Wh3SoundBankSubType.CampaignMusic => CampaignMusicDisplayString,
                //Wh3SoundBankSubType.BattleMusic => BattleMusicDisplayString,
                //Wh3SoundBankSubType.LoadingScreenMusic => LoadingScreenMusicDisplayString,
                Wh3SoundBankSubType.FrontendVO => FrontendVODisplayString,
                Wh3SoundBankSubType.CampaignVO => CampaignVODisplayString,
                Wh3SoundBankSubType.CampaignConversationalVO => CampaignConversationalVODisplayString,
                Wh3SoundBankSubType.BattleVO => BattleVODisplayString,
                Wh3SoundBankSubType.BattleConversationalVO => BattleConversationalVODisplayString,
                //Wh3SoundBankSubType.BattleIndividualMelee => BattleIndividualMeleeDisplayString,
            };
        }

        public static Wh3SoundBankSubType GetSoundBankEnum(string soundBankString)
        {
            return soundBankString switch
            {
                AbilitiesDisplayString => Wh3SoundBankSubType.Abilities,
                CampaignAdvisorDisplayString => Wh3SoundBankSubType.CampaignAdvisor,
                DiplomacyLinesDisplayString => Wh3SoundBankSubType.DiplomacyLines,
                EventNarrationDisplayString => Wh3SoundBankSubType.EventNarration,
                MagicDisplayString => Wh3SoundBankSubType.Magic,
                MoviesDisplayString => Wh3SoundBankSubType.Movies,
                QuestBattlesDisplayString => Wh3SoundBankSubType.QuestBattles,
                //RitualsDisplayString => Wh3SoundBankSubType.Rituals,
                //UIDisplayString => Wh3SoundBankSubType.UI,
                //VocalisationDisplayString => Wh3SoundBankSubType.Vocalisation,
                //FrontendMusicDisplayString => Wh3SoundBankSubType.FrontendMusic,
                //CampaignMusicDisplayString => Wh3SoundBankSubType.CampaignMusic,
                //BattleMusicDisplayString => Wh3SoundBankSubType.BattleMusic,
                //LoadingScreenMusicDisplayString => Wh3SoundBankSubType.LoadingScreenMusic,
                FrontendVODisplayString => Wh3SoundBankSubType.FrontendVO,
                CampaignVODisplayString => Wh3SoundBankSubType.CampaignVO,
                CampaignConversationalVODisplayString => Wh3SoundBankSubType.CampaignConversationalVO,
                BattleVODisplayString => Wh3SoundBankSubType.BattleVO,
                BattleConversationalVODisplayString => Wh3SoundBankSubType.BattleConversationalVO,
                //BattleIndividualMeleeDisplayString => Wh3SoundBankSubType.BattleIndividualMelee,
            };
        }
    }
}
