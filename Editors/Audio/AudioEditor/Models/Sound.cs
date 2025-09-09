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
        public string WavPackFileName { get; set; }
        public string WavPackFilePath { get; set; }
        public string WemPackFileName { get; set; }
        public string WemPackFilePath { get; set; }
        public string WemDiskFilePath { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }
        public AudioSettings AudioSettings { get; set; }

        public static Sound Create(string fileName, string filePath, AudioSettings audioSettings)
        {
            return new Sound()
            {
                WavPackFileName = fileName,
                WavPackFilePath = filePath,
                AudioSettings = audioSettings
            };
        }

        public static Sound Create(string fileName, string filePath)
        {
            return new Sound()
            {
                WavPackFileName = fileName,
                WavPackFilePath = filePath
            };
        }

        public static Sound Create(AudioFile audioFile)
        {
            return new Sound()
            {
                WavPackFileName = audioFile.FileName,
                WavPackFilePath = audioFile.FilePath,
            };
        }

        public string GetAsString()
        {
            var audioSettings = AudioSettings?.GetAsString() ?? "null";
            return $"{OverrideBusId}_{DirectParentId}_{audioSettings}";
        }
    }
}
