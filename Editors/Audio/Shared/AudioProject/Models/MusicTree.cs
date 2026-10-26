using System;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// Which of a music event's audio plays when: a decision tree over the other things the vanilla
    /// music containers branch on, below the culture the event selects - the battle result, the
    /// battle phase, the musical key and so on. See <see cref="GameInformation.Warhammer3.Wh3MusicTreeLevelInformation"/>.
    ///
    /// It can say as much or as little as wanted. A root holding the audio and splitting on nothing
    /// plays the same audio everywhere, the way every event did before trees existed. Each split
    /// narrows one part of the music down further, to the point of a different file for every
    /// combination vanilla has.
    ///
    /// The tree is what the modder edits. The music hierarchy that plays it is generated from it -
    /// one random sequence per node that has audio, per container that node can play in - and the
    /// decision tree paths that reach each one are worked out when the project is compiled, from the
    /// game's containers as they are then.
    /// </summary>
    public class MusicTree
    {
        public string StateGroupName { get; set; }
        public string StateName { get; set; }
        public MusicTreeNode Root { get; set; } = new();

        public IEnumerable<MusicTreeNode> GetNodes() => Root.GetNodes();

        public MusicTreeNode GetNode(int id) => GetNodes().FirstOrDefault(node => node.Id == id);

        public MusicTreeNode GetParent(MusicTreeNode child) => GetNodes().FirstOrDefault(node => node.Branches.Contains(child));

        public int NextNodeId() => GetNodes().Max(node => node.Id) + 1;

        /// <summary>
        /// The node that plays for one situation: down through the branches matching it, falling back
        /// to the "any other value" branch, and stopping at a split on a level that does not apply,
        /// for which <paramref name="valueForLevel"/> returns null. The deepest node on the way that has
        /// audio is the one that plays; null when none has.
        /// </summary>
        public MusicTreeNode Evaluate(Func<string, string> valueForLevel)
        {
            var selected = Root.AudioFileIds.Count != 0 ? Root : null;
            var node = Root;

            while (node.SplitBy != null && valueForLevel(node.SplitBy) is string value)
            {
                var next = node.Branches.FirstOrDefault(branch => branch.Value != null && string.Equals(branch.Value, value, StringComparison.OrdinalIgnoreCase))
                    ?? node.Branches.FirstOrDefault(branch => branch.Value == null);
                if (next == null)
                    break;

                node = next;
                if (node.AudioFileIds.Count != 0)
                    selected = node;
            }

            return selected;
        }
    }

    /// <summary>
    /// One node of a <see cref="MusicTree"/>. A node with audio plays it wherever the tree leads to
    /// it; a node without plays its parent's. A node can split on one level, into a branch per value
    /// of it that should play something different, and optionally an "any other value" branch.
    /// </summary>
    public class MusicTreeNode
    {
        /// <summary>Unique within the tree, and what the generated hierarchy points back at.</summary>
        public int Id { get; set; }

        /// <summary>The value of the parent's <see cref="SplitBy"/> level this branch is for, by name;
        /// null for the root and for the parent's "any other value" branch.</summary>
        public string Value { get; set; }

        /// <summary>The audio, in playlist order, by audio file id.</summary>
        public List<uint> AudioFileIds { get; set; } = [];

        /// <summary>The level the branches below are values of: a State Group name, or
        /// <see cref="GameInformation.Warhammer3.Wh3MusicTreeLevelInformation.ContainerLevel"/>. Null
        /// when the node does not split.</summary>
        public string SplitBy { get; set; }

        public List<MusicTreeNode> Branches { get; set; } = [];

        public IEnumerable<MusicTreeNode> GetNodes() => new[] { this }.Concat(Branches.SelectMany(branch => branch.GetNodes()));
    }
}
