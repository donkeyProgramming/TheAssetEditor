using System.Collections.Generic;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService
{
    public interface IWwiseIDService
    {
        Dictionary<Wh3SoundBank, uint> ActorMixerIds { get; set; }
        Dictionary<Wh3SoundBank, uint> AttenuationIds { get; set; }
    }
}
