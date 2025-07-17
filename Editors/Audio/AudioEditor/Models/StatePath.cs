using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Models
{
    public class StatePath
    {
        public List<StatePathNode> Nodes { get; set; } = [];
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public Sound Sound { get; set; }
    }
}
