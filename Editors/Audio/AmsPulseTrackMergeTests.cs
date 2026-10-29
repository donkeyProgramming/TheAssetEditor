using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.ByteParsing;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    // Adding a culture to the adaptive music system's pulse layers. The shape asserted here was read
    // out of the shipped banks (October 2026): sixteen
    // sub-tracks, fifteen with a clip and a clipless 'Off', and an association array indexed by
    // sub-track. That indexing is what makes this delicate - the array is positional, so getting it
    // wrong re-points other cultures rather than failing.
    internal class AmsPulseTrackMergeTests
    {
        const uint TrackId = 726934980;
        const uint PercussionGroupId = 1282272532;
        const double SegmentDurationMs = 41600;

        [Test]
        public void ANewCultureIsAppendedAsItsOwnSubTrack()
        {
            var merged = Merge(CreateVanillaTrack(), CreateClip(sourceId: 900, durationMs: 41600), "Araby");

            Assert.Multiple(() =>
            {
                Assert.That(merged.SwitchParams.SwitchAssoc, Has.Count.EqualTo(4));
                Assert.That(merged.SwitchParams.SwitchAssoc[3], Is.EqualTo(WwiseHash.Compute("Araby")));
                Assert.That(merged.NumSubTrack, Is.EqualTo(4));

                // The clip has to name the sub-track it belongs to, not the source's position.
                var clip = merged.PlaylistList.Single(item => item.SourceId == 900);
                Assert.That(clip.TrackId, Is.EqualTo(3));
                Assert.That(merged.SourceList.Select(source => source.AkMediaInformation.SourceId), Does.Contain(900u));
            });
        }

        [Test]
        public void TheVanillaCulturesKeepTheirSubTrackIndexes()
        {
            var vanillaTrack = CreateVanillaTrack();
            var merged = Merge(vanillaTrack, CreateClip(sourceId: 900, durationMs: 41600), "Araby");

            Assert.Multiple(() =>
            {
                // Inserting rather than appending would shift these, which does not fail anywhere -
                // it just plays Cathay's stem when the game asks for Chaos.
                for (var subTrack = 0; subTrack < vanillaTrack.SwitchParams.SwitchAssoc.Count; subTrack++)
                {
                    Assert.That(merged.SwitchParams.SwitchAssoc[subTrack],
                        Is.EqualTo(vanillaTrack.SwitchParams.SwitchAssoc[subTrack]));

                    var vanillaClip = vanillaTrack.PlaylistList.FirstOrDefault(item => item.TrackId == subTrack);
                    var mergedClip = merged.PlaylistList.FirstOrDefault(item => item.TrackId == subTrack);
                    Assert.That(mergedClip?.SourceId, Is.EqualTo(vanillaClip?.SourceId));
                }
            });
        }

        [Test]
        public void AClipLongerThanTheSegmentIsTrimmedToIt()
        {
            // Vanilla stems run past the segment and are cut with a negative end trim; a clip left
            // whole would overlap the next segment rather than being cut off.
            var merged = Merge(CreateVanillaTrack(), CreateClip(sourceId: 900, durationMs: 48000), "Araby");
            var clip = merged.PlaylistList.Single(item => item.SourceId == 900);

            Assert.Multiple(() =>
            {
                Assert.That(clip.SrcDuration, Is.EqualTo(48000));
                Assert.That(clip.EndTrimOffset, Is.EqualTo(-6400));
                Assert.That(clip.PlayAt, Is.EqualTo(0));
                Assert.That(clip.BeginTrimOffset, Is.EqualTo(0));
            });
        }

        [Test]
        public void AClipShorterThanTheSegmentIsLeftWhole()
        {
            var merged = Merge(CreateVanillaTrack(), CreateClip(sourceId: 900, durationMs: 20000), "Araby");
            var clip = merged.PlaylistList.Single(item => item.SourceId == 900);

            Assert.That(clip.EndTrimOffset, Is.EqualTo(0), "trimming a short clip further would cut audio that is already too short");
        }

        [Test]
        public void AddingTheSameCultureTwiceReplacesItsClipRatherThanDuplicatingTheSubTrack()
        {
            var service = new AmsPulseTrackMergeService();
            var once = service.AddSubTracks(CreateVanillaTrack(),
                [new AmsPulseBranch("Araby", CreateClip(sourceId: 900, durationMs: 41600))], SegmentDurationMs);

            var twice = service.AddSubTracks(once,
                [new AmsPulseBranch("Araby", CreateClip(sourceId: 901, durationMs: 41600))], SegmentDurationMs);

            Assert.Multiple(() =>
            {
                // Two sub-tracks answering to one State means the second is unreachable.
                Assert.That(twice.SwitchParams.SwitchAssoc.Count(id => id == WwiseHash.Compute("Araby")), Is.EqualTo(1));
                Assert.That(twice.PlaylistList.Where(item => item.TrackId == 3).Select(item => item.SourceId),
                    Is.EqualTo(new[] { 901u }));
                Assert.That(twice.SourceList.Select(source => source.AkMediaInformation.SourceId), Does.Not.Contain(900u));
            });
        }

        [Test]
        public void MergingDoesNotEditTheVanillaTrack()
        {
            var vanillaTrack = CreateVanillaTrack();
            Merge(vanillaTrack, CreateClip(sourceId: 900, durationMs: 41600), "Araby");

            Assert.Multiple(() =>
            {
                Assert.That(vanillaTrack.SwitchParams.SwitchAssoc, Has.Count.EqualTo(3));
                Assert.That(vanillaTrack.SourceList, Has.Count.EqualTo(2));
                Assert.That(vanillaTrack.PlaylistList, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void TheMergedTrackSurvivesAWriteReadRoundTrip()
        {
            // A generated hirc has no parsed SectionSize to fall back on, so bad size arithmetic
            // corrupts the bank from the next hirc onwards rather than failing here.
            var merged = Merge(CreateVanillaTrack(), CreateClip(sourceId: 900, durationMs: 48000), "Araby");

            var reloaded = new CAkMusicTrack_V136();
            reloaded.ReadHirc(new ByteChunk(merged.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(TrackId));
                Assert.That(reloaded.TrackType, Is.EqualTo(CAkMusicTrack_V136.SwitchTrackType));
                Assert.That(reloaded.SwitchParams, Is.Not.Null);
                Assert.That(reloaded.SwitchParams.GroupId, Is.EqualTo(PercussionGroupId));
                Assert.That(reloaded.SwitchParams.SwitchAssoc, Is.EqualTo(merged.SwitchParams.SwitchAssoc));
                Assert.That(reloaded.NumSubTrack, Is.EqualTo(4));
                Assert.That(reloaded.SourceList, Has.Count.EqualTo(3));
                Assert.That(reloaded.PlaylistList, Has.Count.EqualTo(3));
                Assert.That(reloaded.PlaylistList.Single(item => item.SourceId == 900).EndTrimOffset, Is.EqualTo(-6400));
                Assert.That(reloaded.LookAheadTime, Is.EqualTo(merged.LookAheadTime));
            });
        }

        [Test]
        public void ANonSwitchTrackIsRefused()
        {
            var normalTrack = new CAkMusicTrack_V136 { Id = TrackId, TrackType = Wh3MusicHierarchyInformation.NormalTrackType };

            Assert.That(
                () => new AmsPulseTrackMergeService().AddSubTracks(
                    normalTrack, [new AmsPulseBranch("Araby", CreateClip(900, 41600))], SegmentDurationMs),
                Throws.TypeOf<NotSupportedException>());
        }

        static CAkMusicTrack_V136 Merge(CAkMusicTrack_V136 vanillaTrack, AmsPulseClip clip, string stateName)
        {
            return new AmsPulseTrackMergeService()
                .AddSubTracks(vanillaTrack, [new AmsPulseBranch(stateName, clip)], SegmentDurationMs);
        }

        static AmsPulseClip CreateClip(uint sourceId, double durationMs)
        {
            return new AmsPulseClip
            {
                SourceId = sourceId,
                DurationMs = durationMs,
                InMemoryMediaSize = 4242,
                Language = Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)
            };
        }

        /// <summary>Cut down from the real thing: two cultures with clips and a clipless 'Off', which
        /// is the arrangement every vanilla pulse track has.</summary>
        static CAkMusicTrack_V136 CreateVanillaTrack()
        {
            var track = new CAkMusicTrack_V136
            {
                Id = TrackId,
                HircType = AkBkHircType.Music_Track,
                TrackType = CAkMusicTrack_V136.SwitchTrackType,
                LookAheadTime = Wh3MusicHierarchyInformation.TrackLookAheadTime,
                NumSubTrack = 3,
                TransRule = new CAkMusicTrack_V136.AkMusicTrackTransRule_V136(),
                NodeBaseParams = new NodeBaseParams_V136(),
                SwitchParams = new CAkMusicTrack_V136.AkMusicTrackSwitchParams_V136
                {
                    GroupType = 1,
                    GroupId = PercussionGroupId,
                    DefaultSwitch = WwiseHash.Compute("Cathay"),
                    SwitchAssoc =
                    [
                        WwiseHash.Compute("Cathay"),
                        WwiseHash.Compute("Chaos"),
                        WwiseHash.Compute("Off")
                    ]
                }
            };

            foreach (var (subTrack, sourceId) in new[] { (0u, 100u), (1u, 101u) })
            {
                track.SourceList.Add(new AkBankSourceData_V136
                {
                    PluginId = Wh3MusicHierarchyInformation.VorbisPluginId,
                    StreamType = AKBKSourceType.Streaming,
                    AkMediaInformation = new AkBankSourceData_V136.AkMediaInformation_V136 { SourceId = sourceId }
                });

                track.PlaylistList.Add(new CAkMusicTrack_V136.AkTrackSrcInfo_V136
                {
                    TrackId = subTrack,
                    SourceId = sourceId,
                    SrcDuration = SegmentDurationMs
                });
            }

            return track;
        }
    }
}
