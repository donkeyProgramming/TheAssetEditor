using Editors.Audio.AudioEditor.AudioProjectData;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService
{
    public interface IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank);
    }
}
