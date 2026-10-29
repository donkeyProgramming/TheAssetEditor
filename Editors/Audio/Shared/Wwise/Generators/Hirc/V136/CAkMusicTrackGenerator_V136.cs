using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    /// <summary>
    /// Builds the Music Track under a Music Segment - the node that actually names the audio. One
    /// streamed Vorbis source and one clip covering the whole file, which is what every vanilla
    /// music track for a straightforward piece of music looks like.
    /// </summary>
    public class CAkMusicTrackGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
        {
            var musicSegment = audioProjectItem as MusicSegment;

            var trackHirc = new CAkMusicTrack_V136
            {
                Id = musicSegment.TrackId,
                HircType = AkBkHircType.Music_Track,
                TrackType = Wh3MusicHierarchyInformation.NormalTrackType,
                LookAheadTime = Wh3MusicHierarchyInformation.TrackLookAheadTime,

                // The track hangs off its segment, and like every other music node it leaves the
                // bus alone rather than overriding it the way a Sound under an actor mixer does.
                NodeBaseParams = new NodeBaseParams_V136
                {
                    DirectParentId = musicSegment.Id,
                    OverrideBusId = Wh3MusicHierarchyInformation.NoOverrideBusId
                }
            };

            trackHirc.SourceList.Add(CreateSource(musicSegment));
            trackHirc.PlaylistList.Add(CreateClip(musicSegment));

            // One sub-track holding the single clip above. Vanilla writes this whenever there is a
            // playlist at all, which there always is here.
            trackHirc.NumSubTrack = 1;

            trackHirc.UpdateSectionSize();
            return trackHirc;
        }

        private static AkBankSourceData_V136 CreateSource(MusicSegment musicSegment)
        {
            return new AkBankSourceData_V136
            {
                PluginId = Wh3MusicHierarchyInformation.VorbisPluginId,
                StreamType = AKBKSourceType.Streaming,
                AkMediaInformation = new AkBankSourceData_V136.AkMediaInformation_V136
                {
                    SourceId = musicSegment.SourceId,
                    InMemoryMediaSize = (uint)musicSegment.InMemoryMediaSize,
                    SourceBits = (byte)(musicSegment.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx) ? 0x00 : 0x01)
                }
            };
        }

        private static CAkMusicTrack_V136.AkTrackSrcInfo_V136 CreateClip(MusicSegment musicSegment)
        {
            // The whole file, untrimmed and starting at the segment's zero point. Vanilla trims
            // clips where a track has a pickup before the first beat; nothing here knows about
            // musical timing, so the clip is left as recorded.
            return new CAkMusicTrack_V136.AkTrackSrcInfo_V136
            {
                TrackId = 0,
                SourceId = musicSegment.SourceId,
                EventId = 0,
                PlayAt = 0,
                BeginTrimOffset = 0,
                EndTrimOffset = 0,
                SrcDuration = musicSegment.DurationMs
            };
        }
    }
}
