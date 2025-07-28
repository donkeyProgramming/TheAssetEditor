using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IRandomSequenceContainerFactory
    {
        RandomSequenceContainer Create(AudioSettings audioSettings, List<AudioFile> audioFiles);
    }

    public class RandomSequenceContainerFactory(ISoundFactory soundFactory) : IRandomSequenceContainerFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;

        public RandomSequenceContainer Create(AudioSettings audioSettings, List<AudioFile> audioFiles)
        {
            var sounds = new List<Sound>();
            foreach (var audioFile in audioFiles)
            {
                var sound = _soundFactory.Create(audioFile);
                sounds.Add(sound);
            }

            var randomSequenceContainerSettings = AudioSettings.CreateRandomSequenceContainerSettings(audioSettings);
            var randomSequenceContainer = RandomSequenceContainer.Create(randomSequenceContainerSettings, sounds);
            return randomSequenceContainer;
        }
    }
}
