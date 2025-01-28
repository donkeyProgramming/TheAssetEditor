namespace Editors.Audio.GameSettings.Warhammer3
{
    public class SoundBanks
    {
        public enum Wh3SoundBank
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

        public enum Wh3SoundBankType
        {
            ActionEventSoundBank,
            DialogueEventSoundBank
        }

        public static Wh3SoundBankType GetSoundBankType(Wh3SoundBank soundbank)
        {
            return soundbank switch
            {
                Wh3SoundBank.Abilities => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.CampaignAdvisor => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.DiplomacyLines => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.EventNarration => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.Magic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.Movies => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.QuestBattles => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.Rituals => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.UI => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.Vocalisation => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.FrontendMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.CampaignMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.BattleMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.LoadingScreenMusic => Wh3SoundBankType.ActionEventSoundBank,
                Wh3SoundBank.FrontendVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBank.CampaignVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBank.CampaignConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBank.BattleVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBank.BattleConversationalVO => Wh3SoundBankType.DialogueEventSoundBank,
                Wh3SoundBank.BattleIndividualMelee => Wh3SoundBankType.DialogueEventSoundBank
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

        public static string GetSoundBankDisplayString(Wh3SoundBank soundbank)
        {
            return soundbank switch
            {
                Wh3SoundBank.Abilities => AbilitiesDisplayString,
                Wh3SoundBank.CampaignAdvisor => CampaignAdvisorDisplayString,
                Wh3SoundBank.DiplomacyLines => DiplomacyLinesDisplayString,
                Wh3SoundBank.EventNarration => EventNarrationDisplayString,
                Wh3SoundBank.Magic => MagicDisplayString,
                Wh3SoundBank.Movies => MoviesDisplayString,
                Wh3SoundBank.QuestBattles => QuestBattlesDisplayString,
                Wh3SoundBank.Rituals => RitualsDisplayString,
                Wh3SoundBank.UI => UIDisplayString,
                Wh3SoundBank.Vocalisation => VocalisationDisplayString,
                Wh3SoundBank.FrontendMusic => FrontendMusicDisplayString,
                Wh3SoundBank.CampaignMusic => CampaignMusicDisplayString,
                Wh3SoundBank.BattleMusic => BattleMusicDisplayString,
                Wh3SoundBank.LoadingScreenMusic => LoadingScreenMusicDisplayString,
                Wh3SoundBank.FrontendVO => FrontendVODisplayString,
                Wh3SoundBank.CampaignVO => CampaignVODisplayString,
                Wh3SoundBank.CampaignConversationalVO => CampaignConversationalVODisplayString,
                Wh3SoundBank.BattleVO => BattleVODisplayString,
                Wh3SoundBank.BattleConversationalVO => BattleConversationalVODisplayString,
                Wh3SoundBank.BattleIndividualMelee => BattleIndividualMeleeDisplayString,
            };
        }

        public static Wh3SoundBank GetSoundBankEnum(string soundBankString)
        {
            return soundBankString switch
            {
                AbilitiesDisplayString => Wh3SoundBank.Abilities,
                CampaignAdvisorDisplayString => Wh3SoundBank.CampaignAdvisor,
                DiplomacyLinesDisplayString => Wh3SoundBank.DiplomacyLines,
                EventNarrationDisplayString => Wh3SoundBank.EventNarration,
                MagicDisplayString => Wh3SoundBank.Magic,
                MoviesDisplayString => Wh3SoundBank.Movies,
                QuestBattlesDisplayString => Wh3SoundBank.QuestBattles,
                RitualsDisplayString => Wh3SoundBank.Rituals,
                UIDisplayString => Wh3SoundBank.UI,
                VocalisationDisplayString => Wh3SoundBank.Vocalisation,
                FrontendMusicDisplayString => Wh3SoundBank.FrontendMusic,
                CampaignMusicDisplayString => Wh3SoundBank.CampaignMusic,
                BattleMusicDisplayString => Wh3SoundBank.BattleMusic,
                LoadingScreenMusicDisplayString => Wh3SoundBank.LoadingScreenMusic,
                FrontendVODisplayString => Wh3SoundBank.FrontendVO,
                CampaignVODisplayString => Wh3SoundBank.CampaignVO,
                CampaignConversationalVODisplayString => Wh3SoundBank.CampaignConversationalVO,
                BattleVODisplayString => Wh3SoundBank.BattleVO,
                BattleConversationalVODisplayString => Wh3SoundBank.BattleConversationalVO,
                BattleIndividualMeleeDisplayString => Wh3SoundBank.BattleIndividualMelee,
            };
        }
    }
}
