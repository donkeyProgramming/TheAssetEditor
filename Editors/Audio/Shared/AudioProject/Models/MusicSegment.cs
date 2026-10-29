using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// One piece of music: a Music Segment wrapping a single Music Track wrapping one audio file.
    /// This is what a decision tree branch points at, and the only part of the music hierarchy that
    /// carries audio - see <see cref="GameInformation.Warhammer3.Wh3MusicHierarchyInformation"/> for
    /// the vanilla shape this reproduces.
    ///
    /// Unlike a Sound, a music node is parented inside the music hierarchy rather than under an
    /// actor mixer, and never overrides the bus - both are zero in every vanilla music node.
    /// </summary>
    public class MusicSegment : AudioProjectItem
    {
        /// <summary>The Music Track underneath this segment. Vanilla always has exactly one for a
        /// straightforward piece of music, and the segment lists it as its only child.</summary>
        public uint TrackId { get; set; }

        /// <summary>The Music Random Sequence this segment sits under, which is what the decision
        /// tree node actually names.</summary>
        public uint DirectParentId { get; set; }

        public uint SourceId { get; set; }
        public long InMemoryMediaSize { get; set; }
        public string Language { get; set; }

        /// <summary>
        /// Length of the audio in milliseconds, read from the encoded wem at compile time. It sets the
        /// segment duration, the exit cue position and the clip's source duration. Fractional, as
        /// vanilla's are.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>Beats per minute. Only used for the segment's meter info, which the game needs
        /// for beat-synced transitions; 120 is what most vanilla segments use.</summary>
        public float Tempo { get; set; } = DefaultTempo;

        public const float DefaultTempo = 120f;

        public MusicSegment()
        {
            HircType = AkBkHircType.Music_Segment;
        }
    }
}
