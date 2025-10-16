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

        public static void InsertAlphabetically(this List<State> existingStates, State state)
        {
            ArgumentNullException.ThrowIfNull(existingStates);
            ArgumentNullException.ThrowIfNull(state);

            if (existingStates.Any(existingState => StringComparer.OrdinalIgnoreCase.Equals(existingState.Name, state.Name)))
                throw new ArgumentException($"Cannot add State with Name {state.Name} as it already exists.");

            var index = existingStates.BinarySearch(state, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            existingStates.Insert(index, state);
        }
    }
}
