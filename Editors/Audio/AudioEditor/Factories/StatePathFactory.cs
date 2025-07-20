using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IStatePathFactory
    {
        StatePath Create(Dictionary<string, string> stateLookupByStateGroup, List<AudioFile> audioFiles, AudioSettings audioSettings);
    }

    public class StatePathFactory : IStatePathFactory
    {
        private readonly ISoundFactory _soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory;

        public StatePathFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory)
        {
            _soundFactory = soundFactory;
            _randomSequenceContainerFactory = randomSequenceContainerFactory;
        }

        public StatePath Create(Dictionary<string, string> stateLookupByStateGroup, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var statePath = new StatePath();
            var statePathNodes = new List<StatePathNode>();

            foreach (var kvp in stateLookupByStateGroup)
            {
                var stateGroupName = kvp.Key;
                var stateGroup = StateGroup.Create(stateGroupName);

                var stateName = kvp.Value;
                var state = State.Create(stateName);

                var statePathNode = StatePathNode.Create(stateGroup, state);
                statePathNodes.Add(statePathNode);
            }

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(audioFiles[0], audioSettings);
                statePath = StatePath.Create(statePathNodes, sound);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(audioSettings, audioFiles); 
                statePath = StatePath.Create(statePathNodes, randomSequenceContainer);
            }
            return statePath;
        }
    }
}
