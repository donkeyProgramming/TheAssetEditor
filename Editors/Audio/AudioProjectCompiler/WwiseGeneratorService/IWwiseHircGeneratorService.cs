using Editors.Audio.AudioEditor.Data;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService
{
    public interface IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank);
    }
}
