using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.Models
{
    public class State : AudioProjectItem
    {
        public static State Create(string name)
        {
            return new State
            {
                Id = name != "Any" ? WwiseHash.Compute(name) : 0,
                Name = name
            };
        }
    }
}
