using System;
using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class RandomSequenceContainer : AudioProjectItem
    {
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<uint> SoundReferences { get; set; }

        public RandomSequenceContainer()
        {
            HircType = AkBkHircType.RandomSequenceContainer;
        }

        public static RandomSequenceContainer Create(Guid guid, uint id, AudioSettings audioSettings, List<uint> soundReferences, uint overrideBusId = 0, uint directParentId = 0)
        {
            return new RandomSequenceContainer
            {
                Guid = guid,
                Id = id,
                OverrideBusId = overrideBusId,
                DirectParentId = directParentId,
                AudioSettings = audioSettings,
                SoundReferences = soundReferences
            };
        }
    }
}
