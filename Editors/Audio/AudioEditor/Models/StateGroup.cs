using System;
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

        public void InsertAlphabetically(State state)
        {
            if (state == null) 
                return;

            States ??= [];

            var index = States.BinarySearch(state, StateNameComparer.Instance);
            if (index >= 0) 
                return;

            States.Insert(~index, state);
        }

        private sealed class StateNameComparer : IComparer<State>
        {
            public static readonly StateNameComparer Instance = new();

            public int Compare(State left, State right)
            {
                var leftName = left?.Name ?? string.Empty;
                var rightName = right?.Name ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }
    }
}
