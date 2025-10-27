using System;
using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class Sound : AudioProjectItem
    {
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public uint SourceId { get; set; }
        public int PlaylistOrder { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }
        public HircSettings HircSettings { get; set; }

        public Sound()
        {
            HircType = AkBkHircType.Sound;
        }

        public static Sound Create(Guid guid, uint id, uint overrideBusId, uint directParentId, uint sourceId, string language, HircSettings hircSettings)
        {
            return new Sound()
            {
                Guid = guid,
                Id = id,
                OverrideBusId = overrideBusId,
                DirectParentId = directParentId,
                SourceId = sourceId,
                Language = language,
                HircSettings = hircSettings
            };
        }

        public static Sound Create(Guid guid, uint id, uint directParentId, int playlistOrder, uint sourceId, string language)
        {
            return new Sound()
            {
                Guid = guid,
                Id = id,
                DirectParentId = directParentId,
                SourceId = sourceId,
                PlaylistOrder = playlistOrder,
                Language = language
            };
        }
    }

    public static class SoundListExtensions
    {
        public static void TryAdd(this List<Sound> existingSounds, Sound sound)
        {
            ArgumentNullException.ThrowIfNull(existingSounds);
            ArgumentNullException.ThrowIfNull(sound);

            if (existingSounds.Any(existingSound => existingSound.Id == sound.Id))
                throw new ArgumentException($"Cannot add Sound with Id {sound.Id} as it already exists.");

            var i = existingSounds.BinarySearch(sound, AudioProjectItem.IdComparer);
            if (i < 0)
                i = ~i;

            existingSounds.Insert(i, sound);
        }
    }
}
