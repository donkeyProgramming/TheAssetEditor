using Editors.Audio.AudioEditor.Models;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService
{
    public interface IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null);
    }
}
