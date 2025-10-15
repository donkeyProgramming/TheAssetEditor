using System.Collections.Generic;
using System.Linq;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3ActionEventType;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3SoundBank;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    // ActionEventType is a representation of the different types of Action Events within a SoundBank.
    // For example in the battle_individual_magic__core.bnk there are Action Events for abilities,
    // animation meta ability sounds, spells etc., and within spells for example some lores use
    // different top level Actor Mixers but are all in the same SoundBank so we split them into types
    // so we can determine what Actor Mixer to use when compiling.
    public enum Wh3ActionEventType
    {
        BattleAbilities,
        BattleAdvice,
        BattleMagic,
        CampaignAdvice,
        EventDilemmaNarration,
        DiplomacyLines,
        Movies,
        Music,
        QuestBattleSpeeches
    }

    public record Wh3ActionEventDefinition(
        string Name,
        Wh3ActionEventType ActionEventType,
        Wh3SoundBank SoundBank,
        uint ActorMixerId = 0,
        uint OverrideBusId = 0);

    public static class Wh3ActionEventInformation
    {
        // To find Actor Mixer IDs click on a sound in the Audio Explorer and refer to the lowest ActorMixer in the Graph (the top level Actor Mixer)
        public static List<Wh3ActionEventDefinition> Information { get; } =
        [
            // Reference: Play_wh3_prologue_battle_advice_lords_1
            new("Battle Advice", Wh3ActionEventType.BattleAdvice, Wh3SoundBank.BattleAdvice, ActorMixerId: 142435894),

            // Reference: Play_wh2_dlc13_camp_advice_emp_emperors_mandate_001_1
            new("Battle Advice", Wh3ActionEventType.CampaignAdvice, Wh3SoundBank.CampaignAdvice, ActorMixerId: 517250292),

            // Reference: Battle_Individual_Ability_Storm_Dragon_To_Human_Form_Transform
            new("Battle Abilities", BattleAbilities, BattleIndividualMagic, ActorMixerId: 140075115),

            // Reference: Battle_IND_Magic_Light_Banishment_Start
            new("Battle Magic", BattleMagic, BattleIndividualMagic, ActorMixerId: 645285343),

            // Reference: Play_cat_miao_ying_dip_pos_gen_01
            new("Diplomacy Lines", DiplomacyLines, CampaignDiplomacy, ActorMixerId: 54848735),

            // Reference: Play_gorbad_cam_da_plan_yoreeka
            new("Event / Dilemma Narration", EventDilemmaNarration, CampaignVOConversational, ActorMixerId: 306196174),

            // Reference: Play_Movie_warhammer3_belakor_1
            new("Movies", Movies, GlobalMovies, ActorMixerId: 573597124),

            // Reference: Global_Music_Play
            // Strangely the second from top bus (3356399930) works but the top level bus (3267614108) doesn't and neither does the third level bus (4042387584)
            new("Music", Music, GlobalMusic, OverrideBusId: 3356399930),

            // Reference: Play_EMP_KF_GS_Qbattle_silver_seal_pt_01
            new("Quest Battle Speech", QuestBattleSpeeches, BattleVOGeneralsSpeech, ActorMixerId: 659413513)

            //new(Wh3SoundBankSubtype.Rituals, "rituals", "Rituals", Wh3SoundBankEventType.ActionEvent),
            //new(Wh3SoundBankSubtype.UI, "ui", "UI", Wh3SoundBankEventType.ActionEvent),
            //new(Wh3SoundBankSubtype.Vocalisation, "vocalisation", "Vocalisation", Wh3SoundBankEventType.ActionEvent),
        ];

        public static Wh3ActionEventType GetActionEventType(string name) => Information.First(definition => definition.Name == name).ActionEventType;

        public static string GetName(Wh3ActionEventType actionEventType) => Information.First(definition => definition.ActionEventType == actionEventType).Name;

        public static Wh3SoundBank GetSoundBank(string name) => Information.First(definition => definition.Name == name).SoundBank;

        public static uint GetActorMixerId(Wh3ActionEventType actionEventType) => Information.First(definition => definition.ActionEventType == actionEventType).ActorMixerId;
        
        public static uint GetOverrideBusId(Wh3ActionEventType actionEventType) => Information.First(definition => definition.ActionEventType == actionEventType).OverrideBusId;

        public static List<Wh3ActionEventType> GetSoundBankActionEventTypes(Wh3SoundBank soundBank)
        {
            return Information
                .Where(definition => definition.SoundBank == soundBank)
                .Select(definition => definition.ActionEventType)
                .Distinct()
                .ToList();
        }

        public static bool Contains(Wh3SoundBank soundBank) => Information.Any(definition => definition.SoundBank == soundBank);
    }
}
