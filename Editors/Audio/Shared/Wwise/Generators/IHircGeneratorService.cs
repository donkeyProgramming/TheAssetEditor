using Editors.Audio.Shared.AudioProject.Models;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.Generators
{
    public interface IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null);
    }
}
