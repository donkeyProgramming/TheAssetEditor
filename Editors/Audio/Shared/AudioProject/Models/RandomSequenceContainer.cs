using System;
using System.Collections.Generic;
using System.Linq;
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

    public static class RandomSequenceContainerListExtensions
    {
        public static void TryAdd(this List<RandomSequenceContainer> randomSequenceContainers, RandomSequenceContainer randomSequenceContainer)
        {
            ArgumentNullException.ThrowIfNull(randomSequenceContainers);
            ArgumentNullException.ThrowIfNull(randomSequenceContainer);

            if (randomSequenceContainers.Any(x => x.Id == randomSequenceContainer.Id))
                throw new ArgumentException($"Cannot add RandomSequenceContainer with Id {randomSequenceContainer.Id} as it already exists.");

            var i = randomSequenceContainers.BinarySearch(randomSequenceContainer, AudioProjectItem.IdComparer);
            if (i < 0)
                i = ~i;

            randomSequenceContainers.Insert(i, randomSequenceContainer);
        }
    }
}
