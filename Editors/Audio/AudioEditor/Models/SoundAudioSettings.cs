using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.AudioEditor.Models
{
    public class SoundSettings : ISettings
    {
        public LoopingType LoopingType { get; set; }
        public uint NumberOfLoops { get; set; }
    }
}
