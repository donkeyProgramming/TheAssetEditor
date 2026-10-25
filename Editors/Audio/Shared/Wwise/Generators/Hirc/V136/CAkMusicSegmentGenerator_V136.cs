using System;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    /// <summary>
    /// Builds the Music Segment for a piece of music, matching the vanilla shape recorded in
    /// <see cref="Wh3MusicHierarchyInformation"/>: one child track, and cue markers whose ids are the
    /// same in every shipped segment - three, or four under the in-battle container, whose script
    /// reads different ones (see <see cref="Wh3MusicHierarchyInformation.TrackStartCueName"/>).
    /// </summary>
    public class CAkMusicSegmentGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
        {
            var musicSegment = audioProjectItem as MusicSegment;

            var segmentHirc = new CAkMusicSegment_V136
            {
                Id = musicSegment.Id,
                HircType = musicSegment.HircType,
                Duration = musicSegment.DurationMs,
                MusicNodeParams = CreateMusicNodeParams(musicSegment)
            };

            // Entry and exit cues bracket the audio; the named cue sits with the entry cue, which
            // is where vanilla puts it on a segment that starts at zero. Under the in-battle
            // container the battle script's track cues take its place.
            segmentHirc.ArrayMarkersList.Add(CreateMarker(Wh3MusicHierarchyInformation.EntryCueMarkerId, 0, null));
            if (IsUnderInBattleContainer(musicSegment, soundBank))
            {
                var trackEndPosition = Math.Max(
                    musicSegment.DurationMs - Wh3MusicHierarchyInformation.TrackEndCueLeadMs,
                    musicSegment.DurationMs * Wh3MusicHierarchyInformation.TrackEndCueEarliestShare);
                segmentHirc.ArrayMarkersList.Add(CreateMarker(Wh3MusicHierarchyInformation.TrackStartCueMarkerId, 0, Wh3MusicHierarchyInformation.TrackStartCueName));
                segmentHirc.ArrayMarkersList.Add(CreateMarker(Wh3MusicHierarchyInformation.TrackEndCueMarkerId, trackEndPosition, Wh3MusicHierarchyInformation.TrackEndCueName));
            }
            else
                segmentHirc.ArrayMarkersList.Add(CreateMarker(Wh3MusicHierarchyInformation.CustomCueMarkerId, 0, Wh3MusicHierarchyInformation.CustomCueName));
            segmentHirc.ArrayMarkersList.Add(CreateMarker(Wh3MusicHierarchyInformation.ExitCueMarkerId, musicSegment.DurationMs, null));

            segmentHirc.UpdateSectionSize();
            return segmentHirc;
        }

        /// <summary>Whether the Music Random Sequence over this segment branches out of the in-battle
        /// container. Battle culture music has a copy of the segment under each of its containers,
        /// so this is decided per copy.</summary>
        private static bool IsUnderInBattleContainer(MusicSegment musicSegment, SoundBank soundBank) =>
            soundBank?.MusicRandomSequences
                .FirstOrDefault(musicRandomSequence => musicRandomSequence.Id == musicSegment.DirectParentId)
                ?.DirectParentId == Wh3MusicHierarchyInformation.InBattleMusicSwitchContainerId;

        private static MusicNodeParams_V136 CreateMusicNodeParams(MusicSegment musicSegment)
        {
            var musicNodeParams = new MusicNodeParams_V136();

            musicNodeParams.NodeBaseParams.DirectParentId = musicSegment.DirectParentId;
            musicNodeParams.NodeBaseParams.OverrideBusId = Wh3MusicHierarchyInformation.NoOverrideBusId;

            musicNodeParams.Children.ChildIds.Add(musicSegment.TrackId);

            musicNodeParams.AkMeterInfo.Tempo = musicSegment.Tempo;
            musicNodeParams.AkMeterInfo.TimeSigNumBeatsBar = 4;
            musicNodeParams.AkMeterInfo.TimeSigBeatValue = 4;
            musicNodeParams.AkMeterInfo.GridPeriod = Wh3MusicHierarchyInformation.DefaultGridPeriod;

            return musicNodeParams;
        }

        private static CAkMusicSegment_V136.AkMusicMarkerWwise_V136 CreateMarker(uint id, double position, string name)
        {
            return new CAkMusicSegment_V136.AkMusicMarkerWwise_V136
            {
                Id = id,
                Position = position,
                MarkerName = name
            };
        }
    }
}
