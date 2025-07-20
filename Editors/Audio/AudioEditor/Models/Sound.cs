using Editors.Audio.AudioEditor.Settings;
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
        public AudioSettings AudioSettings { get; set; }

        public static Sound Create(string fileName, string filePath, AudioSettings audioSettings)
        {
            return new Sound()
            {
                WavFileName = fileName,
                WavFilePath = filePath,
                AudioSettings = audioSettings
            };
        }

        public static Sound Create(string fileName, string filePath)
        {
            return new Sound()
            {
                WavFileName = fileName,
                WavFilePath = filePath
            };
        }

        public static Sound Create(AudioFile audioFile)
        {
            return new Sound()
            {
                WavFileName = audioFile.FileName,
                WavFilePath = audioFile.FilePath,
            };
        }
    }
}
