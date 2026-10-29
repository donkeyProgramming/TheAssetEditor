using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// The Music Random Sequence for one branch of a music decision tree. This is the node the tree
    /// actually names: the State an Action Event sets selects a branch, the branch points here, and
    /// this picks between the <see cref="MusicSegment"/>s underneath it.
    ///
    /// One segment plays as a continuous sequence; several play as a weighted random pick that
    /// avoids repeating - which is the shape vanilla uses for a culture with music variations.
    /// </summary>
    public class MusicRandomSequence : AudioProjectItem
    {
        /// <summary>The vanilla Music Switch container this branch is merged into - see
        /// <see cref="GameInformation.Warhammer3.Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds"/>.</summary>
        public uint DirectParentId { get; set; }

        /// <summary>The State that selects this branch - the same State the Action Event sets. The
        /// decision tree node is keyed on its hash.</summary>
        public string StateName { get; set; }

        /// <summary>The State Group that State belongs to. A container can branch on several, so the
        /// merge needs this to know which level of the tree the State sits at.</summary>
        public string StateGroupName { get; set; }

        /// <summary>Zero to inherit the bus, which is what almost every vanilla branch does.</summary>
        public uint OverrideBusId { get; set; }

        /// <summary>Beats per minute for the container's meter info.</summary>
        public float Tempo { get; set; } = MusicSegment.DefaultTempo;

        /// <summary>
        /// The playlist is a tree, and its nodes carry ids of their own that are separate from hirc
        /// ids. The root holds no segment; the entries below it each name one.
        /// </summary>
        public int PlaylistRootItemId { get; set; }

        public List<MusicPlaylistEntry> Segments { get; set; } = [];

        /// <summary>The node of the State's <see cref="MusicTree"/> this plays the audio of, which is
        /// what decides the decision tree paths that reach it.</summary>
        public int TreeNodeId { get; set; }

        public MusicRandomSequence()
        {
            HircType = AkBkHircType.Music_Random_Sequence;
        }
    }

    public class MusicPlaylistEntry
    {
        public uint SegmentId { get; set; }
        public int PlaylistItemId { get; set; }
    }
}
