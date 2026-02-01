using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.Wwise;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
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

        public StatePath GetStatePath(string statePathName) => StatePaths.FirstOrDefault(statePath => statePath.Name.Equals(statePathName));
    }

    public static class DialogueEventListExtensions
    {
        public static void TryAdd(this List<DialogueEvent> existingDialogueEvents, DialogueEvent dialogueEvent)
        {
            ArgumentNullException.ThrowIfNull(existingDialogueEvents);
            ArgumentNullException.ThrowIfNull(dialogueEvent);

            if (existingDialogueEvents.Any(existingDialogueEvent => existingDialogueEvent.Id == dialogueEvent.Id))
                throw new ArgumentException($"Cannot add DialogueEvent with Id {dialogueEvent.Id} as it already exists.");

            var index = existingDialogueEvents.BinarySearch(dialogueEvent, AudioProjectItem.IdComparer);
            if (index < 0)
                index = ~index;

            existingDialogueEvents.Insert(index, dialogueEvent);
        }
    }
}
