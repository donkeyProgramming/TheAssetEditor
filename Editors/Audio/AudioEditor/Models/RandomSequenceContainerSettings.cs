using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.AudioEditor.Models
{
    public class RandomSequenceContainerSettings : ISettings
    {
        public PlaylistType PlaylistType { get; set; }
        public bool EnableRepetitionInterval { get; set; }
        public uint RepetitionInterval { get; set; }
        public EndBehaviour EndBehaviour { get; set; }
        public bool AlwaysResetPlaylist { get; set; }
        public PlaylistMode PlaylistMode { get; set; }
        public LoopingType LoopingType { get; set; }
        public uint NumberOfLoops { get; set; }
        public TransitionType TransitionType { get; set; }
        public decimal TransitionDuration { get; set; }
    }
}
