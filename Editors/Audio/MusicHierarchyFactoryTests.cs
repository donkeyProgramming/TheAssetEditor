using System;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;

namespace Test.Audio
{
    // The hierarchy a music branch points at, as built when a modder picks wavs for a music Action
    // Event. Everything here has to be reproducible from the audio project alone, since the project
    // is saved and recompiled rather than rebuilt from the UI.
    internal class MusicHierarchyFactoryTests
    {
        const uint SwitchContainerId = 698158058;
        const string SubcultureStateGroup = "WH3_Campaign_Subcultures";

        [Test]
        public void EachAudioFileBecomesASegmentUnderTheBranch()
        {
            var branch = CreateBranch("Araby", "empire_theme_01.wav", "empire_theme_02.wav");

            Assert.Multiple(() =>
            {
                Assert.That(branch.MusicSegments, Has.Count.EqualTo(2));
                Assert.That(branch.MusicRandomSequence.Segments, Has.Count.EqualTo(2));

                foreach (var musicSegment in branch.MusicSegments)
                    Assert.That(musicSegment.DirectParentId, Is.EqualTo(branch.MusicRandomSequence.Id));

                // The random sequence hangs off the vanilla container, which is what the merged
                // decision tree needs it to say.
                Assert.That(branch.MusicRandomSequence.DirectParentId, Is.EqualTo(SwitchContainerId));
                Assert.That(branch.MusicRandomSequence.StateName, Is.EqualTo("Araby"));
            });
        }

        [Test]
        public void TheSegmentsPointAtTheAudioTheyWereBuiltFrom()
        {
            var audioFiles = CreateAudioFiles("empire_theme_01.wav", "empire_theme_02.wav");
            var branch = new MusicHierarchyFactory().CreateMusicBranch([], SwitchContainerId, SubcultureStateGroup, "Araby", audioFiles, "english(uk)");

            Assert.Multiple(() =>
            {
                Assert.That(branch.MusicSegments[0].SourceId, Is.EqualTo(audioFiles[0].Id));
                Assert.That(branch.MusicSegments[1].SourceId, Is.EqualTo(audioFiles[1].Id));
                Assert.That(branch.MusicSegments[0].Language, Is.EqualTo("english(uk)"));
            });
        }

        [Test]
        public void EveryGeneratedIdIsDistinctAndRecordedAsUsed()
        {
            // A track has no audio project item of its own, so its id is easy to leave out of the
            // used set - and a reused id puts two hircs in the bank under one id, which the game
            // resolves to whichever it read last.
            var usedHircIds = new HashSet<uint>();
            var branch = new MusicHierarchyFactory()
                .CreateMusicBranch(usedHircIds, SwitchContainerId, SubcultureStateGroup, "Araby", CreateAudioFiles("a.wav", "b.wav"), "sfx");

            var ids = new List<uint> { branch.MusicRandomSequence.Id };
            foreach (var musicSegment in branch.MusicSegments)
            {
                ids.Add(musicSegment.Id);
                ids.Add(musicSegment.TrackId);
            }

            Assert.Multiple(() =>
            {
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
                foreach (var id in ids)
                    Assert.That(usedHircIds, Contains.Item(id));
            });
        }

        [Test]
        public void ThePlaylistItemIdsAreUniqueWithinTheContainerAndNeverZero()
        {
            // Zero means no item to Wwise, and the root has to be distinguishable from its leaves.
            var branch = CreateBranch("Araby", "a.wav", "b.wav", "c.wav");

            var playlistItemIds = branch.MusicRandomSequence.Segments
                .Select(entry => entry.PlaylistItemId)
                .Append(branch.MusicRandomSequence.PlaylistRootItemId)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(playlistItemIds.Distinct().Count(), Is.EqualTo(playlistItemIds.Count));
                Assert.That(playlistItemIds, Has.None.EqualTo(0));
            });
        }

        static MusicBranchResult CreateBranch(string stateName, params string[] wavFileNames)
        {
            return new MusicHierarchyFactory()
                .CreateMusicBranch([], SwitchContainerId, SubcultureStateGroup, stateName, CreateAudioFiles(wavFileNames), "sfx");
        }

        static List<AudioFile> CreateAudioFiles(params string[] wavFileNames)
        {
            return [.. wavFileNames.Select((wavFileName, index) => new AudioFile(
                Guid.NewGuid(),
                (uint)(1000 + index),
                wavFileName,
                $"audio\\wwise\\{wavFileName}"))];
        }
    }
}

