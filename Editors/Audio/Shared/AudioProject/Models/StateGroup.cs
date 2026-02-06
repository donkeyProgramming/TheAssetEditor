using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class StateGroup : AudioProjectItem
    {
        public List<State> States { get; set; }

        public static StateGroup CreateForStatePath(string name)
        {
            return new StateGroup
            {
                Id = WwiseHash.Compute(name),
                Name = name
            };
        }

        public static StateGroup CreateForAudioProjectFile(string name, List<State> states)
        {
            return new StateGroup
            {
                Id = WwiseHash.Compute(name),
                Name = name,
                States = states.ToList()
            };
        }

        public StateGroup Clean()
        {
            if (States.Count == 0)
                return null;

            return CreateForAudioProjectFile(Name, States);
        }

        public State GetState(string stateName) => States.FirstOrDefault(state => state.Name == stateName);
    }

    public static class StateGroupListExtensions
    {
        public static void TryAdd(this List<StateGroup> existingStateGroups, StateGroup stateGroup)
        {
            ArgumentNullException.ThrowIfNull(existingStateGroups);
            ArgumentNullException.ThrowIfNull(stateGroup);

            if (existingStateGroups.Any(existingStateGroup => existingStateGroup.Id == stateGroup.Id))
                throw new ArgumentException($"Cannot add StateGroup with Id {stateGroup.Id} as it already exists.");

            var index = existingStateGroups.BinarySearch(stateGroup, AudioProjectItem.IdComparer);
            if (index < 0)
                index = ~index;

            existingStateGroups.Insert(index, stateGroup);
        }
    }
}
