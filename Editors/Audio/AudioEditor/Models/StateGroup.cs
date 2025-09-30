using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.Models
{
    public class StateGroup : AudioProjectItem
    {
        public List<State> States { get; set; }

        public static StateGroup Create(string name)
        {
            return new StateGroup
            {
                Id = WwiseHash.Compute(name),
                Name = name
            };
        }

        public static StateGroup Create(string name, List<State> states)
        {
            return new StateGroup
            {
                Id = WwiseHash.Compute(name),
                Name = name,
                States = states
            };
        }

        public State GetState(string stateName)
        {
            return States.FirstOrDefault(state => state.Name == stateName);
        }

        public void InsertAlphabetically(State state) => InsertAlphabeticallyUnique(States, state);
    }
}
