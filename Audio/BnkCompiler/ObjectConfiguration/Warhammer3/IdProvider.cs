using System.Collections.Generic;

namespace Audio.BnkCompiler.ObjectConfiguration.Warhammer3
{
    public interface IVanillaObjectIds
    {
        Dictionary<string, uint> EventMixerIds { get; }
        Dictionary<string, uint> DialogueEventMixerIds { get; }
        Dictionary<string, uint> AttenuationIds { get; }

    }

    public class IdProvider : IVanillaObjectIds
    {
        /*
        How to find Ids:

            Finding Ids for mixer objects can be done by using the Audio Explorer and referring to the lowest ActorMixer in the Graph Structure of a given sound (top level mixer).

            Finding Ids for attenuation objects involves clicking on the sound of an event in the Audio Explorer to find the parent bnk and the top level mixer. 
            Then extract the parent bnk to your PC, load it with Wwiser, then click Dump Bnk.
            Open the Xml file that Dump Bnk created and ctrl+f for the mixer Id. Find the Actor Mixer object and look through its settings to see if there's any attenuation - if so then that's the attenuation Id.
        */

        // Top level mixers for Events
        public Dictionary<string, uint> EventMixerIds { get; } = new Dictionary<string, uint>()
        {
            // {"Type of Event", Mixer Id}
            {"ability", 140075115},
            {"advisor", 517250292},
            {"diplomacy_line", 54848735},
            {"event_narration", 517250292},
            {"magic", 140075115},
            {"movie", 573597124},
            {"quest_battle", 659413513},
            {"ui", 608071769},
            {"vocalisation", 508226369},
        };

        // Top level mixers for Dialogue Events
        public Dictionary<string, uint> DialogueEventMixerIds { get; } = new Dictionary<string, uint>()
        {
            // {"Type of Dialogue Event", Mixer Id}
            {"battle_vo_conversational", 600762068},
            {"battle_vo_orders", 1009314120},
            {"campaign_vo_conversational", 652491101},
            {"campaign_vo", 306196174},
            {"frontend_vo", 745637913}
        };

        // Attenuation IDs used for the top level mixers of dialogue events. This essentially pairs up a key from DialogueEventMixerIds or EventMixerIds with "_attenuation" and associates this with an attenuation object Id.
        public Dictionary<string, uint> AttenuationIds { get; } = new Dictionary<string, uint>()
        {
            // {"Type of attenuation", Attenuation Id}
            // Dialogue Event Attenuation
            {"battle_vo_conversational_attenuation", 649943956},
            {"battle_vo_orders_attenuation", 803409642},
            {"campaign_vo_conversational_attenuation", 62329658},
            {"campaign_vo_attenuation", 432982952},
            // Event Attenuation
            {"ability_attenuation", 588111330},
            {"magic_attenuation", 588111330},
            {"quest_battle_attenuation", 6016245},
            {"vocalisation_attenuation", 987979793}
        };
    }

    internal static class CompilerConstants
    {
        public static readonly string Game_Warhammer3 = "Warhammer3";
    }
}
