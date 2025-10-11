using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler;

namespace Editors.Audio.AudioEditor.Factories
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
            AudioSettings audioSettings,
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
            AudioSettings audioSettings,
            List<AudioFile> audioFiles,
            string language,
            uint overrideBusId = 0,
            uint actorMixerId = 0)
        {
            var result = new RandomSequenceContainerFactoryResult();
            var randomSequenceContainerIds = IdGenerator.GenerateIds(usedHircIds);

            var soundReferences = new List<uint>();
            var playlistOrder = 0;
            foreach (var audioFile in audioFiles)
            {
                playlistOrder++;
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFile, randomSequenceContainerIds.Id, playlistOrder, language);
                soundReferences.Add(sound.Id);
                result.RandomSequenceContainerSounds.Add(sound);
            }

            var randomSequenceContainerSettings = AudioSettings.CreateRandomSequenceContainerSettings(audioSettings);
            var randomSequenceContainer = RandomSequenceContainer.Create(
                randomSequenceContainerIds.Guid,
                randomSequenceContainerIds.Id,
                randomSequenceContainerSettings,
                soundReferences,
                overrideBusId,
                actorMixerId);
            result.RandomSequenceContainer = randomSequenceContainer;
            return result;
        }
    }
}
