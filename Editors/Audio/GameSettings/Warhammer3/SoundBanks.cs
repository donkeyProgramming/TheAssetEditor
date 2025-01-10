namespace Editors.Audio.GameSettings.Warhammer3
{
    public class SoundBanks
    {
        public enum GameSoundBank
        {
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
            FrontendMusic,
            CampaignMusic,
            BattleMusic,
            LoadingScreenMusic,
            FrontendVO,
            CampaignVO,
            CampaignConversationalVO,
            BattleVO,
            BattleConversationalVO,
            BattleIndividualMelee
        }

        public enum GameSoundBankType
        {
            ActionEventSoundBank,
            DialogueEventSoundBank
        }

        public static GameSoundBankType GetSoundBankType(GameSoundBank soundbank)
        {
            return soundbank switch
            {
                GameSoundBank.Abilities => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.CampaignAdvisor => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.DiplomacyLines => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.EventNarration => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.Magic => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.Movies => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.QuestBattles => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.Rituals => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.UI => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.Vocalisation => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.FrontendMusic => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.CampaignMusic => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.BattleMusic => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.LoadingScreenMusic => GameSoundBankType.ActionEventSoundBank,
                GameSoundBank.FrontendVO => GameSoundBankType.DialogueEventSoundBank,
                GameSoundBank.CampaignVO => GameSoundBankType.DialogueEventSoundBank,
                GameSoundBank.CampaignConversationalVO => GameSoundBankType.DialogueEventSoundBank,
                GameSoundBank.BattleVO => GameSoundBankType.DialogueEventSoundBank,
                GameSoundBank.BattleConversationalVO => GameSoundBankType.DialogueEventSoundBank,
                GameSoundBank.BattleIndividualMelee => GameSoundBankType.DialogueEventSoundBank
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

        public static string GetDisplayString(GameSoundBank soundbank)
        {
            return soundbank switch
            {
                GameSoundBank.Abilities => AbilitiesDisplayString,
                GameSoundBank.CampaignAdvisor => CampaignAdvisorDisplayString,
                GameSoundBank.DiplomacyLines => DiplomacyLinesDisplayString,
                GameSoundBank.EventNarration => EventNarrationDisplayString,
                GameSoundBank.Magic => MagicDisplayString,
                GameSoundBank.Movies => MoviesDisplayString,
                GameSoundBank.QuestBattles => QuestBattlesDisplayString,
                GameSoundBank.Rituals => RitualsDisplayString,
                GameSoundBank.UI => UIDisplayString,
                GameSoundBank.Vocalisation => VocalisationDisplayString,
                GameSoundBank.FrontendMusic => FrontendMusicDisplayString,
                GameSoundBank.CampaignMusic => CampaignMusicDisplayString,
                GameSoundBank.BattleMusic => BattleMusicDisplayString,
                GameSoundBank.LoadingScreenMusic => LoadingScreenMusicDisplayString,
                GameSoundBank.FrontendVO => FrontendVODisplayString,
                GameSoundBank.CampaignVO => CampaignVODisplayString,
                GameSoundBank.CampaignConversationalVO => CampaignConversationalVODisplayString,
                GameSoundBank.BattleVO => BattleVODisplayString,
                GameSoundBank.BattleConversationalVO => BattleConversationalVODisplayString,
                GameSoundBank.BattleIndividualMelee => BattleIndividualMeleeDisplayString,
            };
        }

        public static GameSoundBank GetSoundBank(string soundBankString)
        {
            return soundBankString switch
            {
                AbilitiesDisplayString => GameSoundBank.Abilities,
                CampaignAdvisorDisplayString => GameSoundBank.CampaignAdvisor,
                DiplomacyLinesDisplayString => GameSoundBank.DiplomacyLines,
                EventNarrationDisplayString => GameSoundBank.EventNarration,
                MagicDisplayString => GameSoundBank.Magic,
                MoviesDisplayString => GameSoundBank.Movies,
                QuestBattlesDisplayString => GameSoundBank.QuestBattles,
                RitualsDisplayString => GameSoundBank.Rituals,
                UIDisplayString => GameSoundBank.UI,
                VocalisationDisplayString => GameSoundBank.Vocalisation,
                FrontendMusicDisplayString => GameSoundBank.FrontendMusic,
                CampaignMusicDisplayString => GameSoundBank.CampaignMusic,
                BattleMusicDisplayString => GameSoundBank.BattleMusic,
                LoadingScreenMusicDisplayString => GameSoundBank.LoadingScreenMusic,
                FrontendVODisplayString => GameSoundBank.FrontendVO,
                CampaignVODisplayString => GameSoundBank.CampaignVO,
                CampaignConversationalVODisplayString => GameSoundBank.CampaignConversationalVO,
                BattleVODisplayString => GameSoundBank.BattleVO,
                BattleConversationalVODisplayString => GameSoundBank.BattleConversationalVO,
                BattleIndividualMeleeDisplayString => GameSoundBank.BattleIndividualMelee,
            };
        }
    }
}
