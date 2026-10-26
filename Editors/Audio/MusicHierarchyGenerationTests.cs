using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise.Generators.Hirc.V136;
using Shared.ByteParsing;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Test.Audio
{
    // The shape asserted here is the vanilla one, read out of the shipped banks and
    // recorded in Wh3MusicHierarchyInformation. The round-trip checks
    // matter as much as the field checks: a generated hirc has no parsed SectionSize to fall back
    // on, so if the size arithmetic is wrong the bank is corrupt from the next hirc onwards.
    internal class MusicHierarchyGenerationTests
    {
        const uint SegmentId = 1000;
        const uint TrackId = 1001;
        const uint RanSeqId = 1002;
        const uint SourceId = 555444333;
        const uint SwitchContainerId = 698158058;
        const double DurationMs = 28800.02267573696;

        [Test]
        public void GeneratedSegmentCarriesTheThreeVanillaCueMarkers()
        {
            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136().GenerateHirc(CreateMusicSegment());

            Assert.Multiple(() =>
            {
                Assert.That(segment.ArrayMarkersList, Has.Count.EqualTo(3));

                Assert.That(segment.ArrayMarkersList[0].Id, Is.EqualTo(Wh3MusicHierarchyInformation.EntryCueMarkerId));
                Assert.That(segment.ArrayMarkersList[0].Position, Is.EqualTo(0));

                Assert.That(segment.ArrayMarkersList[1].Id, Is.EqualTo(Wh3MusicHierarchyInformation.CustomCueMarkerId));
                Assert.That(segment.ArrayMarkersList[1].MarkerName, Is.EqualTo(Wh3MusicHierarchyInformation.CustomCueName));

                // The exit cue has to sit at the end of the audio, not at some fixed value, or the
                // segment ends before or after the file does.
                Assert.That(segment.ArrayMarkersList[2].Id, Is.EqualTo(Wh3MusicHierarchyInformation.ExitCueMarkerId));
                Assert.That(segment.ArrayMarkersList[2].Position, Is.EqualTo(DurationMs));
            });
        }

        // battle_music.dat ignores Update_PreBattle and drives its state machine off track start and
        // track end cues, so the copy of a segment under the in-battle container carries those. The
        // copy under any other container keeps the campaign's cue.
        [Test]
        public void ASegmentUnderTheInBattleContainerCarriesTheBattleScriptsTrackCues()
        {
            var inBattle = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136()
                .GenerateHirc(CreateMusicSegment(), Wh3MusicHierarchyInformation.InBattleMusicSwitchContainerId);
            var results = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136()
                .GenerateHirc(CreateMusicSegment(), 26264058);

            Assert.Multiple(() =>
            {
                Assert.That(inBattle.ArrayMarkersList.Select(marker => marker.Id), Is.EqualTo(new[]
                {
                    Wh3MusicHierarchyInformation.EntryCueMarkerId,
                    Wh3MusicHierarchyInformation.TrackStartCueMarkerId,
                    Wh3MusicHierarchyInformation.TrackEndCueMarkerId,
                    Wh3MusicHierarchyInformation.ExitCueMarkerId
                }));
                Assert.That(inBattle.ArrayMarkersList[1].Position, Is.EqualTo(0));
                Assert.That(inBattle.ArrayMarkersList[2].Position, Is.EqualTo(DurationMs - Wh3MusicHierarchyInformation.TrackEndCueLeadMs));
                Assert.That(inBattle.ArrayMarkersList[3].Position, Is.EqualTo(DurationMs));

                Assert.That(results.ArrayMarkersList[1].MarkerName, Is.EqualTo(Wh3MusicHierarchyInformation.CustomCueName));
            });
        }

        // The ids are not looked up anywhere, but vanilla's are the hash of the name and the battle
        // script is handed the name - a mismatch would mean one of the two constants is mistyped.
        [TestCase(Wh3MusicHierarchyInformation.CustomCueMarkerId, Wh3MusicHierarchyInformation.CustomCueName)]
        [TestCase(Wh3MusicHierarchyInformation.TrackStartCueMarkerId, Wh3MusicHierarchyInformation.TrackStartCueName)]
        [TestCase(Wh3MusicHierarchyInformation.TrackEndCueMarkerId, Wh3MusicHierarchyInformation.TrackEndCueName)]
        public void ANamedCuesIdIsTheHashOfItsName(uint id, string name)
        {
            Assert.That(WwiseHash.Compute(name.TrimEnd('\0')), Is.EqualTo(id));
        }

        // On a short piece the lead would put the track end in the first half, where vanilla never has
        // one, so it stops at a share of the way through instead.
        [Test]
        public void AShortPiecesTrackEndCueStaysNearItsEnd()
        {
            var musicSegment = CreateMusicSegment();
            musicSegment.DurationMs = 8000;

            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136()
                .GenerateHirc(musicSegment, Wh3MusicHierarchyInformation.InBattleMusicSwitchContainerId);

            Assert.That(segment.ArrayMarkersList[2].Position, Is.EqualTo(8000 * Wh3MusicHierarchyInformation.TrackEndCueEarliestShare));
        }

        [Test]
        public void TheInBattleSegmentSurvivesAWriteReadRoundTrip()
        {
            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136()
                .GenerateHirc(CreateMusicSegment(), Wh3MusicHierarchyInformation.InBattleMusicSwitchContainerId);

            var reloaded = new CAkMusicSegment_V136();
            reloaded.ReadHirc(new ByteChunk(segment.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.ArrayMarkersList, Has.Count.EqualTo(4));
                Assert.That(reloaded.ArrayMarkersList[1].MarkerName, Is.EqualTo(Wh3MusicHierarchyInformation.TrackStartCueName));
                Assert.That(reloaded.ArrayMarkersList[2].MarkerName, Is.EqualTo(Wh3MusicHierarchyInformation.TrackEndCueName));
                Assert.That(reloaded.MusicNodeParams.Children.ChildIds, Is.EqualTo(new[] { TrackId }));
            });
        }

        [Test]
        public void GeneratedSegmentIsParentedInTheMusicHierarchyAndDoesNotOverrideTheBus()
        {
            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136().GenerateHirc(CreateMusicSegment());
            var nodeParams = segment.MusicNodeParams;

            Assert.Multiple(() =>
            {
                Assert.That(nodeParams.NodeBaseParams.DirectParentId, Is.EqualTo(RanSeqId));
                Assert.That(nodeParams.NodeBaseParams.OverrideBusId, Is.EqualTo(0), "vanilla music nodes never override the bus");
                Assert.That(nodeParams.Children.ChildIds, Is.EqualTo(new[] { TrackId }));
                Assert.That(segment.Duration, Is.EqualTo(DurationMs));
            });
        }

        // A transition into a node is scheduled against that node's musical grid, so a grid period of
        // zero leaves Wwise nothing to make the switch on and the node never starts - the branch is
        // selected and nothing is heard. Every vanilla child of the two containers this touches
        // declares a non-zero period, and the ones that do not override the meter declare Wwise's
        // default of 1000. These two pin that down at both levels of the hierarchy.
        [Test]
        public void GeneratedSegmentDeclaresAPlayableMusicalGrid()
        {
            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136().GenerateHirc(CreateMusicSegment());
            var meterInfo = segment.MusicNodeParams.AkMeterInfo;

            Assert.Multiple(() =>
            {
                Assert.That(meterInfo.GridPeriod, Is.EqualTo(Wh3MusicHierarchyInformation.DefaultGridPeriod));
                Assert.That(meterInfo.GridPeriod, Is.GreaterThan(0), "a zero grid period is silent rather than an error");
            });
        }

        [Test]
        public void GeneratedRandomSequenceDeclaresAPlayableMusicalGrid()
        {
            var ranSeq = (CAkMusicRanSeqCntr_V136)new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(CreateRandomSequence(SegmentId));
            var meterInfo = ranSeq.MusicTransNodeParams.MusicNodeParams.AkMeterInfo;

            Assert.Multiple(() =>
            {
                Assert.That(meterInfo.GridPeriod, Is.EqualTo(Wh3MusicHierarchyInformation.DefaultGridPeriod));
                Assert.That(meterInfo.GridPeriod, Is.GreaterThan(0), "a zero grid period is silent rather than an error");
            });
        }

        [Test]
        public void GeneratedTrackNamesTheAudioAsAStreamedVorbisSource()
        {
            var track = (CAkMusicTrack_V136)new CAkMusicTrackGenerator_V136().GenerateHirc(CreateMusicSegment());

            Assert.Multiple(() =>
            {
                Assert.That(track.Id, Is.EqualTo(TrackId));
                Assert.That(track.TrackType, Is.EqualTo(Wh3MusicHierarchyInformation.NormalTrackType));
                Assert.That(track.LookAheadTime, Is.EqualTo(Wh3MusicHierarchyInformation.TrackLookAheadTime));
                Assert.That(track.NodeBaseParams.DirectParentId, Is.EqualTo(SegmentId));

                Assert.That(track.SourceList, Has.Count.EqualTo(1));
                Assert.That(track.SourceList[0].PluginId, Is.EqualTo(Wh3MusicHierarchyInformation.VorbisPluginId));
                Assert.That(track.SourceList[0].StreamType, Is.EqualTo(AKBKSourceType.Streaming));
                Assert.That(track.SourceList[0].AkMediaInformation.SourceId, Is.EqualTo(SourceId));

                Assert.That(track.PlaylistList, Has.Count.EqualTo(1));
                Assert.That(track.PlaylistList[0].SourceId, Is.EqualTo(SourceId));
                Assert.That(track.PlaylistList[0].SrcDuration, Is.EqualTo(DurationMs));
                Assert.That(track.NumSubTrack, Is.EqualTo(1), "the sub-track count is only written when there is a playlist");
            });
        }

        [Test]
        public void TheGeneratedSegmentSurvivesAWriteReadRoundTrip()
        {
            var segment = (CAkMusicSegment_V136)new CAkMusicSegmentGenerator_V136().GenerateHirc(CreateMusicSegment());

            var reloaded = new CAkMusicSegment_V136();
            reloaded.ReadHirc(new ByteChunk(segment.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(SegmentId));
                Assert.That(reloaded.Duration, Is.EqualTo(DurationMs));
                Assert.That(reloaded.ArrayMarkersList, Has.Count.EqualTo(3));
                Assert.That(reloaded.ArrayMarkersList[1].MarkerName, Is.EqualTo(Wh3MusicHierarchyInformation.CustomCueName));
                Assert.That(reloaded.MusicNodeParams.Children.ChildIds, Is.EqualTo(new[] { TrackId }));
            });
        }

        [Test]
        public void TheGeneratedTrackSurvivesAWriteReadRoundTrip()
        {
            var track = (CAkMusicTrack_V136)new CAkMusicTrackGenerator_V136().GenerateHirc(CreateMusicSegment());

            var reloaded = new CAkMusicTrack_V136();
            reloaded.ReadHirc(new ByteChunk(track.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(TrackId));
                Assert.That(reloaded.SourceList, Has.Count.EqualTo(1));
                Assert.That(reloaded.SourceList[0].AkMediaInformation.SourceId, Is.EqualTo(SourceId));
                Assert.That(reloaded.PlaylistList, Has.Count.EqualTo(1));
                Assert.That(reloaded.PlaylistList[0].SrcDuration, Is.EqualTo(DurationMs));
                Assert.That(reloaded.LookAheadTime, Is.EqualTo(Wh3MusicHierarchyInformation.TrackLookAheadTime));
            });
        }

        [Test]
        public void GeneratedRandomSequenceIsParentedToTheSwitchContainerAndListsItsSegments()
        {
            var ranSeq = (CAkMusicRanSeqCntr_V136)new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(CreateRandomSequence(SegmentId));
            var nodeParams = ranSeq.MusicTransNodeParams.MusicNodeParams;

            Assert.Multiple(() =>
            {
                Assert.That(ranSeq.Id, Is.EqualTo(RanSeqId));
                Assert.That(nodeParams.NodeBaseParams.DirectParentId, Is.EqualTo(SwitchContainerId));
                Assert.That(nodeParams.Children.ChildIds, Is.EqualTo(new[] { SegmentId }));
                Assert.That(ranSeq.MusicTransNodeParams.PlayList, Has.Count.EqualTo(1), "vanilla branches carry exactly one transition rule");
            });
        }

        [Test]
        public void ASingleSegmentPlaysAsAContinuousSequenceAndLoops()
        {
            var ranSeq = (CAkMusicRanSeqCntr_V136)new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(CreateRandomSequence(SegmentId));
            var root = ranSeq.PlayList.Single();

            Assert.Multiple(() =>
            {
                Assert.That(root.SegmentId, Is.EqualTo(0), "the root holds no segment of its own");
                Assert.That(root.RsType, Is.EqualTo(Wh3MusicHierarchyInformation.PlaylistTypeSequenceContinuous));
                Assert.That(root.PlayList, Has.Count.EqualTo(1));

                // With nothing to hand back to, the leaf is what loops - otherwise the music plays
                // once and the branch falls silent.
                Assert.That(root.PlayList[0].SegmentId, Is.EqualTo(SegmentId));
                Assert.That(root.PlayList[0].Loop, Is.EqualTo(1));
                Assert.That(root.PlayList[0].RsType, Is.EqualTo(Wh3MusicHierarchyInformation.PlaylistTypeNone));
            });
        }

        [Test]
        public void SeveralSegmentsPlayAsAWeightedRandomPickThatAvoidsRepeats()
        {
            var ranSeq = (CAkMusicRanSeqCntr_V136)new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(CreateRandomSequence(SegmentId, SegmentId + 10));
            var root = ranSeq.PlayList.Single();

            Assert.Multiple(() =>
            {
                Assert.That(root.RsType, Is.EqualTo(Wh3MusicHierarchyInformation.PlaylistTypeRandomStep));
                Assert.That(root.AvoidRepeatCount, Is.EqualTo(1));
                Assert.That(root.IsUsingWeight, Is.EqualTo(1));
                Assert.That(root.PlayList.Select(item => item.SegmentId), Is.EqualTo(new[] { SegmentId, SegmentId + 10 }));
                Assert.That(root.PlayList.Select(item => item.Loop), Is.All.EqualTo(0), "the root does the looping once there is more than one segment");
            });
        }

        [Test]
        public void TheGeneratedRandomSequenceSurvivesAWriteReadRoundTrip()
        {
            var ranSeq = (CAkMusicRanSeqCntr_V136)new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(CreateRandomSequence(SegmentId, SegmentId + 10));

            var reloaded = new CAkMusicRanSeqCntr_V136();
            reloaded.ReadHirc(new ByteChunk(ranSeq.WriteData()));

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Id, Is.EqualTo(RanSeqId));

                // The count on disk is every node in the flattened run, root included, so a root
                // over two segments is three.
                Assert.That(reloaded.NumPlaylistItems, Is.EqualTo(3));
                Assert.That(reloaded.PlayList.Single().PlayList.Select(item => item.SegmentId),
                    Is.EqualTo(new[] { SegmentId, SegmentId + 10 }));

                var rule = reloaded.MusicTransNodeParams.PlayList.Single();
                Assert.That(rule.SrcIdList.Single(), Is.EqualTo(Wh3MusicHierarchyInformation.AnyTransitionId));
                Assert.That(rule.AkMusicTransSrcRule.SyncType, Is.EqualTo(Wh3MusicHierarchyInformation.TransitionSyncTypeExitCue));
                Assert.That(rule.AkMusicTransDstRule.PlayPreEntry, Is.EqualTo(1));
            });
        }

        static MusicRandomSequence CreateRandomSequence(params uint[] segmentIds)
        {
            return new MusicRandomSequence
            {
                Id = RanSeqId,
                DirectParentId = SwitchContainerId,
                PlaylistRootItemId = 12345,
                Segments = [.. segmentIds.Select((id, index) => new MusicPlaylistEntry
                {
                    SegmentId = id,
                    PlaylistItemId = 12346 + index
                })]
            };
        }


        static MusicSegment CreateMusicSegment()
        {
            return new MusicSegment
            {
                Id = SegmentId,
                TrackId = TrackId,
                DirectParentId = RanSeqId,
                SourceId = SourceId,
                InMemoryMediaSize = 4238,
                DurationMs = DurationMs,
                Language = Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)
            };
        }
    }
}
