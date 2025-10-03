using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IStatePathFactory
    {
        StatePath Create(
            Dictionary<string, string> stateLookupByStateGroup,
            List<AudioFile> audioFiles,
            AudioSettings audioSettings,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            uint actorMixerId = 0);
    }

    public class StatePathFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IStatePathFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public StatePath Create(
            Dictionary<string, string> stateLookupByStateGroup, 
            List<AudioFile> audioFiles, 
            AudioSettings audioSettings,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            uint actorMixerId = 0)
        {
            var statePath = new StatePath();
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
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFiles[0], audioSettings, directParentId: actorMixerId);
                statePath = StatePath.Create(statePathNodes, sound);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(usedHircIds, usedSourceIds, audioSettings, audioFiles, directParentId: actorMixerId);
                statePath = StatePath.Create(statePathNodes, randomSequenceContainer);
            }

            return statePath;
        }
    }
}
