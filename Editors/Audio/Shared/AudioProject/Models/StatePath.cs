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

        public static StatePath Create(List<Node> nodes, uint targetHircId, AkBkHircType targetHircType)
        {
            return new StatePath
            {
                Name = BuildName(nodes),
                Nodes = nodes,
                TargetHircId = targetHircId,
                TargetHircType = targetHircType
            };
        }

        public static string BuildName(List<Node> nodes)
        {
            return string.Join('.', nodes.Select(node => $"[{node.StateGroup.Name}]{node.State.Name}"));
        }

        public bool TargetHircTypeIsSound()
        {
            if (TargetHircType == AkBkHircType.Sound)
                return true;
            else
                return false;
        }

        public bool TargetHircTypeIsRandomSequenceContainer()
        {
            if (TargetHircType == AkBkHircType.RandomSequenceContainer)
                return true;
            else
                return false;
        }

        public class Node
        {
            public StateGroup StateGroup { get; set; }
            public State State { get; set; }

            public static Node Create(StateGroup stateGroup, State state)
            {
                return new Node
                {
                    StateGroup = stateGroup,
                    State = state
                };
            }
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

        public static void InsertAlphabetically(this List<StatePath> statePaths, StatePath statePath)
        {
            ArgumentNullException.ThrowIfNull(statePaths);
            ArgumentNullException.ThrowIfNull(statePath);

            if (statePaths.Any(statePath => StringComparer.OrdinalIgnoreCase.Equals(statePath?.Name ?? string.Empty, statePath.Name ?? string.Empty)))
                throw new ArgumentException($"Cannot add StatePath with Name {statePath.Name} as it already exists.");

            var index = statePaths.BinarySearch(statePath, s_nameComparerIgnoreCase);
            if (index < 0)
                index = ~index;

            statePaths.Insert(index, statePath);
        }
    }
}
