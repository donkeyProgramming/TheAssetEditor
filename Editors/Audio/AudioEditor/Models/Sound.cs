using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class Sound : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.Sound;
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public uint SourceId { get; set; }
        public string WavFileName { get; set; }
        public string WavFilePath { get; set; }
        public string WemFileName { get; set; }
        public string WemFilePath { get; set; }
        public string WemDiskFilePath { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }
        public SoundSettings Settings { get; set; }
    }
}
