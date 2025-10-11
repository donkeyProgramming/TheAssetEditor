using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class DialogueEvent : AudioProjectItem
    {
        public List<StatePath> StatePaths { get; set; } = [];

        public DialogueEvent()
        {
            HircType = AkBkHircType.Dialogue_Event;
        }

        public static DialogueEvent Create(string name)
        {
            return new DialogueEvent
            {
                Id = WwiseHash.Compute(name),
                Name = name
            };
        }

        public StatePath GetStatePath(string statePathName)
        {
            return StatePaths.FirstOrDefault(statePath => statePath.Name.Equals(statePathName));
        }


        public void InsertAlphabetically(StatePath statePath)
        {
            if (statePath == null) 
                return;

            StatePaths ??= [];

            var index = StatePaths.BinarySearch(statePath, StatePathNameComparer.Instance);
            if (index >= 0) 
                return;

            StatePaths.Insert(~index, statePath);
        }

        private sealed class StatePathNameComparer : IComparer<StatePath>
        {
            public static readonly StatePathNameComparer Instance = new();

            public int Compare(StatePath left, StatePath right)
            {
                var leftName = left?.Name ?? string.Empty;
                var rightName = right?.Name ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }
    }
}
