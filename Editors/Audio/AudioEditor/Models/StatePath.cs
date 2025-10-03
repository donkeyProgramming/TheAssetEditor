using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.Models
{
    public class StatePath : AudioProjectItem
    {
        public List<Node> Nodes { get; set; } = [];
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public Sound Sound { get; set; }

        public static StatePath Create(List<Node> nodes, Sound sound)
        {
            return new StatePath
            {
                Name = BuildName(nodes),
                Nodes = nodes,
                Sound = sound
            };
        }

        public static StatePath Create(List<Node> nodes, RandomSequenceContainer randomSequenceContainer)
        {
            return new StatePath
            {
                Name = BuildName(nodes),
                Nodes = nodes,
                RandomSequenceContainer = randomSequenceContainer
            };
        }

        public static string BuildName(List<Node> nodes)
        {
            return string.Join('.', nodes.Select(node => $"[{node.StateGroup.Name}]{node.State.Name}"));
        }

        public AudioSettings GetAudioSettings()
        {
            if (RandomSequenceContainer != null)
                return RandomSequenceContainer.AudioSettings;
            else
                return Sound.AudioSettings;
        }

        public class Node
        {
            public StateGroup StateGroup { get; set; }
            public State State { get; set; }

            public static Node Create(StateGroup stateGroup, State state)
            {
                return new Node
                {
                    StateGroup = stateGroup,
                    State = state
                };
            }
        }
    }
}
