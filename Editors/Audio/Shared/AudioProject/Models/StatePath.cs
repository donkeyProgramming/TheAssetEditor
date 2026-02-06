using System;
using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class StatePath : AudioProjectItem
    {
        public uint TargetHircId { get; set; }
        public AkBkHircType TargetHircType { get; set; }
        public List<Node> Nodes { get; set; } = [];

        public StatePath(List<Node> nodes, uint targetHircId, AkBkHircType targetHircType)
        {
            Name = BuildName(nodes);
            Nodes = nodes;
            TargetHircId = targetHircId;
            TargetHircType = targetHircType;
        }

        public static string BuildName(List<Node> nodes)
        {
            return string.Join('.', nodes.Select(node => $"[{node.StateGroup.Name}]{node.State.Name}"));
        }

        public bool TargetHircTypeIsSound() => TargetHircType == AkBkHircType.Sound;

        public bool TargetHircTypeIsRandomSequenceContainer() => TargetHircType == AkBkHircType.RandomSequenceContainer;

        public class Node(StateGroup stateGroup, State state)
        {
            public StateGroup StateGroup { get; set; } = stateGroup;
            public State State { get; set; } = state;
        }
    }

    public static class StatePathListExtensions
    {
        private static readonly IComparer<StatePath> s_nameComparerIgnoreCase = new NameComparer();

        private sealed class NameComparer : IComparer<StatePath>
        {
            public int Compare(StatePath left, StatePath right)
            {
                var leftName = left?.Name ?? string.Empty;
                var rightName = right?.Name ?? string.Empty;
                return StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            }
        }

        public static void InsertAlphabetically(this List<StatePath> existingStatePaths, StatePath statePath)
        {
            ArgumentNullException.ThrowIfNull(existingStatePaths);
            ArgumentNullException.ThrowIfNull(statePath);

            if (existingStatePaths.Any(existingStatePath => StringComparer.OrdinalIgnoreCase.Equals(existingStatePath, statePath.Name)))
                throw new ArgumentException($"Cannot add StatePath with Name {statePath.Name} as it already exists.");

            var index = existingStatePaths.BinarySearch(statePath, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            existingStatePaths.Insert(index, statePath);
        }

        public static void TryAdd(this List<StatePath> existingStatePaths, StatePath statePath)
        {
            ArgumentNullException.ThrowIfNull(existingStatePaths);
            ArgumentNullException.ThrowIfNull(statePath);

            if (existingStatePaths.Any(existingStatePath => StringComparer.OrdinalIgnoreCase.Equals(existingStatePath.Name, statePath.Name)))
                throw new ArgumentException($"Cannot add StatePath with Name {statePath.Name} as it already exists.");

            var index = existingStatePaths.BinarySearch(statePath, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            existingStatePaths.Insert(index, statePath);
        }
    }
}
