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
        public Dictionary<string, uint> EventMixerIds { get; } = new Dictionary<string, uint>()
        {
            {"campaign advisor", 517250292},
            {"event narration", 517250292},
            {"battle individual vocalisation", 508226369},
            {"diplomacy line", 54848735},
            {"magic", 140075115},
            {"ability", 140075115},
            {"movie", 573597124},
            {"quest battle", 659413513},
            {"ui", 608071769},
        };

        public Dictionary<string, uint> DialogueEventMixerIds { get; } = new Dictionary<string, uint>()
        {
            {"battle_vo_conversational", 600762068},
            {"battle_vo_orders", 1009314120},
            {"campaign_vo_conversational", 652491101},
            {"campaign_vo", 306196174},
            {"frontend_vo", 745637913}
        };

        public Dictionary<string, uint> AttenuationIds { get; } = new Dictionary<string, uint>()
        {
            {"battle_vo_conversational_attenuation", 649943956},
            {"battle_vo_orders_attenuation", 803409642},
            {"campaign_vo_conversational_attenuation", 62329658},
            {"campaign_vo_attenuation", 432982952},
        };
    }

    internal static class CompilerConstants
    {
        public static readonly string Game_Warhammer3 = "Warhammer3";
    }
}
