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
        public HircSettings HircSettings { get; set; }
        public List<uint> Children { get; set; } = [];

        public RandomSequenceContainer(Guid guid, uint id, uint overrideBusId, uint directParentId, HircSettings hircSettings, List<uint> children)
        {
            Guid = guid;
            Id = id;
            HircType = AkBkHircType.RandomSequenceContainer;
            OverrideBusId = overrideBusId;
            DirectParentId = directParentId;
            HircSettings = hircSettings;
            Children = children;
        }
    }

    public static class RandomSequenceContainerListExtensions
    {
        public static void TryAdd(this List<RandomSequenceContainer> existingRandomSequenceContainers, RandomSequenceContainer randomSequenceContainer)
        {
            ArgumentNullException.ThrowIfNull(existingRandomSequenceContainers);
            ArgumentNullException.ThrowIfNull(randomSequenceContainer);

            if (existingRandomSequenceContainers.Any(existingRandomSequenceContainer => existingRandomSequenceContainer.Id == randomSequenceContainer.Id))
                throw new ArgumentException($"Cannot add RandomSequenceContainer with Id {randomSequenceContainer.Id} as it already exists.");

            var i = existingRandomSequenceContainers.BinarySearch(randomSequenceContainer, AudioProjectItem.IdComparer);
            if (i < 0)
                i = ~i;

            existingRandomSequenceContainers.Insert(i, randomSequenceContainer);
        }
    }
}
