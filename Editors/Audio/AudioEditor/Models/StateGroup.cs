using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioEditor.Models
{
    public class StateGroup : AudioProjectItem
    {
        public List<State> States { get; set; }

        public static StateGroup Create(string name)
        {
            return new StateGroup
            {
                Name = name
            };
        }

        public State GetState(string stateName)
        {
            return States.FirstOrDefault(state => state.Name == stateName);
        }

        public void InsertAlphabetically(State state) => InsertAlphabeticallyUnique(States, state);
    }
}
