using System.Collections.Generic;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioProjectCompiler.WwiseIdService.Warhammer3
{
    // TODO: Add missing Ids
    public class Wh3WwiseIdService : IWwiseIdService
    {
        // Finding Actor Mixer Ids:
        // Click on a sound in the Audio Explorer and refer to the lowest ActorMixer in the Graph Structure of a given sound (top level mixer).
        public Dictionary<Wh3SoundBankSubtype, uint> ActorMixerIds { get; } = new()
        {
            //{Wh3SoundBankSubtype.Abilities, 140075115},
            //{Wh3SoundBankSubtype.CampaignAdvisor, 517250292},
            {Wh3SoundBankSubtype.DiplomacyLines, 54848735},                 // Reference: Play_cat_miao_ying_dip_pos_gen_01
            //{Wh3SoundBankSubtype.EventNarration, 517250292},
            //{Wh3SoundBankSubtype.Magic, 140075115},
            {Wh3SoundBankSubtype.Movies, 573597124},                        // Reference: Play_Movie_warhammer3_belakor_1
            {Wh3SoundBankSubtype.QuestBattles, 659413513},                  // Reference: Play_EMP_KF_GS_Qbattle_silver_seal_pt_01
            {Wh3SoundBankSubtype.FrontendVO, 745637913},                    // Reference: frontend_vo_character_select
            {Wh3SoundBankSubtype.CampaignVO, 306196174},                    // Reference: campaign_vo_attack
            {Wh3SoundBankSubtype.CampaignConversationalVO, 652491101},      // Reference: campaign_vo_cs_proximity
            {Wh3SoundBankSubtype.BattleVO, 1009314120},                     // Reference: battle_vo_orders_attack
            {Wh3SoundBankSubtype.BattleConversationalVO, 600762068},        // Reference: battle_vo_conversation_clash
        };

        public Dictionary<Wh3SoundBankSubtype, uint> OverrideBusIds { get; } = new()
        {
            {Wh3SoundBankSubtype.FrontendMusic, 3356399930} // 3356399930, which is the second from top bus, works. 3267614108 which is the top level bus doesn't work and neither does 4042387584.
        };
    }
}
