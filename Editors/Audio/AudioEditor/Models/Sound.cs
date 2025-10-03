using System;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class Sound : AudioProjectItem
    {
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public uint SourceId { get; set; }
        public int PlaylistOrder { get; set; }
        public string WavPackFileName { get; set; }
        public string WavPackFilePath { get; set; }
        public string WemPackFileName { get; set; }
        public string WemPackFilePath { get; set; }
        public string WemDiskFilePath { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }
        public AudioSettings AudioSettings { get; set; }

        public Sound()
        {
            HircType = AkBkHircType.Sound;
        }

        public static Sound Create(Guid guid, uint id, uint overrideBusId, uint directParentId, uint sourceId, string fileName, string filePath, AudioSettings audioSettings)
        {
            return new Sound()
            {
                Guid = guid,
                Id = id,
                OverrideBusId = overrideBusId,
                DirectParentId = directParentId,
                SourceId = sourceId,
                WavPackFileName = fileName,
                WavPackFilePath = filePath,
                AudioSettings = audioSettings
            };
        }

        public static Sound Create(Guid guid, uint id, uint directParentId, uint sourceId, int playlistOrder, string fileName, string filePath)
        {
            return new Sound()
            {
                Guid = guid,
                Id = id,
                DirectParentId = directParentId,
                SourceId = sourceId,
                PlaylistOrder = playlistOrder,
                WavPackFileName = fileName,
                WavPackFilePath = filePath
            };
        }
    }
}
