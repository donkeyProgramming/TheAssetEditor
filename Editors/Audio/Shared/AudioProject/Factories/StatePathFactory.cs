using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Factories
{
    public record StatePathFactoryResult
    {
        public StatePath StatePath { get; set; }
        public Sound SoundTarget { get; set; }
        public RandomSequenceContainer RandomSequenceContainerTarget { get; set; }
        public List<Sound> RandomSequenceContainerSounds { get; set; } = [];
    }

    public interface IStatePathFactory
    {
        StatePathFactoryResult Create(
            Dictionary<string, string> stateLookupByStateGroup,
            List<AudioFile> audioFiles,
            HircSettings hircSettings,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            string language,
            uint actorMixerId = 0);
    }

    public class StatePathFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IStatePathFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public StatePathFactoryResult Create(
            Dictionary<string, string> stateLookupByStateGroup, 
            List<AudioFile> audioFiles, 
            HircSettings hircSettings,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            string language,
            uint actorMixerId = 0)
        {
            var statePathFactoryResult = new StatePathFactoryResult();
            var statePathNodes = new List<StatePath.Node>();

            foreach (var kvp in stateLookupByStateGroup)
            {
                var stateGroupName = kvp.Key;
                var stateGroup = StateGroup.Create(stateGroupName);

                var stateName = kvp.Value;
                var state = State.Create(stateName);

                var statePathNode = StatePath.Node.Create(stateGroup, state);
                statePathNodes.Add(statePathNode);
            }

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFiles[0], hircSettings, language, directParentId: actorMixerId);
                statePathFactoryResult.SoundTarget = sound;

                var statePath = StatePath.Create(statePathNodes, sound.Id, AkBkHircType.Sound);
                statePathFactoryResult.StatePath = statePath;
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainerResult = _randomSequenceContainerFactory.Create(usedHircIds, usedSourceIds, hircSettings, audioFiles, language, directParentId: actorMixerId);
                statePathFactoryResult.RandomSequenceContainerTarget = randomSequenceContainerResult.RandomSequenceContainer;
                statePathFactoryResult.RandomSequenceContainerSounds.AddRange(randomSequenceContainerResult.RandomSequenceContainerSounds);

                var statePath = StatePath.Create(statePathNodes, statePathFactoryResult.RandomSequenceContainerTarget.Id, AkBkHircType.RandomSequenceContainer);
                statePathFactoryResult.StatePath = statePath;
            }

            return statePathFactoryResult;
        }
    }
}
