using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// Everything a music project has that a Dialogue Event project does not: the cultures wired into
    /// the music scripts, and the music hierarchy their events' States select. The events themselves,
    /// and the Sounds of the ambient fragments, are ordinary Action Events and Sounds in the project's
    /// global_music SoundBank.
    /// </summary>
    public class MusicProjectData
    {
        /// <summary>In project order, which is the order the scripts are built in.</summary>
        public List<MusicCulture> Cultures { get; set; } = [];

        /// <summary>Which audio each music event plays when, one tree per State. The lists below are
        /// generated from these.</summary>
        public List<MusicTree> Trees { get; set; } = [];

        /// <summary>The branches merged into vanilla Music Switch containers, and their segments.</summary>
        public List<MusicRandomSequence> RandomSequences { get; set; } = [];
        public List<MusicSegment> Segments { get; set; } = [];

        /// <summary>Clips added as sub-tracks to vanilla pulse switch tracks. No hircs of their own.</summary>
        public List<AmsPulse> AmsPulses { get; set; } = [];

        /// <summary>Cultures added to the vanilla ambient fragments container, with their generated switches.</summary>
        public List<AmsFragment> AmsFragments { get; set; } = [];

        public bool HasHierarchy => RandomSequences.Count != 0 || AmsPulses.Count != 0 || AmsFragments.Count != 0;

        public MusicSegment GetSegment(uint id) => Segments.FirstOrDefault(segment => segment.Id == id);

        public List<MusicSegment> GetSegments(MusicRandomSequence randomSequence) => randomSequence.Segments
            .Select(entry => GetSegment(entry.SegmentId))
            .Where(segment => segment != null)
            .ToList();

        /// <summary>Every hirc id the music generates, so a new one cannot reuse it. Tracks and the
        /// fragment switches are built at compile time from ids recorded here.</summary>
        public IEnumerable<uint> GetUsedHircIds() =>
            RandomSequences.Select(randomSequence => randomSequence.Id)
                .Concat(Segments.SelectMany(segment => new[] { segment.Id, segment.TrackId }))
                .Concat(AmsFragments.SelectMany(fragment => fragment.SoundIds
                    .Concat(fragment.ContainerIds)
                    .Concat(fragment.CallResponseSwitches.Select(callResponse => callResponse.Id))
                    .Append(fragment.KeySwitchContainerId)));

        /// <summary>The source ids of every audio file the music plays directly, rather than through a Sound.</summary>
        public IEnumerable<uint> GetDirectSourceIds() =>
            Segments.Select(segment => segment.SourceId)
                .Concat(AmsPulses.SelectMany(amsPulse => amsPulse.Clips).Select(clip => clip.SourceId));

        /// <summary>A copy without the parts that have nothing to play.</summary>
        public MusicProjectData Clean() => new()
        {
            Cultures = [.. Cultures],
            Trees = [.. Trees],
            RandomSequences = [.. RandomSequences.Where(randomSequence => randomSequence.Segments.Count != 0)],
            Segments = [.. Segments],
            AmsPulses = [.. AmsPulses.Where(amsPulse => amsPulse.Clips.Count != 0)],
            AmsFragments = [.. AmsFragments.Where(amsFragment => amsFragment.SoundIds.Count != 0)]
        };
    }
}
