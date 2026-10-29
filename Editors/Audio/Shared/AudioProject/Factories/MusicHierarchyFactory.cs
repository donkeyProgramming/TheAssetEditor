using System.Collections.Generic;
using System.IO;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Editors.Audio.Shared.AudioProject.Factories
{
    public record MusicBranchResult(MusicRandomSequence MusicRandomSequence, List<MusicSegment> MusicSegments);

    public interface IMusicHierarchyFactory
    {
        MusicBranchResult CreateMusicBranch(
            HashSet<uint> usedHircIds,
            uint musicSwitchContainerId,
            string stateGroupName,
            string stateName,
            List<AudioFile> audioFiles,
            string language);
    }

    /// <summary>
    /// Builds the hierarchy under one branch of a music decision tree: a Music Random Sequence
    /// holding a Music Segment per audio file, each wrapping a Music Track. See
    /// <see cref="Wh3MusicHierarchyInformation"/> for the vanilla shape this reproduces.
    /// </summary>
    public class MusicHierarchyFactory : IMusicHierarchyFactory
    {
        public MusicBranchResult CreateMusicBranch(
            HashSet<uint> usedHircIds,
            uint musicSwitchContainerId,
            string stateGroupName,
            string stateName,
            List<AudioFile> audioFiles,
            string language)
        {
            var randomSequenceIds = IdGenerator.GenerateIds(usedHircIds);

            var randomSequence = new MusicRandomSequence
            {
                Guid = randomSequenceIds.Guid,
                Id = randomSequenceIds.Id,
                Name = stateName,
                DirectParentId = musicSwitchContainerId,
                StateName = stateName,
                StateGroupName = stateGroupName,
                OverrideBusId = Wh3MusicHierarchyInformation.NoOverrideBusId,
                PlaylistRootItemId = RootPlaylistItemId
            };

            var musicSegments = new List<MusicSegment>();

            foreach (var audioFile in audioFiles)
            {
                var segmentIds = IdGenerator.GenerateIds(usedHircIds);
                var trackIds = IdGenerator.GenerateIds(usedHircIds);

                musicSegments.Add(new MusicSegment
                {
                    Guid = segmentIds.Guid,
                    Id = segmentIds.Id,
                    Name = Path.GetFileNameWithoutExtension(audioFile.WavPackFileName),
                    TrackId = trackIds.Id,
                    DirectParentId = randomSequence.Id,
                    SourceId = audioFile.Id,
                    Language = language
                });

                // The playlist node ids only have to be unique inside their own container, so they
                // are numbered rather than drawn from the hirc id space. Duration and media size are
                // left alone here - they come from the encoded wem at compile time, because the wem
                // is what ships and the segment has to agree with it.
                randomSequence.Segments.Add(new MusicPlaylistEntry
                {
                    SegmentId = segmentIds.Id,
                    PlaylistItemId = RootPlaylistItemId + randomSequence.Segments.Count + 1
                });
            }

            return new MusicBranchResult(randomSequence, musicSegments);
        }

        /// <summary>Not zero, which Wwise uses to mean no item.</summary>
        private const int RootPlaylistItemId = 1;
    }
}
