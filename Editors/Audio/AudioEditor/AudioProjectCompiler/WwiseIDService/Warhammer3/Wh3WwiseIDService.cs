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

        public Dictionary<Wh3SoundBank, uint> ActorMixerIds { get; set; } = new()
        {
            {Wh3SoundBank.Abilities, 140075115},
            {Wh3SoundBank.CampaignAdvisor, 517250292},
            {Wh3SoundBank.DiplomacyLines, 54848735},
            {Wh3SoundBank.EventNarration, 517250292},
            {Wh3SoundBank.Magic, 140075115},
            {Wh3SoundBank.Movies, 573597124},
            {Wh3SoundBank.QuestBattles, 659413513},
            {Wh3SoundBank.FrontendVO, 745637913},
            {Wh3SoundBank.CampaignVO, 306196174},
            {Wh3SoundBank.CampaignConversationalVO, 652491101},
            {Wh3SoundBank.BattleVO, 1009314120},
            {Wh3SoundBank.BattleConversationalVO, 600762068}
        };

        public Dictionary<Wh3SoundBank, uint> AttenuationIds { get; set; } = new()
        {
            {Wh3SoundBank.Abilities, 588111330},
            {Wh3SoundBank.Magic, 588111330},
            {Wh3SoundBank.QuestBattles, 6016245},
            {Wh3SoundBank.CampaignVO, 432982952},
            {Wh3SoundBank.CampaignConversationalVO, 62329658},
            {Wh3SoundBank.BattleVO, 803409642},
            {Wh3SoundBank.BattleConversationalVO, 649943956},
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
