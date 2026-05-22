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
            List<KeyValuePair<string, string>> statePathList,
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
            List<KeyValuePair<string, string>> statePathList, 
            List<AudioFile> audioFiles, 
            HircSettings hircSettings,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            string language,
            uint actorMixerId = 0)
        {
            var statePathFactoryResult = new StatePathFactoryResult();
            var statePathNodes = new List<StatePath.Node>();

            foreach (var kvp in statePathList)
            {
                var stateGroupName = kvp.Key;
                var stateGroup = StateGroup.CreateForStatePath(stateGroupName);

                var stateName = kvp.Value;
                var state = new State(stateName);

                var statePathNode = new StatePath.Node(stateGroup, state);
                statePathNodes.Add(statePathNode);
            }

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.CreateTargetSound(usedHircIds, usedSourceIds, audioFiles[0], hircSettings, language, directParentId: actorMixerId);
                statePathFactoryResult.SoundTarget = sound;

                var statePath = new StatePath(statePathNodes, sound.Id, AkBkHircType.Sound);
                statePathFactoryResult.StatePath = statePath;
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainerResult = _randomSequenceContainerFactory.Create(usedHircIds, usedSourceIds, hircSettings, audioFiles, language, directParentId: actorMixerId);
                statePathFactoryResult.RandomSequenceContainerTarget = randomSequenceContainerResult.RandomSequenceContainer;
                statePathFactoryResult.RandomSequenceContainerSounds.AddRange(randomSequenceContainerResult.RandomSequenceContainerSounds);

                var statePath = new StatePath(statePathNodes, statePathFactoryResult.RandomSequenceContainerTarget.Id, AkBkHircType.RandomSequenceContainer);
                statePathFactoryResult.StatePath = statePath;
            }

            return statePathFactoryResult;
        }
    }
}
