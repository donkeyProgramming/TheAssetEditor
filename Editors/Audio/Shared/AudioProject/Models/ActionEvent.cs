using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class ActionEvent : AudioProjectItem
    {
        // Actions is a List because in Wwise an Action Event can have multiple actions but making multiple Actions isn't
        // supported by the tool as it's unlikely to be needed so really there will only ever be one Action in the list.
        public List<Action> Actions { get; set; } = [];
        public Wh3ActionEventType ActionEventType { get; set; }

        public ActionEvent(uint id, string name, List<Action> actions, Wh3ActionEventType actionEventType)
        {
            Id = id;
            Name = name;
            HircType = AkBkHircType.Event;
            Actions = actions;
            ActionEventType = actionEventType;
        }

        public List<Action> GetPlayActions()
        {
            return Actions.
                Select(action => action)
                .Where(action => action.ActionType == AkActionType.Play)
                .ToList();
        }
    }

    public static class ActionEventListExtensions
    {
        private static readonly IComparer<ActionEvent> s_nameComparerIgnoreCase = new NameComparer();

        private sealed class NameComparer : IComparer<ActionEvent>
        {
            public int Compare(ActionEvent left, ActionEvent right)
            {
                var leftName = left?.Name ?? string.Empty;
                var rightName = right?.Name ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }

        public static void InsertAlphabetically(this List<ActionEvent> existingActionEvents, ActionEvent actionEvent)
        {
            ArgumentNullException.ThrowIfNull(existingActionEvents);
            ArgumentNullException.ThrowIfNull(actionEvent);

            if (existingActionEvents.Any(existingActionEvent => StringComparer.OrdinalIgnoreCase.Equals(existingActionEvent.Name, actionEvent.Name)))
                throw new ArgumentException($"Cannot add ActionEvent with Name {actionEvent.Name} as it already exists.");

            var index = existingActionEvents.BinarySearch(actionEvent, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            existingActionEvents.Insert(index, actionEvent);
        }

        public static void TryAdd(this List<ActionEvent> existingActionEvents, ActionEvent actionEvent)
        {
            ArgumentNullException.ThrowIfNull(existingActionEvents);
            ArgumentNullException.ThrowIfNull(actionEvent);

            if (existingActionEvents.Any(existingActionEvent => existingActionEvent.Id == actionEvent.Id))
                throw new ArgumentException($"Cannot add ActionEvent with Id {actionEvent.Id} as it already exists.");

            var index = existingActionEvents.BinarySearch(actionEvent, AudioProjectItem.IdComparer);
            if (index < 0)
                index = ~index;

            existingActionEvents.Insert(index, actionEvent);
        }
    }
}
