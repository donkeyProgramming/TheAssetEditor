using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.Shared.AudioProject.Factories
{
    public record RandomSequenceContainerFactoryResult
    {
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public List<Sound> RandomSequenceContainerSounds { get; set; } = [];
    }

    public interface IRandomSequenceContainerFactory
    {
        RandomSequenceContainerFactoryResult Create(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            HircSettings hircSettings,
            List<AudioFile> audioFiles,
            string language,
            uint overrideBusId = 0,
            uint directParentId = 0);
    }

    public class RandomSequenceContainerFactory(ISoundFactory soundFactory) : IRandomSequenceContainerFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;

        public RandomSequenceContainerFactoryResult Create(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            HircSettings hircSettings,
            List<AudioFile> audioFiles,
            string language,
            uint overrideBusId = 0,
            uint actorMixerId = 0)
        {
            var result = new RandomSequenceContainerFactoryResult();
            var randomSequenceContainerIds = IdGenerator.GenerateIds(usedHircIds);

            var children = new List<uint>();
            var playlistOrder = 0;
            foreach (var audioFile in audioFiles)
            {
                playlistOrder++;
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFile, randomSequenceContainerIds.Id, playlistOrder, language);
                children.Add(sound.Id);
                result.RandomSequenceContainerSounds.Add(sound);
            }

            var randomSequenceContainerSettings = HircSettings.CreateRandomSequenceContainerSettings(hircSettings);
            var randomSequenceContainer = RandomSequenceContainer.Create(
                randomSequenceContainerIds.Guid,
                randomSequenceContainerIds.Id,
                randomSequenceContainerSettings,
                children,
                overrideBusId,
                actorMixerId);
            result.RandomSequenceContainer = randomSequenceContainer;
            return result;
        }
    }
}
