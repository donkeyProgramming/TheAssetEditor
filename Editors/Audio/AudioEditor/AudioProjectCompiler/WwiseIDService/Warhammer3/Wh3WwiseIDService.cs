using System.Collections.Generic;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService.Warhammer3
{
    // TODO: Add missing IDs

    public class Wh3WwiseIDService : IWwiseIDService
    {
        // Finding Actor Mixer IDs:
        // Click on a sound in the Audio Explorer and refer to the lowest ActorMixer in the Graph Structure of a given sound (top level mixer).

        // Finding Attenuation IDs:
        // Click on a sound in the Audio Explorer to find the parent bnk and the top level mixer. 
        // Then extract the parent bnk to your PC, load it with Wwiser, then click Dump Bnk.
        // Open the Xml file that Dump Bnk created and ctrl+f for the mixer Id. Find the Actor Mixer object and look through its settings to see if there's any attenuation - if so then that's the attenuation Id.

        public Dictionary<Wh3SoundBankSubtype, uint> ActorMixerIds { get; } = new()
        {
            //{Wh3SoundBankSubtype.Abilities, 140075115},
            //{Wh3SoundBankSubtype.CampaignAdvisor, 517250292},
            {Wh3SoundBankSubtype.DiplomacyLines, 54848735},                 // Reference: Play_cat_miao_ying_dip_pos_gen_01
            //{Wh3SoundBankSubtype.EventNarration, 517250292},
            //{Wh3SoundBankSubtype.Magic, 140075115},
            //{Wh3SoundBankSubtype.Movies, 573597124},
            //{Wh3SoundBankSubtype.QuestBattles, 659413513},
            {Wh3SoundBankSubtype.FrontendVO, 745637913},
            {Wh3SoundBankSubtype.CampaignVO, 306196174},
            {Wh3SoundBankSubtype.CampaignConversationalVO, 652491101},
            {Wh3SoundBankSubtype.BattleVO, 1009314120},
            {Wh3SoundBankSubtype.BattleConversationalVO, 600762068}
        };

        public Dictionary<Wh3SoundBankSubtype, uint> AttenuationIds { get; } = new()
        {
            //{Wh3SoundBankSubtype.Abilities, 588111330},
            //{Wh3SoundBankSubtype.Magic, 588111330},
            //{Wh3SoundBankSubtype.QuestBattles, 6016245},
            {Wh3SoundBankSubtype.CampaignVO, 432982952},
            {Wh3SoundBankSubtype.CampaignConversationalVO, 62329658},
            {Wh3SoundBankSubtype.BattleVO, 803409642},
            {Wh3SoundBankSubtype.BattleConversationalVO, 649943956},
        };
    }
}
