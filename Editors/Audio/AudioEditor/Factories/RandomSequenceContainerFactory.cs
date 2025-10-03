using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioProjectCompiler;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IRandomSequenceContainerFactory
    {
        RandomSequenceContainer Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioSettings audioSettings, List<AudioFile> audioFiles, uint overrideBusId = 0, uint directParentId = 0);
    }

    public class RandomSequenceContainerFactory(ISoundFactory soundFactory) : IRandomSequenceContainerFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;

        public RandomSequenceContainer Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioSettings audioSettings, List<AudioFile> audioFiles, uint overrideBusId = 0, uint actorMixerId = 0)
        {
            var randomSequenceContainerIds = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);

            var sounds = new List<Sound>();
            var playlistOrder = 0;
            foreach (var audioFile in audioFiles)
            {
                playlistOrder++;
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFile, randomSequenceContainerIds.Id, playlistOrder);
                sounds.Add(sound);
            }

            var randomSequenceContainerSettings = AudioSettings.CreateRandomSequenceContainerSettings(audioSettings);
            var randomSequenceContainer = RandomSequenceContainer.Create(randomSequenceContainerIds.Guid, randomSequenceContainerIds.Id, randomSequenceContainerSettings, sounds, overrideBusId, actorMixerId);
            return randomSequenceContainer;
        }
    }
}
