using System.Collections.Generic;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioProjectCompiler.WwiseIdService
{
    public interface IWwiseIddService
    {
        Dictionary<Wh3SoundBankSubtype, uint> ActorMixerIds { get; }
        Dictionary<Wh3SoundBankSubtype, uint> OverrideBusIds { get; }
    }
}
