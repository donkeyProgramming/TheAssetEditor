using System.Collections.Generic;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// Audio contributed to the adaptive music system's pulse layers for one State. These State
    /// Groups are read by vanilla switch tracks, which hold one sub-track per culture, so a culture
    /// is served by adding a sub-track to each of them rather than by a hirc of its own - hence
    /// clips and no ids. Fewer clips than tracks are cycled, so a single file is a valid answer.
    /// </summary>
    public class AmsPulse
    {
        /// <summary>One of the three pulse State Groups - see
        /// <see cref="GameInformation.Warhammer3.Wh3MusicHierarchyInformation.IsAmsPulseStateGroup"/>.</summary>
        public string StateGroupName { get; set; }

        /// <summary>The State the Action Event sets, which is what the switch track matches on.</summary>
        public string StateName { get; set; }

        public List<AmsPulseClip> Clips { get; set; } = [];
    }

    /// <summary>
    /// One clip handed to a switch track. Not a <see cref="MusicSegment"/>: it owns no hirc, it is
    /// only a row in a vanilla track's source and playlist lists.
    /// </summary>
    public class AmsPulseClip
    {
        public uint SourceId { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }

        /// <summary>Length of the audio in milliseconds. The clip is trimmed against the segment it
        /// is dropped into rather than played whole, so this is what the trim is computed from.</summary>
        public double DurationMs { get; set; }
    }
}
