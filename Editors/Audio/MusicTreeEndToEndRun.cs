using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Test.Audio
{
    /// <summary>
    /// A culture whose battle music is a decision tree rather than one playlist, built against a real
    /// WH3 install and read back out of the spliced vanilla banks: the tree splits by the part of the
    /// game, then by the battle result before the battle and by the phase during it.
    ///
    /// The unit tests cover the path building on made-up containers. This covers it on the game's own
    /// six-level in-battle container, and the compile in between. It looks paths up the way Wwise does
    /// - the exact key at each level, else the default - but it cannot show the game plays them.
    /// </summary>
    [Explicit("End to end run, needs a WH3 install and a wav on disk, run manually")]
    internal class MusicTreeEndToEndRun
    {
        const string WavDiskFilePath = @"C:\Users\rob\Desktop\tw modding\projects\araby audio projects\music\241 Adrian von Ziegler - Hual Hadi.wav";
        const string BattleEvent = "music_b_faction_araby";
        const uint PreBattleContainerId = 26264058;
        const uint InBattleContainerId = 67383790;

        [SetUp]
        public void RequireInstall() => VanillaBankReader.RequireOnDisk(VanillaBankReader.GameDirectory, WavDiskFilePath);

        [Test]
        public void EachPartOfTheTreeIsReachedWhereItSaysInTheShippedBanks()
        {
            using var provider = VanillaBankReader.CreateProvider(VanillaBankReader.GameDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(VanillaBankReader.GameDirectory, "data", packName));
            packFileService.CreateNewPackFileContainer("araby_tree", PackFileVersion.PFH5, PackFileCAType.MOD, setEditablePack: true);

            var everywhereWav = ImportWav(packFileService, "araby_everywhere.wav");
            var specialWav = ImportWav(packFileService, "araby_special.wav");

            var service = provider.GetRequiredService<IMusicAudioProjectService>();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, new MusicCulture
            {
                AudioState = "Araby",
                SubcultureKey = "ovn_sc_arb_araby",
                Slots = [new MusicCultureSlot { EventPrefix = "music_b_faction_" }]
            });

            // Everywhere: one wav. A win before or after the battle, and the action phase during it: another.
            var root = service.GetTree(audioProject, BattleEvent).Root;
            service.SetNodeAudio(audioProject, BattleEvent, root.Id, [everywhereWav]);
            var preBattle = service.AddBranch(audioProject, BattleEvent, root.Id, Wh3MusicTreeLevelInformation.ContainerLevel, PreBattleContainerId.ToString());
            var inBattle = service.AddBranch(audioProject, BattleEvent, root.Id, Wh3MusicTreeLevelInformation.ContainerLevel, InBattleContainerId.ToString());
            var win = service.AddBranch(audioProject, BattleEvent, preBattle, "Battle_Result_State", "win");
            var action = service.AddBranch(audioProject, BattleEvent, inBattle, "Battle_Music_Intensity", "_5_Battle_Music_Action");
            service.SetNodeAudio(audioProject, BattleEvent, win, [specialWav]);
            service.SetNodeAudio(audioProject, BattleEvent, action, [specialWav]);

            const string filePath = MusicAudioProjectService.ProjectFilePath;
            provider.GetRequiredService<IAudioProjectFileService>().Save(audioProject, Path.GetFileName(filePath), filePath);
            var compiledProject = audioProject.Clean();
            provider.GetRequiredService<IMusicProjectCompilerService>().Compile(compiledProject, Path.GetFileName(filePath));

            uint SequenceFor(uint containerId, int nodeId) => compiledProject.Music.RandomSequences
                .Single(sequence => sequence.DirectParentId == containerId && sequence.TreeNodeId == nodeId).Id;

            var preBattleContainer = ShippedContainer(packFileService, PreBattleContainerId);
            var inBattleContainer = ShippedContainer(packFileService, InBattleContainerId);

            Assert.Multiple(() =>
            {
                Assert.That(Lookup(preBattleContainer, "win", "Araby"), Is.EqualTo(SequenceFor(PreBattleContainerId, win)));
                Assert.That(Lookup(preBattleContainer, "lose", "Araby"), Is.EqualTo(SequenceFor(PreBattleContainerId, root.Id)));
                Assert.That(Lookup(preBattleContainer, "draw", "Araby"), Is.EqualTo(SequenceFor(PreBattleContainerId, root.Id)));
                Assert.That(Lookup(preBattleContainer, "win", "Cathay"), Is.Not.EqualTo(0u), "vanilla's own cultures still resolve");

                Assert.That(Lookup(inBattleContainer, "_5_Battle_Music_Action", null, "Araby", "C", "_01", "Winning"), Is.EqualTo(SequenceFor(InBattleContainerId, action)));
                Assert.That(Lookup(inBattleContainer, "_5_Battle_Music_Action", null, "Araby", "Gb", "_07", "Losing"), Is.EqualTo(SequenceFor(InBattleContainerId, action)));
                Assert.That(Lookup(inBattleContainer, "_1_Battle_Music_Reveal", null, "Araby", "C", "_01", "Winning"), Is.EqualTo(SequenceFor(InBattleContainerId, root.Id)));
                Assert.That(Lookup(inBattleContainer, "_3_Battle_Music_Marching", null, "Araby", "Eb", "_03", "Full_Mix"), Is.EqualTo(SequenceFor(InBattleContainerId, root.Id)));

                // The pre-battle sequence for the in-battle branch's root would never be reached there,
                // so it is not shipped: each container carries what its tree reaches and nothing else.
                Assert.That(compiledProject.Music.RandomSequences.Any(sequence => sequence.DirectParentId == PreBattleContainerId && sequence.TreeNodeId == action), Is.False);
            });

            foreach (var sequence in compiledProject.Music.RandomSequences)
                Console.WriteLine($"{Wh3MusicTreeLevelInformation.GetContainerName(sequence.DirectParentId)}: node {sequence.TreeNodeId} -> sequence {sequence.Id}, " +
                    $"{string.Join(", ", compiledProject.Music.GetSegments(sequence).Select(segment => compiledProject.GetAudioFile(segment.SourceId)?.WavPackFileName))}");
        }

        /// <summary>
        /// The other three kinds of tree, each giving one part its own wav: the campaign theme a won
        /// resolution screen, the ambient fragments the key of C, and the percussion pulse the key of
        /// Eb. Read back out of the shipped banks: the resolution container's decision tree, the
        /// fragment switches down to their Sounds, and the vanilla pulse tracks' new sub-tracks.
        /// </summary>
        [Test]
        public void TheCampaignAndAdaptiveTreesAreBuiltTheWayTheGameIsLaidOut()
        {
            using var provider = VanillaBankReader.CreateProvider(VanillaBankReader.GameDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(VanillaBankReader.GameDirectory, "data", packName));
            packFileService.CreateNewPackFileContainer("araby_campaign_tree", PackFileVersion.PFH5, PackFileCAType.MOD, setEditablePack: true);

            var everywhereWav = ImportWav(packFileService, "araby_everywhere.wav");
            var specialWav = ImportWav(packFileService, "araby_special.wav");

            const string themeEvent = "music_c_subculture_araby";
            const string fragmentEvent = "music_c_ams_araby";
            const string percussionEvent = "music_c_ams_pulse_perc_araby";

            var service = provider.GetRequiredService<IMusicAudioProjectService>();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, new MusicCulture
            {
                AudioState = "Araby",
                SubcultureKey = "ovn_sc_arb_araby",
                Slots =
                [
                    new MusicCultureSlot { EventPrefix = "music_c_subculture_" },
                    new MusicCultureSlot { EventPrefix = "music_c_ams_" },
                    new MusicCultureSlot { EventPrefix = "music_c_ams_pulse_perc_" }
                ]
            });

            int GiveRootAndOneBranch(string eventName, params (string Level, string Value)[] path)
            {
                var root = service.GetTree(audioProject, eventName).Root;
                service.SetNodeAudio(audioProject, eventName, root.Id, [everywhereWav]);
                var nodeId = root.Id;
                foreach (var (level, value) in path)
                    nodeId = service.AddBranch(audioProject, eventName, nodeId, level, value);
                service.SetNodeAudio(audioProject, eventName, nodeId, [specialWav]);
                return nodeId;
            }

            var won = GiveRootAndOneBranch(themeEvent,
                (Wh3MusicTreeLevelInformation.ContainerLevel, Wh3MusicHierarchyInformation.CampaignResolutionMusicSwitchContainerId.ToString()),
                ("WH3_Campaign_Resolution_State", "Won"));
            GiveRootAndOneBranch(fragmentEvent, (Wh3MusicTreeLevelInformation.AmsKeyLevel, "C"));
            GiveRootAndOneBranch(percussionEvent, (Wh3MusicTreeLevelInformation.AmsKeyLevel, "Eb"));

            const string filePath = MusicAudioProjectService.ProjectFilePath;
            provider.GetRequiredService<IAudioProjectFileService>().Save(audioProject, Path.GetFileName(filePath), filePath);
            var compiledProject = audioProject.Clean();
            provider.GetRequiredService<IMusicProjectCompilerService>().Compile(compiledProject, Path.GetFileName(filePath));

            var everywhereId = compiledProject.GetAudioFile(everywhereWav).Id;
            var specialId = compiledProject.GetAudioFile(specialWav).Id;
            var themeRoot = service.GetTree(audioProject, themeEvent).Root.Id;
            uint SequenceFor(uint containerId, int nodeId) => compiledProject.Music.RandomSequences
                .Single(sequence => sequence.DirectParentId == containerId && sequence.TreeNodeId == nodeId).Id;

            var shipped = ShippedHircs(packFileService);
            var mapContainer = shipped.OfType<CAkMusicSwitchCntr_V136>().Single(hirc => hirc.Id == 698158058);
            var resolutionContainer = shipped.OfType<CAkMusicSwitchCntr_V136>().Single(hirc => hirc.Id == Wh3MusicHierarchyInformation.CampaignResolutionMusicSwitchContainerId);

            // The fragments, walked from the culture's key switch down to each Sound's source.
            var amsFragment = compiledProject.Music.AmsFragments.Single();
            var keySwitch = shipped.OfType<CAkSwitchCntr_V136>().Single(hirc => hirc.Id == amsFragment.KeySwitchContainerId);
            var audioRepository = provider.GetRequiredService<Editors.Audio.Shared.Storage.IAudioRepository>();
            List<uint> SourcesFor(string keyName, string callOrResponse)
            {
                var callResponseId = Packages(keySwitch).Single(package => package.SwitchId == WwiseHash.Compute(keyName)).NodeIdList.Single();
                var callResponse = shipped.OfType<CAkSwitchCntr_V136>().Single(hirc => hirc.Id == callResponseId);
                var target = Packages(callResponse).Single(package => package.SwitchId == WwiseHash.Compute(callOrResponse)).NodeIdList.Single();
                return SourcesUnder(shipped, target);
            }

            // The percussion pulse: the source of Araby's sub-track in each vanilla track.
            var layout = provider.GetRequiredService<IAmsMusicLayout>();
            var arabyState = WwiseHash.Compute("araby");
            var pulseSources = layout.GetPulseSlots("WH3_AMS_Pulse_Percussion_Options").Select(slot =>
            {
                var track = shipped.OfType<CAkMusicTrack_V136>().Single(hirc => hirc.Id == slot.Track.Id);
                var subTrack = track.SwitchParams!.SwitchAssoc.IndexOf(arabyState);
                return (slot.Key, slot.Variation, Source: track.PlaylistList.Single(clip => clip.TrackId == subTrack).SourceId);
            }).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(Lookup(mapContainer, "Araby", null), Is.EqualTo(SequenceFor(698158058, themeRoot)));
                Assert.That(Lookup(resolutionContainer, "Araby", null, "Won"), Is.EqualTo(SequenceFor(Wh3MusicHierarchyInformation.CampaignResolutionMusicSwitchContainerId, won)));
                Assert.That(Lookup(resolutionContainer, "Araby", null, "Lost"), Is.EqualTo(SequenceFor(Wh3MusicHierarchyInformation.CampaignResolutionMusicSwitchContainerId, themeRoot)));
                Assert.That(Lookup(resolutionContainer, "Cathay", null, "Won"), Is.Not.EqualTo(0u), "vanilla's own cultures still resolve");

                foreach (var keyName in new[] { "C_01", "C_02", "C_03" })
                    foreach (var callOrResponse in new[] { AmsMusicLayout.Call, AmsMusicLayout.Response })
                        Assert.That(SourcesFor(keyName, callOrResponse), Is.EqualTo(new[] { specialId }), $"{keyName} {callOrResponse}");
                foreach (var keyName in new[] { "Eb_02", "Gb_03" })
                    Assert.That(SourcesFor(keyName, AmsMusicLayout.Call), Is.EqualTo(new[] { everywhereId }), keyName);
                Assert.That(Packages(keySwitch).Any(package => package.SwitchId == WwiseHash.Compute("None")), Is.False, "None stays silent");

                Assert.That(pulseSources, Has.Count.EqualTo(9));
                foreach (var (key, variation, source) in pulseSources)
                    Assert.That(source, Is.EqualTo(key == "Eb" ? specialId : everywhereId), $"pulse in {key}, variation {variation}");
            });

            Console.WriteLine($"fragments: {amsFragment.Keys.Count} keys over {amsFragment.CallResponseSwitches.Count} call/response switch(es)");
            foreach (var (key, variation, source) in pulseSources)
                Console.WriteLine($"pulse {key} {variation}: {(source == specialId ? "special" : "everywhere")}");
        }

        static List<Shared.GameFormats.Wwise.Hirc.HircItem> ShippedHircs(IPackFileService packFileService) =>
            [.. packFileService.GetEditablePack().GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .SelectMany(file => BnkFile.CreateFromBytes(file.Value.DataSource.ReadData(), file.Key, false).HircChunk.HircItems)];

        static IEnumerable<CAkSwitchCntr_V136.CAkSwitchPackage_V136> Packages(CAkSwitchCntr_V136 container) =>
            container.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>();

        static List<uint> SourcesUnder(List<Shared.GameFormats.Wwise.Hirc.HircItem> hircs, uint id) => hircs.FirstOrDefault(hirc => hirc.Id == id) switch
        {
            Shared.GameFormats.Wwise.Hirc.ICAkSound sound => [sound.GetSourceId()],
            CAkRanSeqCntr_V136 container => [.. container.Children.ChildIds.SelectMany(child => SourcesUnder(hircs, child))],
            _ => []
        };

        static string ImportWav(IPackFileService packFileService, string wavPackFileName)
        {
            PackFileUtil.LoadFileFromDisk(packFileService, new PackFileUtil.FileRef(WavDiskFilePath, "audio\\wwise", wavPackFileName));
            return $"audio\\wwise\\{wavPackFileName}";
        }

        static CAkMusicSwitchCntr_V136 ShippedContainer(IPackFileService packFileService, uint containerId)
        {
            var banks = packFileService.GetEditablePack().GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase));
            foreach (var (path, file) in banks)
            {
                var hircs = BnkFile.CreateFromBytes(file.DataSource.ReadData(), path, false).HircChunk.HircItems;
                if (hircs.FirstOrDefault(hirc => hirc.Id == containerId) is CAkMusicSwitchCntr_V136 container)
                    return container;
            }

            Assert.Fail($"No shipped .bnk holds container {containerId}");
            return null!;
        }

        /// <summary>What Wwise picks for a runtime combination: the exact key at each level, otherwise
        /// the default. A null value is a level the game leaves unset.</summary>
        static uint Lookup(CAkMusicSwitchCntr_V136 container, params string?[] values)
        {
            var node = container.AkDecisionTree.DecisionTree;
            foreach (var value in values)
            {
                var key = value == null ? 0 : WwiseHash.Compute(value);
                node = node.Nodes.FirstOrDefault(child => child.Key == key && key != 0)
                    ?? node.Nodes.FirstOrDefault(child => child.Key == 0);
                if (node == null)
                    return 0;
            }
            return node.AudioNodeId;
        }
    }
}
