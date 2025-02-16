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

        public Dictionary<Wh3SoundBankSubType, uint> ActorMixerIds { get; set; } = new()
        {
            {Wh3SoundBankSubType.Abilities, 140075115},
            {Wh3SoundBankSubType.CampaignAdvisor, 517250292},
            {Wh3SoundBankSubType.DiplomacyLines, 54848735},
            {Wh3SoundBankSubType.EventNarration, 517250292},
            {Wh3SoundBankSubType.Magic, 140075115},
            {Wh3SoundBankSubType.Movies, 573597124},
            {Wh3SoundBankSubType.QuestBattles, 659413513},
            {Wh3SoundBankSubType.FrontendVO, 745637913},
            {Wh3SoundBankSubType.CampaignVO, 306196174},
            {Wh3SoundBankSubType.CampaignConversationalVO, 652491101},
            {Wh3SoundBankSubType.BattleVO, 1009314120},
            {Wh3SoundBankSubType.BattleConversationalVO, 600762068}
        };

        public Dictionary<Wh3SoundBankSubType, uint> AttenuationIds { get; set; } = new()
        {
            {Wh3SoundBankSubType.Abilities, 588111330},
            {Wh3SoundBankSubType.Magic, 588111330},
            {Wh3SoundBankSubType.QuestBattles, 6016245},
            {Wh3SoundBankSubType.CampaignVO, 432982952},
            {Wh3SoundBankSubType.CampaignConversationalVO, 62329658},
            {Wh3SoundBankSubType.BattleVO, 803409642},
            {Wh3SoundBankSubType.BattleConversationalVO, 649943956},
        };
    }

    internal static class CompilerConstants
    {
        public static readonly string GameWarhammer3 = "Warhammer3";
        public static readonly string ActionType = "Play";
        public static readonly ushort UWeight = 50; // Always 50 in WH3 Dialogue Events.
        public static readonly ushort UProbability = 100; // Always 100 in WH3 Dialogue Events.
    }
}
