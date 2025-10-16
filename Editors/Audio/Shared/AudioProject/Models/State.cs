using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
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

    public static class StateListExtensions
    {
        private static readonly IComparer<State> s_nameComparerIgnoreCase = new NameComparer();

        private sealed class NameComparer : IComparer<State>
        {
            public int Compare(State left, State right)
            {
                var leftName = left?.Name ?? string.Empty;
                var rightName = right?.Name ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }

        public static void InsertAlphabetically(this List<State> states, State state)
        {
            ArgumentNullException.ThrowIfNull(states);
            ArgumentNullException.ThrowIfNull(state);

            if (states.Any(state => StringComparer.OrdinalIgnoreCase.Equals(state?.Name ?? string.Empty, state.Name ?? string.Empty)))
                throw new ArgumentException($"Cannot add State with Name {state.Name} as it already exists.");

            var index = states.BinarySearch(state, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            states.Insert(index, state);
        }
    }
}
