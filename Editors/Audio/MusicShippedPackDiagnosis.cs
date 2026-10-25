using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.GameFormats.MusicDat;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    // Why the shipped pack is silent in game. Everything here reads what is actually on disk -
    // the vanilla packs and the pack the end to end run wrote - rather than the in memory model.
    [Explicit("Diagnosis, needs a WH3 install and the end to end run's output pack")]
    internal class MusicShippedPackDiagnosis
    {
        const string GameDirectory = @"D:\SteamLibrary\steamapps\common\Total War WARHAMMER III";
        static string ModPackPath => Path.Combine(Path.GetTempPath(), "ArabyMusicRun", "araby_music.pack");

        /// <summary>Which vanilla .bnk holds each hirc the mod re-emits. The testing .bnk only
        /// overrides vanilla if it is named after the .bnk the hirc actually came from.</summary>
        [Test]
        public void WhichVanillaBankHoldsWhat()
        {
            uint[] wanted = [698158058, 26264058, 418295225];

            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    List<uint> found;
                    try
                    {
                        var bnk = BnkFile.CreateFromBytes(bytes, path, false);
                        found = bnk.HircChunk.HircItems
                            .Where(hirc => wanted.Contains(hirc.Id))
                            .Select(hirc => hirc.Id)
                            .ToList();
                    }
                    catch
                    {
                        continue;
                    }

                    if (found.Count != 0)
                        Console.WriteLine($"{packName}  {path}  ->  {string.Join(", ", found)}");
                }
            }
        }

        /// <summary>The new culture's branch walked to the wem, next to a vanilla culture's, out of
        /// the .bnk bytes that actually shipped. Anything the mod gets structurally wrong shows up
        /// as a difference against the vanilla column.</summary>
        [Test]
        public void CompareTheArabyChainWithVanilla()
        {
            var hircs = new Dictionary<uint, HircItem>();

            // Vanilla first, then the mod, so the mod's copy of a shared id wins - the same as the
            // load order the .bnk names buy.
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (File.Exists(packPath))
                    IndexBanks(hircs, VanillaBankReader.ReadMusicBanks(packPath));
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            // The merging .bnk never ships and carries the mod's branches alone, so indexing it here
            // would shadow the fully merged container in the testing .bnk and hide vanilla's branches.
            IndexBanks(hircs, modPack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase)
                    && !file.Key.Contains("_for_merging", StringComparison.OrdinalIgnoreCase))
                .Select(file => (file.Key, file.Value.DataSource.ReadData()))
                .ToList());

            foreach (var (containerId, label) in new[] { (698158058u, "campaign subculture"), (26264058u, "battle culture") })
            {
                var container = (CAkMusicSwitchCntr_V136)hircs[containerId];
                Console.WriteLine($"\n################ {label} container {containerId} ################");
                Console.WriteLine($"tree depth {container.TreeDepth}, arguments {string.Join(", ", container.Arguments.Select(a => a.GroupId))}");

                Console.WriteLine($"top level keys ({container.AkDecisionTree.DecisionTree.Nodes.Count}): " +
                    string.Join(", ", container.AkDecisionTree.DecisionTree.Nodes.Select(node => node.Key)));

                foreach (var culture in new[] { "araby", "cathay" })
                {
                    Console.WriteLine($"\n---- {culture} ----");
                    DumpLeaves(hircs, container.AkDecisionTree.DecisionTree, WwiseHash.Compute(culture), "", []);
                }
            }
        }

        /// <summary>Every branch key in the two containers, named where the shipped banks name the
        /// hash. What the game sets the State to has to be one of these, or the tree falls through
        /// to its default and vanilla plays.</summary>
        [Test]
        public void WhatTheContainersBranchOn()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));

            var repository = provider.GetRequiredService<Editors.Audio.Shared.Storage.IAudioRepository>();
            repository.Load(["sfx"]);

            foreach (var containerId in new uint[] { 698158058, 26264058 })
            {
                var container = repository.GetHircs(containerId).OfType<CAkMusicSwitchCntr_V136>().Single(x => x.IsCA);
                Console.WriteLine($"\n################ {containerId} ################");
                Console.WriteLine("arguments: " + string.Join(", ",
                    container.Arguments.Select(argument => $"{Name(repository, argument.GroupId)}({argument.GroupId})")));

                DumpKeys(repository, container.AkDecisionTree.DecisionTree, "  ");
            }
        }

        static void DumpKeys(Editors.Audio.Shared.Storage.IAudioRepository repository, AkDecisionTree_V136.Node_V136 node, string indent)
        {
            foreach (var child in node.Nodes)
            {
                Console.WriteLine($"{indent}{Name(repository, child.Key)}({child.Key})" +
                    (child.Nodes.Count == 0 ? $" -> {child.AudioNodeId}" : ""));
                DumpKeys(repository, child, indent + "  ");
            }
        }

        static string Name(Editors.Audio.Shared.Storage.IAudioRepository repository, uint hash)
        {
            var name = repository.GetNameFromId(hash, out var found);
            return found ? name : "?";
        }

        /// <summary>The State each event sets, next to the branch key that State has to hit. These
        /// are generated from two different strings, so a case difference between them is silence.</summary>
        [Test]
        public void WhatTheEventsSetAgainstWhatTheTreeExpects()
        {
            Console.WriteLine($"hash('araby')  = {WwiseHash.Compute("araby")}");
            Console.WriteLine($"hash('Araby')  = {WwiseHash.Compute("Araby")}");
            Console.WriteLine($"hash('cathay') = {WwiseHash.Compute("cathay")}");
            Console.WriteLine($"hash('Cathay') = {WwiseHash.Compute("Cathay")}  (vanilla tree key is 2752566535)");

            var hircs = new Dictionary<uint, HircItem>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (File.Exists(packPath))
                    IndexBanks(hircs, VanillaBankReader.ReadMusicBanks(packPath));
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            IndexBanks(hircs, modPack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .Select(file => (file.Key, file.Value.DataSource.ReadData()))
                .ToList());

            foreach (var eventName in new[]
            {
                "music_c_subculture_cathay", "music_c_subculture_araby",
                "music_b_faction_cathay", "music_b_faction_araby",
                "music_c_ams_pulse_perc_cathay", "music_c_ams_pulse_perc_araby",
            })
            {
                Console.WriteLine($"\n{eventName} ({WwiseHash.Compute(eventName)})");
                if (!hircs.TryGetValue(WwiseHash.Compute(eventName), out var hirc) || hirc is not CAkEvent_V136 akEvent)
                {
                    Console.WriteLine("  !! not in any .bnk");
                    continue;
                }

                foreach (var actionId in akEvent.GetActionIds())
                {
                    if (!hircs.TryGetValue(actionId, out var actionHirc) || actionHirc is not CAkAction_V136 action)
                    {
                        Console.WriteLine($"  !! action {actionId} missing");
                        continue;
                    }

                    Console.WriteLine($"  {action.ActionType} stateGroup {action.StateActionParams?.StateGroupId} -> state {action.IdExt}");
                }
            }
        }

        /// <summary>Each .bnk the mod ships, and what it says about the two vanilla containers. The
        /// testing bank has to carry vanilla's branches; the merging bank is only the mod's.</summary>
        [Test]
        public void WhatEachShippedBankSaysAboutTheContainers()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            foreach (var file in modPack.GetAllFiles().Where(x => x.Key.EndsWith(".bnk")).OrderBy(x => x.Key))
            {
                var bnk = BnkFile.CreateFromBytes(file.Value.DataSource.ReadData(), file.Key, false);
                Console.WriteLine($"\n================ {file.Key} ================");
                Console.WriteLine("hircs: " + string.Join(", ", bnk.HircChunk.HircItems
                    .GroupBy(hirc => hirc.HircType)
                    .Select(group => $"{group.Key} x{group.Count()}")));

                foreach (var container in bnk.HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>())
                {
                    var keys = container.AkDecisionTree.DecisionTree.Nodes;
                    Console.WriteLine($"  Music_Switch {container.Id}: depth {container.TreeDepth}, " +
                        $"{keys.Count} top level keys -> {string.Join(", ", keys.Select(node => node.Key))}");

                    var children = container.MusicTransNodeParams.MusicNodeParams.Children;
                    Console.WriteLine($"    children ({children.NumChilds} declared, {children.ChildIds.Count} listed): " +
                        string.Join(", ", children.ChildIds));

                    var leaves = new List<uint>();
                    CollectLeaves(container.AkDecisionTree.DecisionTree, leaves);
                    var orphans = leaves.Distinct().Where(id => id != 0 && !children.ChildIds.Contains(id)).ToList();
                    Console.WriteLine(orphans.Count == 0
                        ? "    every leaf is a declared child"
                        : $"    !! leaves that are NOT declared children: {string.Join(", ", orphans)}");
                }

                foreach (var container in bnk.HircChunk.HircItems.OfType<CAkSwitchCntr_V136>())
                    Console.WriteLine($"  SwitchContainer {container.Id}: {container.SwitchList.Count} switches, " +
                        $"{container.Children.ChildIds.Count} children");

                foreach (var track in bnk.HircChunk.HircItems.OfType<CAkMusicTrack_V136>().Where(x => x.TrackType == CAkMusicTrack_V136.SwitchTrackType))
                    Console.WriteLine($"  switch Music_Track {track.Id}: {track.SwitchParams!.SwitchAssoc.Count} sub-tracks");
            }
        }

        static void CollectLeaves(AkDecisionTree_V136.Node_V136 node, List<uint> leaves)
        {
            foreach (var child in node.Nodes)
            {
                if (child.Nodes.Count == 0)
                    leaves.Add(child.AudioNodeId);
                else
                    CollectLeaves(child, leaves);
            }
        }

        static void IndexBanks(Dictionary<uint, HircItem> hircs, List<(string Path, byte[] Bytes)> banks)
        {
            foreach (var (path, bytes) in banks)
            {
                try
                {
                    foreach (var hirc in BnkFile.CreateFromBytes(bytes, path, false).HircChunk.HircItems)
                        hircs[hirc.Id] = hirc;
                }
                catch
                {
                }
            }
        }

        /// <summary>Every leaf reachable through a node keyed on the culture, dumped from the leaf
        /// down. A branch can sit at any depth, so the key is looked for on the way down.</summary>
        static void DumpLeaves(Dictionary<uint, HircItem> hircs, AkDecisionTree_V136.Node_V136 node, uint key, string path, HashSet<uint> seen)
        {
            foreach (var child in node.Nodes)
            {
                var matched = child.Key == key || seen.Count != 0;
                var childPath = $"{path}/{child.Key}";

                if (child.Nodes.Count == 0)
                {
                    if (matched)
                    {
                        Console.WriteLine($"  leaf {childPath} -> {child.AudioNodeId}");
                        DumpTarget(hircs, child.AudioNodeId, "    ", 0);
                    }
                    continue;
                }

                DumpLeaves(hircs, child, key, childPath, matched ? [key] : seen);
            }
        }

        static void DumpTarget(Dictionary<uint, HircItem> hircs, uint id, string indent, int depth)
        {
            if (depth > 6)
                return;

            if (!hircs.TryGetValue(id, out var hirc))
            {
                Console.WriteLine($"{indent}!! {id} is in no .bnk");
                return;
            }

            Console.WriteLine($"{indent}{hirc.HircType} {id}");

            switch (hirc)
            {
                case CAkMusicRanSeqCntr_V136 ranSeq:
                    Console.WriteLine($"{indent}  children: {string.Join(", ", ranSeq.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)}");
                    DumpPlaylist(hircs, ranSeq.PlayList, indent + "  ", depth);
                    break;

                case CAkMusicSegment_V136 segment:
                    Console.WriteLine($"{indent}  duration {segment.Duration}ms, markers {segment.ArrayMarkersList.Count}: " +
                        string.Join(", ", segment.ArrayMarkersList.Select(marker => $"{marker.Position}ms '{marker.MarkerName}'")));
                    foreach (var childId in segment.MusicNodeParams.Children.ChildIds)
                        DumpTarget(hircs, childId, indent + "  ", depth + 1);
                    break;

                case CAkMusicTrack_V136 track:
                    Console.WriteLine($"{indent}  trackType {track.TrackType}, subTracks {track.NumSubTrack}, lookAhead {track.LookAheadTime}");
                    foreach (var source in track.SourceList)
                        Console.WriteLine($"{indent}  source {source.AkMediaInformation.SourceId} " +
                            $"plugin {source.PluginId} stream {source.StreamType} inMemory {source.AkMediaInformation.InMemoryMediaSize}");
                    foreach (var clip in track.PlaylistList)
                        Console.WriteLine($"{indent}  clip sub{clip.TrackId} src {clip.SourceId} " +
                            $"playAt {clip.PlayAt} begin {clip.BeginTrimOffset} end {clip.EndTrimOffset} dur {clip.SrcDuration}");
                    break;
            }
        }

        static void DumpPlaylist(Dictionary<uint, HircItem> hircs, List<CAkMusicRanSeqCntr_V136.AkMusicRanSeqPlaylistItem_V136> items, string indent, int depth)
        {
            foreach (var item in items)
            {
                Console.WriteLine($"{indent}playlist segment {item.SegmentId} rsType {item.RsType} loop {item.Loop} weight {item.Weight} children {item.NumChildren}");
                if (item.SegmentId != 0)
                    DumpTarget(hircs, item.SegmentId, indent + "  ", depth + 1);
                DumpPlaylist(hircs, item.PlayList, indent + "  ", depth);
            }
        }

        /// <summary>Which .bnk holds each child of the two containers, in vanilla. If vanilla always
        /// keeps a music child in the same .bnk as its parent then a mod that splits them across two
        /// .bnks is not doing what the game does, however correct the ids look.</summary>
        [Test]
        public void WhetherVanillaKeepsMusicChildrenInTheParentsBank()
        {
            var bankByHirc = new Dictionary<uint, string>();
            var containersByBank = new Dictionary<uint, (string Bank, List<uint> Children)>();

            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    BnkFile bnk;
                    try
                    {
                        bnk = BnkFile.CreateFromBytes(bytes, path, false);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var hirc in bnk.HircChunk?.HircItems ?? [])
                    {
                        bankByHirc[hirc.Id] = path;
                        if (hirc is CAkMusicSwitchCntr_V136 container && (container.Id == 698158058 || container.Id == 26264058))
                            containersByBank[container.Id] = (path, container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds);
                    }
                }
            }

            foreach (var (containerId, (bank, children)) in containersByBank)
            {
                Console.WriteLine($"\n################ {containerId} lives in {bank} ################");
                foreach (var group in children.GroupBy(childId => bankByHirc.TryGetValue(childId, out var b) ? b : "!! nowhere"))
                    Console.WriteLine($"  {group.Count()} of {children.Count} children in {group.Key}");
            }
        }

        /// <summary>Where vanilla keeps its music media, next to where the mod put its own. A track
        /// that streams is silent unless the game finds the .wem where it expects it, and the mod
        /// only ever writes to the sfx path the sound banks use.</summary>
        [Test]
        public void WhereTheMusicMediaLives()
        {
            foreach (var packName in new[] { "audio_base_m.pack", "audio_base.pack", "audio_base_bnk.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                using var vanillaProvider = VanillaBankReader.CreateProvider(GameDirectory);
                var pack = VanillaBankReader.OpenPack(vanillaProvider, packPath);

                var wems = pack.GetAllFiles()
                    .Where(file => file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Console.WriteLine($"\n================ {packName} ================");
                Console.WriteLine($"{wems.Count} wem(s)");
                foreach (var folder in wems.GroupBy(file => Path.GetDirectoryName(file.Key)).OrderByDescending(group => group.Count()))
                    Console.WriteLine($"  {folder.Key}  x{folder.Count()}   e.g. {folder.First().Key}");
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            Console.WriteLine($"\n================ the mod's pack ================");
            foreach (var file in modPack.GetAllFiles().OrderBy(file => file.Key))
                Console.WriteLine($"  {file.Key}  ({file.Value.DataSource.Size} bytes)");
        }

        /// <summary>A vanilla music track's source, so the mod's can be read against it rather than
        /// against an assumption about how music is packaged.</summary>
        [Test]
        public void HowVanillaMusicTracksCarryTheirAudio()
        {
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    BnkFile bnk;
                    try
                    {
                        bnk = BnkFile.CreateFromBytes(bytes, path, false);
                    }
                    catch
                    {
                        continue;
                    }

                    var tracks = (bnk.HircChunk?.HircItems ?? []).OfType<CAkMusicTrack_V136>().ToList();
                    if (tracks.Count == 0)
                        continue;

                    var sources = tracks.SelectMany(track => track.SourceList).ToList();
                    Console.WriteLine($"\n{packName}  {path}: {tracks.Count} track(s), {sources.Count} source(s)");
                    foreach (var group in sources.GroupBy(source => (source.StreamType, source.PluginId)))
                        Console.WriteLine($"  {group.Key.StreamType} plugin {group.Key.PluginId} x{group.Count()}" +
                            $"   e.g. source {group.First().AkMediaInformation.SourceId} inMemory {group.First().AkMediaInformation.InMemoryMediaSize}");
                }
            }
        }

        /// <summary>The size a music source declares in the .bnk against the size of the .wem it
        /// names. The mod writes the whole file size there; if vanilla writes something much smaller
        /// then the field is a prefetch buffer, and claiming a prefetch the .bnk does not carry is a
        /// reason for a segment to select and then play nothing.</summary>
        [Test]
        public void WhatMusicSourcesDeclareAgainstTheWemTheyName()
        {
            var wemSizeById = new Dictionary<uint, long>();
            foreach (var packName in new[] { "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                using var vanillaProvider = VanillaBankReader.CreateProvider(GameDirectory);
                foreach (var file in VanillaBankReader.OpenPack(vanillaProvider, packPath).GetAllFiles())
                {
                    if (file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase)
                        && uint.TryParse(Path.GetFileNameWithoutExtension(file.Key), out var sourceId))
                        wemSizeById[sourceId] = file.Value.DataSource.Size;
                }
            }

            Console.WriteLine($"{wemSizeById.Count} vanilla wems indexed");

            foreach (var bankName in new[] { "global_music__core.bnk", "campaign_music__core.bnk", "battle_music__core.bnk" })
            {
                var bytes = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                    .FirstOrDefault(bank => bank.Path.EndsWith(bankName, StringComparison.OrdinalIgnoreCase));
                if (bytes.Bytes == null)
                    continue;

                var sources = (BnkFile.CreateFromBytes(bytes.Bytes, bytes.Path, false).HircChunk?.HircItems ?? [])
                    .OfType<CAkMusicTrack_V136>()
                    .SelectMany(track => track.SourceList)
                    .Where(source => wemSizeById.ContainsKey(source.AkMediaInformation.SourceId))
                    .ToList();

                if (sources.Count == 0)
                    continue;

                var wholeFile = sources.Count(source => source.AkMediaInformation.InMemoryMediaSize == wemSizeById[source.AkMediaInformation.SourceId]);

                Console.WriteLine($"\n{bankName}: {sources.Count} source(s) whose wem was found");
                Console.WriteLine($"  declared == whole wem: {wholeFile}");
                Console.WriteLine($"  declared <  whole wem: {sources.Count - wholeFile}");

                foreach (var source in sources.Take(6))
                    Console.WriteLine($"    source {source.AkMediaInformation.SourceId}: " +
                        $"declared {source.AkMediaInformation.InMemoryMediaSize}, wem {wemSizeById[source.AkMediaInformation.SourceId]}");
            }

            Console.WriteLine($"\nthe mod declares 817726 for source 121350730, whose wem is 817726 bytes (the whole file)");

            // Sfx is the pattern the editor already ships and modders already hear, so whether
            // vanilla sounds declare the whole file decides if this is a music-only mistake or just
            // how the editor has always written the field.
            var soundBanks = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"));
            var checkedBanks = 0;

            foreach (var (sfxBankName, bankBytes) in soundBanks)
            {
                if (checkedBanks >= 3)
                    break;

                List<CAkSound_V136> sounds;
                try
                {
                    sounds = (BnkFile.CreateFromBytes(bankBytes, sfxBankName, false).HircChunk?.HircItems ?? [])
                        .OfType<CAkSound_V136>()
                        .Where(sound => wemSizeById.ContainsKey(sound.AkBankSourceData.AkMediaInformation.SourceId))
                        .ToList();
                }
                catch
                {
                    continue;
                }

                if (sounds.Count == 0)
                    continue;

                checkedBanks++;
                var wholeFile = sounds.Count(sound =>
                    sound.AkBankSourceData.AkMediaInformation.InMemoryMediaSize == wemSizeById[sound.AkBankSourceData.AkMediaInformation.SourceId]);

                Console.WriteLine($"\n{sfxBankName}: {sounds.Count} sound(s), declared == whole wem: {wholeFile}");
                foreach (var sound in sounds.Take(4))
                    Console.WriteLine($"    {sound.AkBankSourceData.StreamType} source {sound.AkBankSourceData.AkMediaInformation.SourceId}: " +
                        $"declared {sound.AkBankSourceData.AkMediaInformation.InMemoryMediaSize}, " +
                        $"wem {wemSizeById[sound.AkBankSourceData.AkMediaInformation.SourceId]}");
            }
        }

        /// <summary>The mod's chain against Cathay's, field by field. Cathay plays in game under the
        /// same conditions the mod is silent under, so it is the control: anything the mod sets
        /// differently below the decision tree is a candidate, and anything it sets the same is not.</summary>
        [Test]
        public void TheArabyChainFieldByFieldAgainstCathay()
        {
            var hircs = new Dictionary<uint, HircItem>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (File.Exists(packPath))
                    IndexBanks(hircs, VanillaBankReader.ReadMusicBanks(packPath));
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            IndexBanks(hircs, modPack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase)
                    && !file.Key.Contains("_for_merging", StringComparison.OrdinalIgnoreCase))
                .Select(file => (file.Key, file.Value.DataSource.ReadData()))
                .ToList());

            var container = (CAkMusicSwitchCntr_V136)hircs[698158058];

            foreach (var culture in new[] { "cathay", "araby" })
            {
                var key = WwiseHash.Compute(culture);
                var leaf = container.AkDecisionTree.DecisionTree.Nodes.FirstOrDefault(node => node.Key == key);
                Console.WriteLine($"\n################ {culture} ################");

                if (leaf == null)
                {
                    Console.WriteLine("  no branch");
                    continue;
                }

                DumpChainFields(hircs, leaf.AudioNodeId, "  ", 0);
            }
        }

        static void DumpChainFields(Dictionary<uint, HircItem> hircs, uint id, string indent, int depth)
        {
            if (depth > 4 || !hircs.TryGetValue(id, out var hirc))
            {
                Console.WriteLine($"{indent}!! {id} not found");
                return;
            }

            switch (hirc)
            {
                case CAkMusicRanSeqCntr_V136 ranSeq:
                    DumpNodeBaseParams($"{indent}RanSeq {id}", ranSeq.MusicTransNodeParams.MusicNodeParams);
                    foreach (var childId in ranSeq.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                        DumpChainFields(hircs, childId, indent + "  ", depth + 1);
                    break;

                case CAkMusicSegment_V136 segment:
                    DumpNodeBaseParams($"{indent}Segment {id}", segment.MusicNodeParams);
                    Console.WriteLine($"{indent}  duration {segment.Duration:F0}ms, markers " +
                        string.Join(" | ", segment.ArrayMarkersList.Select(marker => $"{marker.Id}@{marker.Position:F0}'{marker.MarkerName?.Replace("\0", "\\0")}'")));
                    foreach (var childId in segment.MusicNodeParams.Children.ChildIds)
                        DumpChainFields(hircs, childId, indent + "  ", depth + 1);
                    break;

                case CAkMusicTrack_V136 track:
                    var baseParams = track.NodeBaseParams;
                    Console.WriteLine($"{indent}Track {id}: bus {baseParams.OverrideBusId}, parent {baseParams.DirectParentId}, " +
                        $"bitVector {baseParams.BitVector}, overrideAttachment {baseParams.OverrideAttachmentParams}");
                    Console.WriteLine($"{indent}  trackType {track.TrackType}, lookAhead {track.LookAheadTime}, " +
                        $"numSubTrack {track.NumSubTrack}");
                    foreach (var source in track.SourceList)
                        Console.WriteLine($"{indent}  source {source.AkMediaInformation.SourceId}: {source.StreamType}, " +
                            $"plugin {source.PluginId}, inMemory {source.AkMediaInformation.InMemoryMediaSize}, " +
                            $"sourceBits {source.AkMediaInformation.SourceBits}");
                    foreach (var clip in track.PlaylistList)
                        Console.WriteLine($"{indent}  clip sub{clip.TrackId} src {clip.SourceId} eventId {clip.EventId} " +
                            $"playAt {clip.PlayAt:F0} begin {clip.BeginTrimOffset:F0} end {clip.EndTrimOffset:F0} dur {clip.SrcDuration:F0}");
                    break;
            }
        }

        static void DumpNodeBaseParams(string label, MusicNodeParams_V136 musicNodeParams)
        {
            var baseParams = musicNodeParams.NodeBaseParams;
            Console.WriteLine($"{label}: bus {baseParams.OverrideBusId}, parent {baseParams.DirectParentId}, " +
                $"bitVector {baseParams.BitVector}, overrideAttachment {baseParams.OverrideAttachmentParams}, " +
                $"flags {musicNodeParams.Flags}, children {musicNodeParams.Children.ChildIds.Count}, " +
                $"meter {musicNodeParams.MeterInfoFlag}, stingers {musicNodeParams.NumStingers}");
        }

        /// <summary>The chunks a vanilla music .bnk is made of, against the mod's. A source that
        /// declares a prefetch has to have somewhere to read it from, and DIDX/DATA is that
        /// somewhere - so whether vanilla music .bnks carry one decides what the small size means.</summary>
        [Test]
        public void WhichChunksVanillaMusicBanksCarry()
        {
            foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack")))
            {
                if (!path.EndsWith("global_music__core.bnk") && !path.EndsWith("campaign_music__core.bnk")
                    && !path.EndsWith("battle_music__core.bnk"))
                    continue;

                Console.WriteLine($"\n{path} ({bytes.Length} bytes): {string.Join(", ", ReadChunkTags(bytes))}");
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var file in VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false).GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk"))
                .OrderBy(file => file.Key))
            {
                var bytes = file.Value.DataSource.ReadData();
                Console.WriteLine($"\n{file.Key} ({bytes.Length} bytes): {string.Join(", ", ReadChunkTags(bytes))}");
            }
        }

        /// <summary>The .bnk chunk table, walked by the length each chunk declares.</summary>
        static List<string> ReadChunkTags(byte[] bytes)
        {
            var tags = new List<string>();
            var offset = 0;

            while (offset + 8 <= bytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
                var length = BitConverter.ToUInt32(bytes, offset + 4);
                tags.Add($"{tag}({length})");

                if (length == 0 || offset + 8 + length > (uint)bytes.Length)
                    break;

                offset += 8 + (int)length;
            }

            return tags;
        }

        /// <summary>What the declared size actually measures, tested against the RIFF layout of the
        /// wem it names. Vanilla music .bnks carry no DIDX/DATA, so the number cannot be media held
        /// in the .bnk - the hypothesis under test is that it is the header the game loads before it
        /// streams the rest, which would make it the offset at which the audio payload starts.</summary>
        [Test]
        public void WhatTheDeclaredSizeMeasuresInTheWem()
        {
            var wemById = new Dictionary<uint, byte[]>();
            var wanted = new HashSet<uint>();

            var bank = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(x => x.Path.EndsWith("global_music__core.bnk"));

            var sources = (BnkFile.CreateFromBytes(bank.Bytes, bank.Path, false).HircChunk?.HircItems ?? [])
                .OfType<CAkMusicTrack_V136>()
                .SelectMany(track => track.SourceList)
                .GroupBy(source => source.AkMediaInformation.SourceId)
                .Select(group => group.First())
                .Take(8)
                .ToList();

            foreach (var source in sources)
                wanted.Add(source.AkMediaInformation.SourceId);

            foreach (var packName in new[] { "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                using var vanillaProvider = VanillaBankReader.CreateProvider(GameDirectory);
                foreach (var file in VanillaBankReader.OpenPack(vanillaProvider, packPath).GetAllFiles())
                {
                    if (file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase)
                        && uint.TryParse(Path.GetFileNameWithoutExtension(file.Key), out var sourceId)
                        && wanted.Contains(sourceId))
                        wemById[sourceId] = file.Value.DataSource.ReadData();
                }
            }

            foreach (var source in sources)
            {
                var sourceId = source.AkMediaInformation.SourceId;
                if (!wemById.TryGetValue(sourceId, out var bytes))
                    continue;

                Console.WriteLine($"\nsource {sourceId}: declared {source.AkMediaInformation.InMemoryMediaSize}, wem {bytes.Length}");
                foreach (var (tag, start, length) in ReadRiffChunks(bytes))
                    Console.WriteLine($"    {tag} at {start}, {length} bytes, payload starts {start + 8}");

                ReportTheFormula(bytes, source.AkMediaInformation.InMemoryMediaSize);
            }

            // The mod's own wem, laid out the same way, so the value it should be declaring can be
            // read straight off it.
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modWem = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false).GetAllFiles()
                .First(file => file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase));

            var modBytes = modWem.Value.DataSource.ReadData();
            Console.WriteLine($"\n{modWem.Key}: declared 817726, wem {modBytes.Length}");
            foreach (var (tag, start, length) in ReadRiffChunks(modBytes))
                Console.WriteLine($"    {tag} at {start}, {length} bytes, payload starts {start + 8}");

            ReportTheFormula(modBytes, 817726);
        }

        /// <summary>The candidate rule - everything up to the first audio packet stays resident -
        /// checked against what the .bnk declares.</summary>
        static void ReportTheFormula(byte[] wemBytes, uint declared)
        {
            var dataChunk = ReadRiffChunks(wemBytes).FirstOrDefault(chunk => chunk.Tag == "data");
            if (dataChunk.Tag == null)
                return;

            Shared.GameFormats.Wwise.Wem.V132.WemFile wemFile;
            try
            {
                wemFile = Shared.GameFormats.Wwise.Wem.V132.WemFile.CreateFromWemBytes(wemBytes);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"    could not parse the wem: {exception.Message}");
                return;
            }

            var dataPayloadStart = (uint)dataChunk.Start + 8;
            var predicted = dataPayloadStart + wemFile.FmtChunk.FirstAudioPacketOffset;

            Console.WriteLine($"    dataPayloadStart {dataPayloadStart} + firstAudioPacketOffset {wemFile.FmtChunk.FirstAudioPacketOffset}" +
                $" = {predicted}   (declared {declared}) {(predicted == declared ? "MATCH" : "no")}");
        }

        /// <summary>The RIFF chunk table of a wem, as (tag, offset of the tag, declared length).</summary>
        static List<(string Tag, int Start, uint Length)> ReadRiffChunks(byte[] bytes)
        {
            var chunks = new List<(string, int, uint)>();
            var offset = 12;

            while (offset + 8 <= bytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
                var length = BitConverter.ToUInt32(bytes, offset + 4);
                chunks.Add((tag, offset, length));

                if (length == 0 || offset + 8 + (long)length > bytes.Length)
                    break;

                offset += 8 + (int)length;
            }

            return chunks;
        }

        /// <summary>The meter and transition rules every vanilla sibling of the mod's node declares.
        /// A music transition is scheduled against a musical grid, so a node that declares no meter
        /// where all of its siblings do is a node the container may never manage to start.</summary>
        [Test]
        public void WhatTheVanillaSiblingsDeclareForMeterAndRules()
        {
            var hircs = new Dictionary<uint, HircItem>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (File.Exists(packPath))
                    IndexBanks(hircs, VanillaBankReader.ReadMusicBanks(packPath));
            }

            foreach (var containerId in new uint[] { 698158058, 26264058 })
            {
                var container = (CAkMusicSwitchCntr_V136)hircs[containerId];
                Console.WriteLine($"\n################ children of {containerId} ################");

                foreach (var childId in container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                {
                    if (!hircs.TryGetValue(childId, out var child) || child is not CAkMusicRanSeqCntr_V136 ranSeq)
                        continue;

                    var musicNodeParams = ranSeq.MusicTransNodeParams.MusicNodeParams;
                    Console.WriteLine($"  RanSeq {childId}: meterFlag {musicNodeParams.MeterInfoFlag}, " +
                        $"rules {ranSeq.MusicTransNodeParams.NumRules}, " +
                        $"tempo {musicNodeParams.AkMeterInfo?.Tempo}, " +
                        $"beat {musicNodeParams.AkMeterInfo?.TimeSigBeatValue}/{musicNodeParams.AkMeterInfo?.TimeSigNumBeatsBar}, " +
                        $"gridPeriod {musicNodeParams.AkMeterInfo?.GridPeriod}, gridOffset {musicNodeParams.AkMeterInfo?.GridOffset}");
                }
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            var modHircs = new Dictionary<uint, HircItem>();
            IndexBanks(modHircs, modPack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase)
                    && !file.Key.Contains("_for_merging", StringComparison.OrdinalIgnoreCase))
                .Select(file => (file.Key, file.Value.DataSource.ReadData()))
                .ToList());

            Console.WriteLine("\n################ the mod's ################");
            foreach (var ranSeq in modHircs.Values.OfType<CAkMusicRanSeqCntr_V136>())
            {
                var musicNodeParams = ranSeq.MusicTransNodeParams.MusicNodeParams;
                Console.WriteLine($"  RanSeq {ranSeq.Id}: meterFlag {musicNodeParams.MeterInfoFlag}, " +
                    $"rules {ranSeq.MusicTransNodeParams.NumRules}, " +
                    $"tempo {musicNodeParams.AkMeterInfo?.Tempo}, " +
                    $"beat {musicNodeParams.AkMeterInfo?.TimeSigBeatValue}/{musicNodeParams.AkMeterInfo?.TimeSigNumBeatsBar}, " +
                    $"gridPeriod {musicNodeParams.AkMeterInfo?.GridPeriod}, gridOffset {musicNodeParams.AkMeterInfo?.GridOffset}");
            }

            Console.WriteLine("\n################ the mod's segments ################");
            foreach (var segment in modHircs.Values.OfType<CAkMusicSegment_V136>())
                Console.WriteLine($"  Segment {segment.Id}: meterFlag {segment.MusicNodeParams.MeterInfoFlag}, " +
                    $"tempo {segment.MusicNodeParams.AkMeterInfo?.Tempo}, gridPeriod {segment.MusicNodeParams.AkMeterInfo?.GridPeriod}");
        }

        /// <summary>
        /// A pack whose Araby branch points at Cathay's own vanilla node instead of the mod's.
        ///
        /// Three fixes aimed at fields of the generated hierarchy have each left the branch silent,
        /// so this stops guessing at fields and splits the problem in half instead. Cathay plays, so
        /// its node is known good; if Araby playing Cathay's music is audible then everything above
        /// the node - the .dat scripts, the State, the container, the decision tree, the .bnk load
        /// order - is proven, and the fault is entirely inside the hircs the editor generates. If it
        /// is still silent then the branch is never reached at all, and every field of the generated
        /// hierarchy is beside the point.
        ///
        /// Nothing of the mod's hierarchy is removed, only the leaf's target is repointed, so the
        /// two packs differ by exactly the thing under test.
        /// </summary>
        [Test]
        public void BuildAPackWhoseArabyBranchPlaysCathaysNode()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            var arabyKey = WwiseHash.Compute("araby");
            var cathayKey = WwiseHash.Compute("cathay");

            var testingBank = modPack.GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music_1_music_araby_for_testing.bnk", StringComparison.OrdinalIgnoreCase));

            var bnk = BnkFile.CreateFromBytes(testingBank.Value.DataSource.ReadData(), testingBank.Key, false);
            var hircItems = bnk.HircChunk.HircItems;

            foreach (var container in hircItems.OfType<CAkMusicSwitchCntr_V136>())
            {
                var arabyLeaves = new List<AkDecisionTree_V136.Node_V136>();
                var cathayTargets = new List<uint>();
                CollectLeavesFor(container.AkDecisionTree.DecisionTree, arabyKey, arabyLeaves);
                CollectTargetsFor(container.AkDecisionTree.DecisionTree, cathayKey, cathayTargets);

                var cathayTarget = cathayTargets.FirstOrDefault(target => target != 0);
                Console.WriteLine($"\ncontainer {container.Id}: {arabyLeaves.Count} araby leaf/leaves, cathay node {cathayTarget}");

                if (cathayTarget == 0)
                {
                    Console.WriteLine("  !! no cathay node to borrow, leaving this container alone");
                    continue;
                }

                foreach (var leaf in arabyLeaves)
                {
                    Console.WriteLine($"  repointing araby leaf from {leaf.AudioNodeId} to {cathayTarget}");
                    leaf.AudioNodeId = cathayTarget;
                }

                // A leaf only resolves to a node the container also claims, so the borrowed node has
                // to join the child list the same way the mod's own node did.
                var children = container.MusicTransNodeParams.MusicNodeParams.Children;
                var childIds = new SortedSet<uint>(children.ChildIds) { cathayTarget };
                children.ChildIds = [.. childIds];
                children.NumChilds = (uint)childIds.Count;

                container.AkDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(container.AkDecisionTree.DecisionTree);
                container.TreeDataSize = container.AkDecisionTree.GetSize();
                container.UpdateSectionSize();
            }

            var bkhdChunkBytes = Shared.GameFormats.Wwise.Bkhd.BkhdChunk.WriteData(bnk.BkhdChunk);
            var hircChunkBytes = HircChunk.WriteData(Editors.Audio.Shared.Wwise.Generators.Hirc.HircChunkGenerator.GenerateHircChunk(hircItems), bnk.BkhdChunk.AkBankHeader.BankGeneratorVersion);

            using var memStream = new MemoryStream();
            memStream.Write(bkhdChunkBytes);
            memStream.Write(hircChunkBytes);
            var rebuilt = memStream.ToArray();

            // Read it straight back, so a pack that cannot be parsed is caught here rather than by
            // being silent in game and looking like the very thing under test.
            var reparsed = BnkFile.CreateFromBytes(rebuilt, testingBank.Key, false);
            Assert.That(reparsed.HircChunk.HircItems, Has.Count.EqualTo(hircItems.Count));

            testingBank.Value.DataSource = new Shared.Core.PackFiles.Models.FileSources.MemorySource(rebuilt);

            modPack.IsReadOnly = false;
            var outputPath = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_music_cathay_node.pack");
            provider.GetRequiredService<IPackFileService>().SavePackContainer(modPack, outputPath, false, Shared.Core.Settings.GameInformationDatabase.GetGameById(Shared.Core.Settings.GameTypeEnum.Warhammer3));

            Console.WriteLine($"\nWrote {outputPath}");
        }

        /// <summary>
        /// A pack where Cathay's branch points at the mod's own node, as a control on the two things
        /// the mod depends on that nothing has ever confirmed: that the testing .bnk overrides the
        /// vanilla .bnk it is named after, and that the mod's node can produce sound at all.
        ///
        /// Repointing the new branch at a node known to play was still silent, so the branch is never
        /// reached and nothing inside the .bnk can be judged through it. Cathay is reached, so this
        /// drives the mod's node through Cathay's branch instead and leaves everything else alone.
        ///
        /// Playing as Cathay separates three cases by ear, which is the point of choosing the modder's
        /// own wav as the target rather than another culture's music:
        ///   the supplied wav  - the testing .bnk overrides vanilla and the mod's node plays, so only
        ///                       the selection of the new branch is broken;
        ///   silence           - the testing .bnk overrides vanilla but the mod's node is silent, so
        ///                       the fault is in the generated hierarchy after all;
        ///   Cathay's music    - the testing .bnk never overrides vanilla, the game has been reading
        ///                       vanilla's container throughout, and no change to the generated hircs
        ///                       could ever have been audible.
        /// </summary>
        [Test]
        public void BuildAPackWhereCathayPlaysTheModsNode()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            var testingBank = modPack.GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music_1_music_araby_for_testing.bnk", StringComparison.OrdinalIgnoreCase));

            var bnk = BnkFile.CreateFromBytes(testingBank.Value.DataSource.ReadData(), testingBank.Key, false);
            var hircItems = bnk.HircChunk.HircItems;

            foreach (var container in hircItems.OfType<CAkMusicSwitchCntr_V136>())
            {
                var cathayLeaves = new List<AkDecisionTree_V136.Node_V136>();
                var arabyTargets = new List<uint>();
                CollectLeavesFor(container.AkDecisionTree.DecisionTree, WwiseHash.Compute("cathay"), cathayLeaves);
                CollectTargetsFor(container.AkDecisionTree.DecisionTree, WwiseHash.Compute("araby"), arabyTargets);

                var arabyTarget = arabyTargets.FirstOrDefault(target => target != 0);
                Console.WriteLine($"\ncontainer {container.Id}: {cathayLeaves.Count} cathay leaf/leaves, mod node {arabyTarget}");

                if (arabyTarget == 0)
                {
                    Console.WriteLine("  !! no mod node to point at, leaving this container alone");
                    continue;
                }

                foreach (var leaf in cathayLeaves)
                {
                    Console.WriteLine($"  repointing cathay leaf from {leaf.AudioNodeId} to {arabyTarget}");
                    leaf.AudioNodeId = arabyTarget;
                }

                // The mod's node is already a declared child, since the araby branch names it, so
                // this only has to keep the list consistent rather than add anything.
                var children = container.MusicTransNodeParams.MusicNodeParams.Children;
                var childIds = new SortedSet<uint>(children.ChildIds) { arabyTarget };
                children.ChildIds = [.. childIds];
                children.NumChilds = (uint)childIds.Count;

                container.AkDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(container.AkDecisionTree.DecisionTree);
                container.TreeDataSize = container.AkDecisionTree.GetSize();
                container.UpdateSectionSize();
            }

            var bkhdChunkBytes = Shared.GameFormats.Wwise.Bkhd.BkhdChunk.WriteData(bnk.BkhdChunk);
            var hircChunkBytes = HircChunk.WriteData(
                Editors.Audio.Shared.Wwise.Generators.Hirc.HircChunkGenerator.GenerateHircChunk(hircItems),
                bnk.BkhdChunk.AkBankHeader.BankGeneratorVersion);

            using var memStream = new MemoryStream();
            memStream.Write(bkhdChunkBytes);
            memStream.Write(hircChunkBytes);
            var rebuilt = memStream.ToArray();

            var reparsed = BnkFile.CreateFromBytes(rebuilt, testingBank.Key, false);
            Assert.That(reparsed.HircChunk.HircItems, Has.Count.EqualTo(hircItems.Count));

            testingBank.Value.DataSource = new Shared.Core.PackFiles.Models.FileSources.MemorySource(rebuilt);

            modPack.IsReadOnly = false;
            var outputPath = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_music_cathay_plays_mod_node.pack");
            provider.GetRequiredService<IPackFileService>().SavePackContainer(modPack, outputPath, false,
                Shared.Core.Settings.GameInformationDatabase.GetGameById(Shared.Core.Settings.GameTypeEnum.Warhammer3));

            Console.WriteLine($"\nWrote {outputPath}");
        }

        /// <summary>
        /// A testing .bnk holding nothing but vanilla's two containers, with Cathay's branches taken
        /// out, to answer one question on its own: does a testing .bnk override the vanilla .bnk it
        /// is named after?
        ///
        /// The previous pack conflated two causes. Hearing Cathay's usual music through it could mean
        /// the override never happens, or it could mean the override happens and Wwise rejects the
        /// .bnk over something in the mod's own hircs and falls back to vanilla. Both look identical
        /// from the sofa, so this removes every generated hirc from the .bnk and edits nothing but
        /// vanilla's decision trees.
        ///
        /// Cathay going silent means a testing .bnk does override vanilla, and the fault is in what
        /// the mod puts inside one. Cathay still playing means the override itself never happens, and
        /// the whole testing .bnk scheme does not work for music.
        ///
        /// The campaign container's default branch goes too. Removing only Cathay's would drop it
        /// through to the default and play something, which is not distinguishable by ear from the
        /// override having failed.
        /// </summary>
        [Test]
        public void BuildAPackThatSilencesCathayWithVanillaContentOnly()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            var testingBank = modPack.GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music_1_music_araby_for_testing.bnk", StringComparison.OrdinalIgnoreCase));

            var bnk = BnkFile.CreateFromBytes(testingBank.Value.DataSource.ReadData(), testingBank.Key, false);

            // Vanilla's containers alone. Every generated hirc is dropped so that nothing the editor
            // produces can be blamed for the .bnk failing to load.
            var containers = bnk.HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().Cast<HircItem>().ToList();
            Console.WriteLine($"keeping {containers.Count} container(s), dropping {bnk.HircChunk.HircItems.Count - containers.Count} generated hirc(s)");

            foreach (var container in containers.Cast<CAkMusicSwitchCntr_V136>())
            {
                var removed = RemoveBranches(container.AkDecisionTree.DecisionTree,
                    [WwiseHash.Compute("cathay"), WwiseHash.Compute("araby"), DefaultKey]);

                // The child list is rebuilt from what the tree still names, so it stays consistent
                // with the trimmed tree rather than keeping ids nothing points at any more.
                var leaves = new List<uint>();
                CollectLeaves(container.AkDecisionTree.DecisionTree, leaves);
                var childIds = new SortedSet<uint>(leaves.Where(id => id != 0));

                container.MusicTransNodeParams.MusicNodeParams.Children = new Children_V136
                {
                    NumChilds = (uint)childIds.Count,
                    ChildIds = [.. childIds]
                };

                container.AkDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(container.AkDecisionTree.DecisionTree);
                container.TreeDataSize = container.AkDecisionTree.GetSize();
                container.UpdateSectionSize();

                Console.WriteLine($"  container {container.Id}: removed {removed} branch(es), " +
                    $"{container.AkDecisionTree.DecisionTree.Nodes.Count} top level keys left, {childIds.Count} children");
            }

            var bkhdChunkBytes = Shared.GameFormats.Wwise.Bkhd.BkhdChunk.WriteData(bnk.BkhdChunk);
            var hircChunkBytes = HircChunk.WriteData(
                Editors.Audio.Shared.Wwise.Generators.Hirc.HircChunkGenerator.GenerateHircChunk(containers),
                bnk.BkhdChunk.AkBankHeader.BankGeneratorVersion);

            using var memStream = new MemoryStream();
            memStream.Write(bkhdChunkBytes);
            memStream.Write(hircChunkBytes);
            var rebuilt = memStream.ToArray();

            var reparsed = BnkFile.CreateFromBytes(rebuilt, testingBank.Key, false);
            Assert.That(reparsed.HircChunk.HircItems, Has.Count.EqualTo(containers.Count));

            testingBank.Value.DataSource = new Shared.Core.PackFiles.Models.FileSources.MemorySource(rebuilt);

            modPack.IsReadOnly = false;
            var outputPath = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_music_cathay_silenced.pack");
            provider.GetRequiredService<IPackFileService>().SavePackContainer(modPack, outputPath, false,
                Shared.Core.Settings.GameInformationDatabase.GetGameById(Shared.Core.Settings.GameTypeEnum.Warhammer3));

            Console.WriteLine($"\nWrote {outputPath}");
        }

        /// <summary>
        /// Two packs holding the same .bnk under two names, to settle whether a .bnk overrides
        /// another by loading after it.
        ///
        /// Traced in the running game (bank_trace.py, October 2026): entering a campaign loads every
        /// .bnk whose name starts with "campaign", one folder at a time and in ascending name order
        /// within it, mod files merged in among vanilla's - so campaign_music_1_..._for_testing.bnk
        /// loads just before campaign_music__core.bnk, never after it. The testing .bnk naming
        /// assumes the opposite. The silenced Cathay pack above made the same bet for global_music
        /// and lost, so if the same order holds there, the later of two .bnks defining an id is
        /// the one Wwise keeps.
        ///
        /// The bytes are identical; only the name differs, and with it the side of
        /// global_music__core.bnk it sorts on ('1' sorts before '_', 'z' after). Each holds vanilla's
        /// two culture containers with Cathay's branches and the defaults taken out, and nothing
        /// generated, so a Cathay campaign map is silent if that .bnk wins and plays as usual if
        /// not. Load one pack at a time:
        ///
        ///   A silent, B silent - a .bnk overrides whichever side it sorts on
        ///   A plays,  B silent - the later .bnk wins; naming mod .bnks to sort after fixes this
        ///   A plays,  B plays  - a .bnk cannot override another by load order at all
        ///   A silent, B plays  - the earlier .bnk wins, which contradicts the result above
        ///
        /// Both played, and a trace from launch showed B loaded straight after global_music__core.bnk
        /// - so the name prefix loader does pick it up. C settles what that means: the same two
        /// containers spliced into vanilla's own global_music__core.bnk and shipped under its name,
        /// which is the file the game reads.
        ///
        ///   C silent - the edit works, so a second .bnk defining an id vanilla already loaded is
        ///              ignored, whichever side it loads on; only replacing vanilla's .bnk changes it
        ///   C plays  - Cathay's campaign map music does not come from these containers, and every
        ///              silencing test so far, August's included, was editing something unheard
        /// </summary>
        [Test]
        public void BuildTwoCathaySilencingPacksThatDifferOnlyInLoadOrder()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();

            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            foreach (var (packName, bankName) in new[]
            {
                ("music_order_test_a_before.pack", "global_music_1_cathay_silenced"),
                ("music_order_test_b_after.pack", "global_music_zz_cathay_silenced"),
                ("music_order_test_c_replaces.pack", "global_music__core"),
                ("music_order_test_d_before_with_parents.pack", "global_music_1_cathay_silenced_with_parents"),
            })
            {
                // Read afresh for each pack, since the branches are removed in place.
                var bnk = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false);
                var containers = bnk.HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>()
                    .Where(container => container.Id is 698158058 or 26264058)
                    .ToList();
                Assert.That(containers, Has.Count.EqualTo(2), "a culture container moved out of global_music__core.bnk");

                foreach (var container in containers)
                {
                    // Only at the culture level. 698158058 is two deep since the October 2026 update, and
                    // its second level is nothing but defaults - removing defaults at every depth empties
                    // every subculture's branch, not just Cathay's.
                    var cultureLevel = container.Arguments.FindIndex(argument =>
                        argument.GroupId == WwiseHash.Compute("WH3_Campaign_Subcultures") ||
                        argument.GroupId == WwiseHash.Compute("Battle_Music_WH3_Culture"));
                    Assert.That(cultureLevel, Is.Not.EqualTo(-1), $"{container.Id} no longer branches on culture");
                    var leavesBefore = new List<uint>();
                    CollectLeaves(container.AkDecisionTree.DecisionTree, leavesBefore);
                    var removed = RemoveBranchesAtDepth(container.AkDecisionTree.DecisionTree, cultureLevel,
                        [WwiseHash.Compute("cathay"), DefaultKey]);

                    // Vanilla's own child list less whatever only the removed branches pointed at, so
                    // nothing changes but Cathay - including children vanilla claims and no leaf names.
                    var leaves = new List<uint>();
                    CollectLeaves(container.AkDecisionTree.DecisionTree, leaves);
                    var vanillaChildIds = container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds;
                    var childIds = vanillaChildIds.Where(id => leaves.Contains(id) || !leavesBefore.Contains(id)).ToList();
                    container.MusicTransNodeParams.MusicNodeParams.Children = new Children_V136
                    {
                        NumChilds = (uint)childIds.Count,
                        ChildIds = childIds
                    };

                    container.AkDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(container.AkDecisionTree.DecisionTree);
                    container.TreeDataSize = container.AkDecisionTree.GetSize();
                    container.UpdateSectionSize();

                    Console.WriteLine($"{bankName}: container {container.Id}, removed {removed} branch(es), " +
                        $"{childIds.Count} of vanilla's {vanillaChildIds.Count} children kept");
                }

                byte[] rebuilt;
                if (bankName == "global_music__core")
                {
                    // Vanilla's bytes with only the two containers swapped, so nothing else in the
                    // file goes through a writer.
                    rebuilt = SpliceHircs(vanilla.Bytes, containers.ToDictionary(container => container.Id, container => (HircItem)container), []);
                    Assert.That(BnkFile.CreateFromBytes(rebuilt, bankName, false).HircChunk.HircItems,
                        Has.Count.EqualTo(bnk.HircChunk.HircItems.Count));
                }
                else
                {
                    // Wwise tracks loaded banks by this id, so it follows the name - two .bnks sharing
                    // one would have the second skipped as already loaded, whatever its name.
                    bnk.BkhdChunk.AkBankHeader.SoundBankId = WwiseHash.Compute(bankName);

                    var bkhdChunkBytes = Shared.GameFormats.Wwise.Bkhd.BkhdChunk.WriteData(bnk.BkhdChunk);
                    // D carries the containers' vanilla ancestors unchanged, after them the way vanilla
                    // orders a child ahead of its parent, so the edited containers do not load into a
                    // hierarchy whose parents are still missing.
                    var hircs = containers.Cast<HircItem>().ToList();
                    if (bankName.EndsWith("_with_parents", StringComparison.Ordinal))
                        hircs.AddRange(AncestorIds.Select(id => bnk.HircChunk.HircItems.Single(hirc => hirc.Id == id)));

                    var hircChunkBytes = HircChunk.WriteData(
                        Editors.Audio.Shared.Wwise.Generators.Hirc.HircChunkGenerator.GenerateHircChunk(hircs),
                        bnk.BkhdChunk.AkBankHeader.BankGeneratorVersion);

                    using var memStream = new MemoryStream();
                    memStream.Write(bkhdChunkBytes);
                    memStream.Write(hircChunkBytes);
                    rebuilt = memStream.ToArray();
                    Assert.That(BnkFile.CreateFromBytes(rebuilt, bankName, false).HircChunk.HircItems, Has.Count.EqualTo(hircs.Count));
                }

                var pack = packFileService.CreateNewPackFileContainer(
                    Path.GetFileNameWithoutExtension(packName), Shared.Core.Settings.PackFileVersion.PFH5, Shared.Core.PackFiles.Models.PackFileCAType.MOD, setEditablePack: true);
                packFileService.AddFilesToPack(pack,
                [
                    new NewPackFileEntry("audio\\wwise",
                        new Shared.Core.PackFiles.Models.PackFile($"{bankName}.bnk",
                            new Shared.Core.PackFiles.Models.FileSources.MemorySource(rebuilt)))
                ]);

                var outputPath = Path.Combine(Path.GetDirectoryName(ModPackPath)!, packName);
                packFileService.SavePackContainer(pack, outputPath, false,
                    Shared.Core.Settings.GameInformationDatabase.GetGameById(Shared.Core.Settings.GameTypeEnum.Warhammer3));
                Console.WriteLine($"Wrote {outputPath}");
            }
        }

        /// <summary>
        /// A pack that ships no .bnk of its own at all - every hirc the mod defines goes into the
        /// vanilla .bnk it belongs in, and those .bnks ship at vanilla's own paths. There is no second
        /// .bnk whose load order, override behaviour or duplicate ids could also be the reason for
        /// what is heard. Everything vanilla had is carried across untouched.
        ///
        /// Built in stages, because a pack carrying every change at once only says whether all of
        /// them together work. Each stage is the one before it plus one thing:
        ///
        ///   hierarchy - the mod's six music hircs appended to global_music__core.bnk. Known to leave
        ///               the game sounding completely normal, so it is the baseline the rest sit on.
        ///   containers - and the two Music Switch containers replaced with the merged ones.
        ///   events     - and the Events and Actions that set the State.
        ///   campaign   - and campaign_music__core.bnk replaced, which is the prebattle layer.
        ///   adaptive   - and a branch in 67383790, the six deep container the table leaves out.
        ///   eventbanks - the campaign stage, with every Event in the vanilla .bnk its vanilla
        ///                siblings live in rather than all six in global_music__core.bnk.
        ///
        /// The campaign stage plays the mod's audio in prebattle and nothing in battle, although
        /// 26264058 is merged, resolves araby, and reaches the wem. 67383790 is the only other
        /// container in the game that branches on Battle_Music_WH3_Culture, so the last stage points
        /// araby at a node 67383790 already owns. That needs no new hierarchy and cannot conflict over
        /// a parent, and it asks one question: is this the container battle actually reads. Music of
        /// any kind in battle means yes.
        /// </summary>
        [TestCase("hierarchy")]
        [TestCase("containers")]
        [TestCase("events")]
        [TestCase("campaign")]
        [TestCase("adaptive")]
        [TestCase("eventbanks")]
        public void BuildAPackThatReplacesTheVanillaMusicBankOutright(string stage)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            var packFileService = provider.GetRequiredService<IPackFileService>();
            modPack.IsReadOnly = false;

            var withContainers = stage is "containers" or "events" or "campaign" or "adaptive" or "eventbanks";
            var withEvents = stage is "events" or "campaign" or "adaptive" or "eventbanks";
            var withCampaign = stage is "campaign" or "adaptive" or "eventbanks";
            var withAdaptive = stage is "adaptive";
            var withRoutedEvents = stage is "eventbanks";

            var modBanks = modPack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(file => file.Key, file => file.Value);

            var vanillaBanks = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack")).ToList();

            HircItem[] HircsOf(string name) => [.. BnkFile
                .CreateFromBytes(modBanks.Single(bank => bank.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase)).Value.DataSource.ReadData(), name, false)
                .HircChunk?.HircItems ?? []];

            var globalHircs = HircsOf("global_music_1_music_araby_for_testing.bnk");
            var shippedHircs = HircsOf("global_music_music_araby.bnk");

            var destinations = new Dictionary<string, List<HircItem>>
            {
                ["global_music__core.bnk"] =
                [
                    .. globalHircs.Where(hirc => withContainers || hirc is not CAkMusicSwitchCntr_V136),

                    // The Events and the Actions they fire are the only things that set the State, and
                    // the .bnk the mod ships is the only place they live. Everything else in it is
                    // already accounted for by the testing .bnk.
                    .. withEvents && !withRoutedEvents
                        ? shippedHircs.Where(hirc => globalHircs.All(existing => existing.Id != hirc.Id))
                        : [],
                ]
            };

            if (withCampaign)
                destinations["campaign_music__core.bnk"] = [.. HircsOf("campaign_music_1_music_araby_for_testing.bnk")];

            if (withRoutedEvents)
                RouteEventsToTheirVanillaBanks(shippedHircs, globalHircs, destinations);

            if (withAdaptive)
                destinations["global_music__core.bnk"].Add(BranchArabyIntoTheAdaptiveContainer());

            var newFiles = new List<NewPackFileEntry>();

            foreach (var (vanillaName, modHircs) in destinations)
            {
                var vanilla = vanillaBanks.First(bank => bank.Path.EndsWith(vanillaName, StringComparison.OrdinalIgnoreCase));
                var vanillaSections = WalkHircSections(vanilla.Bytes).ToDictionary(section => section.Id, section => section.Section);

                // Whatever vanilla already defines is a replacement; the rest is new and gets appended.
                // Splitting on the id rather than on the hirc type keeps this honest about what the
                // mod actually re-emits - the campaign .bnk re-emits plain Switch containers and
                // Music Tracks too, not only Music Switch containers.
                var replacements = modHircs.Where(hirc => vanillaSections.ContainsKey(hirc.Id)).ToDictionary(hirc => hirc.Id);
                var additions = modHircs.Where(hirc => !vanillaSections.ContainsKey(hirc.Id)).ToList();

                var rebuilt = SpliceHircs(vanilla.Bytes, replacements, additions);
                var reparsed = BnkFile.CreateFromBytes(rebuilt, vanilla.Path, false);

                Console.WriteLine($"\n{vanillaName}: {replacements.Count} replaced, {additions.Count} appended, " +
                    $"{rebuilt.Length} bytes (vanilla was {vanilla.Bytes.Length}), reparsed {reparsed.HircChunk.HircItems.Count} hircs");

                // A replacement whose bytes match vanilla's changes nothing, so the ones that differ
                // are the whole of what this pack does to a .bnk the game was happy with.
                foreach (var (id, replacement) in replacements)
                {
                    replacement.UpdateSectionSize();
                    var written = replacement.WriteData();
                    var original = vanillaSections[id];
                    var identical = written.Length == original.Length && written.AsSpan().SequenceEqual(original);

                    // The first difference is always the size field, so the body is compared from
                    // past the type, size and id - that offset is where the content actually diverges.
                    const int bodyStart = 9;

                    Console.WriteLine(identical
                        ? $"    {replacement.HircType,-24} {id}  unchanged"
                        : $"    {replacement.HircType,-24} {id}  +{written.Length - original.Length} bytes, " +
                          $"body diverges at {FirstDifference(written[bodyStart..], original[bodyStart..])}");
                }

                foreach (var addition in additions)
                    Console.WriteLine($"    {addition.HircType,-24} {addition.Id}  appended");

                foreach (var hirc in modHircs)
                    Assert.That(reparsed.HircChunk.HircItems.Any(item => item.Id == hirc.Id), Is.True,
                        $"{hirc.HircType} {hirc.Id} did not survive the splice into {vanillaName}");

                // Vanilla holds to this for every one of its own relations, so a rebuilt .bnk that
                // breaks it is not a .bnk the game has ever been asked to load.
                Assert.That(ChildrenAfterTheirParents(reparsed.HircChunk.HircItems), Is.Empty,
                    $"{vanillaName} puts a hirc after something that claims it as a child");

                newFiles.Add(new NewPackFileEntry("audio\\wwise",
                    new Shared.Core.PackFiles.Models.PackFile(vanillaName,
                        new Shared.Core.PackFiles.Models.FileSources.MemorySource(rebuilt))));
            }

            foreach (var (path, file) in modBanks)
            {
                packFileService.DeleteFile(modPack, file);
                Console.WriteLine($"dropped {path}");
            }

            packFileService.AddFilesToPack(modPack, newFiles);

            var outputPath = Path.Combine(Path.GetDirectoryName(ModPackPath)!, $"araby_stage_{stage}.pack");
            packFileService.SavePackContainer(modPack, outputPath, false,
                Shared.Core.Settings.GameInformationDatabase.GetGameById(Shared.Core.Settings.GameTypeEnum.Warhammer3));

            Console.WriteLine($"\nWrote {outputPath}");
        }

        /// <summary>
        /// Puts each Event, and the Actions it fires, in the vanilla .bnk that holds the vanilla
        /// Events of the same family.
        ///
        /// Vanilla splits them by the context that plays them: music_b_faction_* live in
        /// battle_music__core.bnk, music_c_* in campaign_music__core.bnk. Putting all six in
        /// global_music__core.bnk gets the campaign ones played, because that .bnk is loaded on the
        /// campaign layer, and leaves the battle one somewhere battle never reads - so the script
        /// posts music_b_faction_araby and nothing answers.
        ///
        /// An Event and its Actions have to travel together: the Event claims them as children, and a
        /// child has to be in the same .bnk as the thing that claims it.
        /// </summary>
        static void RouteEventsToTheirVanillaBanks(HircItem[] shippedHircs, HircItem[] globalHircs, Dictionary<string, List<HircItem>> destinations)
        {
            var nameByEventId = new[]
            {
                "music_b_faction_araby",
                "music_c_subculture_araby",
                "music_c_ams_araby",
                "music_c_ams_pulse_perc_araby",
                "music_c_ams_pulse_orch_araby",
                "music_c_ams_pulse_ethnic_araby"
            }.ToDictionary(WwiseHash.Compute, name => name);

            var byId = shippedHircs.ToDictionary(hirc => hirc.Id);
            var routed = new HashSet<uint>();

            foreach (var akEvent in shippedHircs.OfType<CAkEvent_V136>())
            {
                if (!nameByEventId.TryGetValue(akEvent.Id, out var eventName))
                    throw new InvalidDataException($"Event {akEvent.Id} is not one of the names this knows how to place");

                var bank = eventName.StartsWith("music_b_", StringComparison.Ordinal)
                    ? "battle_music__core.bnk"
                    : "campaign_music__core.bnk";

                if (!destinations.TryGetValue(bank, out var hircs))
                    destinations[bank] = hircs = [];

                // Actions first, so nothing claims a hirc that comes after it.
                foreach (var actionId in akEvent.GetActionIds())
                {
                    hircs.Add(byId[actionId]);
                    routed.Add(actionId);
                }

                hircs.Add(akEvent);
                routed.Add(akEvent.Id);

                Console.WriteLine($"{eventName} ({akEvent.Id}) -> {bank}");
            }

            // Whatever is left in the mod's .bnk and not already in the testing .bnk keeps its old
            // home, since nothing here has learned anything about where it belongs.
            destinations["global_music__core.bnk"].AddRange(shippedHircs
                .Where(hirc => !routed.Contains(hirc.Id) && globalHircs.All(existing => existing.Id != hirc.Id)));
        }

        /// <summary>
        /// 67383790 with an araby branch pointing at a node it already owns.
        ///
        /// Its leaves are stem sets for the adaptive battle music rather than whole pieces, so a
        /// branch here cannot name the mod's own Random Sequence without a hierarchy shaped like one
        /// of those sets. Borrowing one of its existing children sidesteps that entirely - no new
        /// hircs, no second container claiming a node another already parents - and still answers
        /// whether this is the container battle reads.
        /// </summary>
        static CAkMusicSwitchCntr_V136 BranchArabyIntoTheAdaptiveContainer()
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var container = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().Single(hirc => hirc.Id == 67383790);

            var cathayKey = WwiseHash.Compute("cathay");
            var borrowed = PathsOf(container)
                .Where(path => path.Key.Split(" / ").ElementAtOrDefault(2) == cathayKey.ToString())
                .Select(path => path.Value)
                .First(audioNodeId => audioNodeId != 0);

            Console.WriteLine($"67383790: araby will point at {borrowed}, a node cathay already reaches");

            var merged = new Editors.Audio.Shared.Wwise.Generators.MusicSwitchContainerMergeService()
                .MergeBranches(container, [new Editors.Audio.Shared.Wwise.Generators.MusicBranch("Battle_Music_WH3_Culture", "araby", borrowed)]);

            var before = PathsOf(container);
            var after = PathsOf(merged);
            Console.WriteLine($"67383790: {before.Count} paths -> {after.Count}");

            // 884 paths is a lot of room for a merge to quietly lose one, and losing one here means
            // some vanilla culture goes silent in battle rather than the new one.
            Assert.That(before.Where(path => !after.TryGetValue(path.Key, out var now) || now != path.Value), Is.Empty,
                "67383790 lost or changed a vanilla path");

            return merged;
        }

        /// <summary>The .bnk without the named hircs, spliced out at the byte level the same way
        /// they are spliced in.</summary>
        static byte[] RemoveHircs(byte[] bankBytes, HashSet<uint> ids, out List<uint> removed)
        {
            removed = [];
            var keptBytes = new List<byte[]>();

            foreach (var (_, id, section) in WalkHircSections(bankBytes))
            {
                if (ids.Contains(id))
                    removed.Add(id);
                else
                    keptBytes.Add(section);
            }

            if (removed.Count == 0)
                return bankBytes;

            var hircChunkStart = 0;
            while (System.Text.Encoding.ASCII.GetString(bankBytes, hircChunkStart, 4) != "HIRC")
                hircChunkStart += 8 + (int)BitConverter.ToUInt32(bankBytes, hircChunkStart + 4);

            var hircChunkSize = BitConverter.ToUInt32(bankBytes, hircChunkStart + 4);
            var body = keptBytes.SelectMany(section => section).ToArray();

            using var output = new MemoryStream();
            output.Write(bankBytes, 0, hircChunkStart);
            output.Write(System.Text.Encoding.ASCII.GetBytes("HIRC"));
            output.Write(BitConverter.GetBytes((uint)(body.Length + 4)));
            output.Write(BitConverter.GetBytes((uint)keptBytes.Count));
            output.Write(body);

            var afterHirc = hircChunkStart + 8 + (int)hircChunkSize;
            if (afterHirc < bankBytes.Length)
                output.Write(bankBytes, afterHirc, bankBytes.Length - afterHirc);

            return output.ToArray();
        }

        /// <summary>See <see cref="Editors.Audio.Shared.Wwise.Generators.VanillaSoundBankSplicer"/>, which
        /// is what the compiler ships with.</summary>
        static byte[] SpliceHircs(byte[] vanillaBytes, Dictionary<uint, HircItem> replacements, List<HircItem> additions) =>
            Editors.Audio.Shared.Wwise.Generators.VanillaSoundBankSplicer.Splice(vanillaBytes, replacements, additions);

        /// <summary>
        /// The parents of the two culture containers up to the root, all in global_music__core.bnk:
        /// 698158058 under 1042256876, 26264058 under 67383790, both of those under 148962132.
        /// </summary>
        static readonly uint[] AncestorIds = [1042256876, 67383790, 148962132];

        /// <summary>Drops every branch keyed on one of the given keys at one depth of the tree, 0 being
        /// the root's children.</summary>
        static int RemoveBranchesAtDepth(AkDecisionTree_V136.Node_V136 node, int depth, uint[] keys)
        {
            if (depth == 0)
                return node.Nodes.RemoveAll(child => keys.Contains(child.Key));

            return node.Nodes.Sum(child => RemoveBranchesAtDepth(child, depth - 1, keys));
        }

        /// <summary>Drops every branch keyed on one of the given keys, at whatever depth it sits.</summary>
        static int RemoveBranches(AkDecisionTree_V136.Node_V136 node, uint[] keys)
        {
            var removed = node.Nodes.RemoveAll(child => keys.Contains(child.Key));
            foreach (var child in node.Nodes)
                removed += RemoveBranches(child, keys);

            return removed;
        }

        const uint DefaultKey = 0;

        static void CollectLeavesFor(AkDecisionTree_V136.Node_V136 node, uint key, List<AkDecisionTree_V136.Node_V136> found, bool matched = false)
        {
            foreach (var child in node.Nodes)
            {
                var childMatched = matched || child.Key == key;

                if (child.Nodes.Count == 0)
                {
                    if (childMatched)
                        found.Add(child);
                }
                else
                    CollectLeavesFor(child, key, found, childMatched);
            }
        }

        static void CollectTargetsFor(AkDecisionTree_V136.Node_V136 node, uint key, List<uint> found, bool matched = false)
        {
            foreach (var child in node.Nodes)
            {
                var childMatched = matched || child.Key == key;

                if (child.Nodes.Count == 0)
                {
                    if (childMatched)
                        found.Add(child.AudioNodeId);
                }
                else
                    CollectTargetsFor(child, key, found, childMatched);
            }
        }

        /// <summary>
        /// Where a State is declared in vanilla, and whether the mod declares its own the same way.
        ///
        /// Pointing the new branch at a node known to play left it silent, so the branch is never
        /// reached and the State is never becoming current. A State Group's members are declared
        /// outside the decision tree - in the init bank's STMG tables and as State hircs - and the
        /// editor writes neither, so a State the game has never been told about is the candidate.
        /// </summary>
        [Test]
        public void WhereAStateIsDeclaredInVanilla()
        {
            uint[] cultureStates =
            [
                WwiseHash.Compute("Cathay"), WwiseHash.Compute("Empire"), WwiseHash.Compute("Araby"),
            ];

            uint[] stateGroups =
            [
                WwiseHash.Compute("Battle_Music_WH3_Culture"), WwiseHash.Compute("WH3_Campaign_Subcultures"),
            ];

            Console.WriteLine("states:  " + string.Join(", ", new[] { "Cathay", "Empire", "Araby" }
                .Select(name => $"{name}={WwiseHash.Compute(name)}")));
            Console.WriteLine("groups:  " + string.Join(", ", new[] { "Battle_Music_WH3_Culture", "WH3_Campaign_Subcultures" }
                .Select(name => $"{name}={WwiseHash.Compute(name)}")));

            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    var chunkTags = ReadChunkTags(bytes);
                    var hasStmg = chunkTags.Any(tag => tag.StartsWith("STMG"));

                    List<HircItem> hircItems;
                    try
                    {
                        hircItems = BnkFile.CreateFromBytes(bytes, path, false).HircChunk?.HircItems ?? [];
                    }
                    catch
                    {
                        hircItems = [];
                    }

                    var stateHircs = hircItems.Where(hirc => hirc.HircType == Shared.GameFormats.Wwise.Enums.AkBkHircType.State).ToList();
                    var namedStates = stateHircs.Where(hirc => cultureStates.Contains(hirc.Id)).Select(hirc => hirc.Id).ToList();

                    if (!hasStmg && stateHircs.Count == 0)
                        continue;

                    Console.WriteLine($"\n{packName}  {path}");
                    Console.WriteLine($"  chunks: {string.Join(", ", chunkTags)}");
                    Console.WriteLine($"  State hircs: {stateHircs.Count}" +
                        (namedStates.Count == 0 ? "" : $"   including culture states {string.Join(", ", namedStates)}"));

                    if (hasStmg)
                        ReportStmgGroups(bytes, stateGroups, cultureStates);
                }
            }

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            Console.WriteLine("\n################ the mod ################");

            foreach (var file in modPack.GetAllFiles().Where(file => file.Key.EndsWith(".bnk")).OrderBy(file => file.Key))
            {
                var bytes = file.Value.DataSource.ReadData();
                var hircItems = BnkFile.CreateFromBytes(bytes, file.Key, false).HircChunk?.HircItems ?? [];
                Console.WriteLine($"  {file.Key}: chunks {string.Join(", ", ReadChunkTags(bytes))}, " +
                    $"State hircs {hircItems.Count(hirc => hirc.HircType == Shared.GameFormats.Wwise.Enums.AkBkHircType.State)}");
            }
        }

        /// <summary>
        /// The State Group table inside an STMG chunk, walked far enough to list the groups and the
        /// states each one declares. Read by hand because nothing in the codebase parses STMG - which
        /// is the point of the check.
        /// </summary>
        static void ReportStmgGroups(byte[] bytes, uint[] wantedGroups, uint[] wantedStates)
        {
            var offset = 0;
            var stmgStart = -1;
            uint stmgLength = 0;

            while (offset + 8 <= bytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
                var length = BitConverter.ToUInt32(bytes, offset + 4);

                if (tag == "STMG")
                {
                    stmgStart = offset + 8;
                    stmgLength = length;
                    break;
                }

                if (length == 0 || offset + 8 + length > (uint)bytes.Length)
                    break;

                offset += 8 + (int)length;
            }

            if (stmgStart == -1)
                return;

            // Every id the chunk contains, so a group or state can be looked for without having to
            // model the whole layout correctly.
            var idsInStmg = new HashSet<uint>();
            for (var index = stmgStart; index + 4 <= stmgStart + (int)stmgLength; index += 4)
                idsInStmg.Add(BitConverter.ToUInt32(bytes, index));

            Console.WriteLine($"  STMG is {stmgLength} bytes");
            foreach (var group in wantedGroups)
                Console.WriteLine($"    state group {group}: {(idsInStmg.Contains(group) ? "PRESENT" : "absent")}");
            foreach (var state in wantedStates)
                Console.WriteLine($"    state {state}: {(idsInStmg.Contains(state) ? "PRESENT" : "absent")}");
        }

        /// <summary>
        /// What the mod's event data .dat says about the State Groups, next to what vanilla's own
        /// event data .dats say. If vanilla enumerates the members of a music State Group anywhere,
        /// that is a list the game reads and a new State has to join.
        /// </summary>
        [Test]
        public void WhatTheEventDataDatSaysAboutStates()
        {
            string[] musicStateGroups =
            [
                "Battle_Music_WH3_Culture", "WH3_Campaign_Subcultures",
                "WH3_Campaign_Music_AMS_Fragments_Faction",
            ];

            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            var modDat = modPack.GetAllFiles()
                .Single(file => file.Key.EndsWith("event_data__music_araby.dat", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"################ {modDat.Key} ################");
            ReportDat(Shared.GameFormats.Dat.DatFileParser.Parse(modDat.Value, false));

            // Every vanilla event data .dat, so the group memberships the game ships with can be
            // read rather than assumed from the editor's comments.
            foreach (var packName in new[] { "audio_base.pack", "audio_base_bnk.pack" })
            {
                var packPath = Path.Combine(GameDirectory, "data", packName);
                if (!File.Exists(packPath))
                    continue;

                using var vanillaProvider = VanillaBankReader.CreateProvider(GameDirectory);
                var pack = VanillaBankReader.OpenPack(vanillaProvider, packPath);

                foreach (var file in pack.GetAllFiles()
                    .Where(file => file.Key.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
                        && file.Key.Contains("event_data", StringComparison.OrdinalIgnoreCase)))
                {
                    Shared.GameFormats.Dat.SoundDatFile parsed;
                    try
                    {
                        parsed = Shared.GameFormats.Dat.DatFileParser.Parse(file.Value, false);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"\n{packName} {file.Key}: could not parse - {exception.Message}");
                        continue;
                    }

                    var relevant = parsed.StateGroupsWithStates0.Concat(parsed.StateGroupsWithStates1)
                        .Where(group => musicStateGroups.Contains(group.StateGroup, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (relevant.Count == 0)
                        continue;

                    Console.WriteLine($"\n################ {packName} {file.Key} ################");
                    foreach (var group in relevant)
                        Console.WriteLine($"  {group.StateGroup}: {group.States.Count} states -> {string.Join(", ", group.States)}");
                }
            }
        }

        static void ReportDat(Shared.GameFormats.Dat.SoundDatFile dat)
        {
            Console.WriteLine($"  EventWithStateGroup: {dat.EventWithStateGroup.Count}");
            foreach (var item in dat.EventWithStateGroup)
                Console.WriteLine($"    {item.Event} = {item.Value}");

            Console.WriteLine($"  StateGroupsWithStates0: {dat.StateGroupsWithStates0.Count}");
            foreach (var item in dat.StateGroupsWithStates0)
                Console.WriteLine($"    {item.StateGroup}: [{string.Join(", ", item.States)}]");

            Console.WriteLine($"  StateGroupsWithStates1: {dat.StateGroupsWithStates1.Count}");
            foreach (var item in dat.StateGroupsWithStates1)
                Console.WriteLine($"    {item.StateGroup}: [{string.Join(", ", item.States)}]");

            Console.WriteLine($"  DialogueEventsWithStateGroups: {dat.DialogueEventsWithStateGroups.Count}");
            Console.WriteLine($"  SettingValues: {dat.SettingValues.Count}");
        }

        /// <summary>What the patched scripts actually say, next to what vanilla says, so the match
        /// key the game compares against is read rather than assumed.</summary>
        [Test]
        public void WhatTheScriptsMatchOn()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var container = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);
            var files = container.GetAllFiles();

            foreach (var name in new[] { "battle_music.dat", "campaign_music.dat" })
            {
                var packFile = files.Single(file => file.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase)).Value;
                Console.WriteLine($"\n================ {name} ================");

                var file = MusicDatParser.Parse(packFile);
                foreach (var slot in MusicDatCultureWiring.FindSlots(file))
                {
                    Console.WriteLine($"\n-- slot: {slot.Title}");
                    foreach (var chain in slot.Chains)
                    {
                        var cases = chain.Cases
                            .Select(c => $"{c.MatchKey} => {c.SoundEvent ?? $"<culture:{c.MusicalCulture}>"}")
                            .ToList();
                        Console.WriteLine("   " + string.Join("\n   ", cases));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Whether this codebase can write a vanilla hirc back as the bytes it read.
        ///
        /// Replacing a vanilla .bnk killed every piece of music in the game, main menu included, so
        /// the file is being loaded and Wwise is rejecting it. Only two things in it were not
        /// vanilla's own bytes: the two re-written switch containers and the appended hircs. This
        /// reads each of vanilla's hircs, writes it straight back with nothing changed, and compares.
        /// Anything that does not come back identical is a writer that cannot be trusted to emit a
        /// hirc the game will accept - including the ones the generator writes from scratch.
        /// </summary>
        [TestCase("global_music__core.bnk")]
        [TestCase("campaign_music__core.bnk")]
        public void WhetherVanillaHircsSurviveBeingWrittenBack(string BankUnderTest)
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith(BankUnderTest, StringComparison.OrdinalIgnoreCase));

            var results = new Dictionary<string, (int Total, int Mismatched, string FirstExample)>();

            foreach (var (index, id, section) in WalkHircSections(vanilla.Bytes))
            {
                HircItem hirc;
                try
                {
                    // 2147483784 is the bank generator version WH3 actually writes; 136 is only
                    // wwiser's nickname for it and the factory does not answer to it.
                    hirc = HircItem.ReadData(vanilla.Path, new Shared.ByteParsing.ByteChunk(section), 2147483784, 0, false, index);
                }
                catch (Exception exception)
                {
                    Record(results, "<unreadable>", false, $"{id}: {exception.Message}");
                    continue;
                }

                byte[] written;
                try
                {
                    written = hirc.WriteData();
                }
                catch (Exception exception)
                {
                    Record(results, hirc.GetType().Name, false, $"{id}: write threw {exception.GetType().Name} {exception.Message}");
                    continue;
                }

                var identical = written.Length == section.Length && written.AsSpan().SequenceEqual(section);
                Record(results, hirc.GetType().Name, identical,
                    identical ? null : $"{id}: wrote {written.Length} bytes, read {section.Length}, first differs at {FirstDifference(written, section)}");
            }

            Console.WriteLine($"{vanilla.Path}\n");
            Console.WriteLine($"{"type",-40} {"total",8} {"bad",8}   example");

            foreach (var (type, result) in results.OrderByDescending(entry => entry.Value.Mismatched).ThenBy(entry => entry.Key))
                Console.WriteLine($"{type,-40} {result.Total,8} {result.Mismatched,8}   {result.FirstExample}");

            var switchContainers = results.TryGetValue(nameof(CAkMusicSwitchCntr_V136), out var containerResult) ? containerResult : default;
            Console.WriteLine($"\nswitch containers: {switchContainers.Mismatched} of {switchContainers.Total} do not round trip");
        }

        /// <summary>
        /// Whether the splice itself is sound, checked without the game.
        ///
        /// Vanilla's own hircs write back byte for byte, so nothing carried across can be the reason
        /// the replacement .bnk killed every piece of music. That leaves the file structure the splice
        /// builds around them. A splice that changes nothing must come back byte identical to vanilla;
        /// if it does not, the chunk table or the tail is wrong, and everything after HIRC - the
        /// bank's other chunks included - is being handed to Wwise misaligned.
        /// </summary>
        [Test]
        public void WhetherASpliceThatChangesNothingComesBackIdentical()
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var passthrough = SpliceHircs(vanilla.Bytes, [], []);

            Console.WriteLine($"vanilla {vanilla.Bytes.Length} bytes, passthrough {passthrough.Length} bytes");
            Console.WriteLine($"chunks vanilla:     {string.Join(", ", WalkChunks(vanilla.Bytes))}");
            Console.WriteLine($"chunks passthrough: {string.Join(", ", WalkChunks(passthrough))}");

            if (passthrough.Length == vanilla.Bytes.Length && !passthrough.AsSpan().SequenceEqual(vanilla.Bytes))
                Console.WriteLine($"same length but differs at {FirstDifference(passthrough, vanilla.Bytes)}");

            Assert.That(passthrough, Is.EqualTo(vanilla.Bytes), "a splice that changes nothing did not reproduce vanilla");
        }

        /// <summary>Every top level chunk as tag and declared length, plus whether the walk lands
        /// exactly on the end of the file. A walk that overruns or stops short means some chunk's
        /// declared length disagrees with what is actually there.</summary>
        static IEnumerable<string> WalkChunks(byte[] bankBytes)
        {
            var offset = 0;

            while (offset + 8 <= bankBytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bankBytes, offset, 4);
                var length = BitConverter.ToUInt32(bankBytes, offset + 4);
                yield return $"{tag}:{length}";
                offset += 8 + (int)length;
            }

            yield return offset == bankBytes.Length ? "(ends exactly)" : $"(ends at {offset} of {bankBytes.Length})";
        }

        /// <summary>
        /// The decision tree resolved the way Wwise resolves it - by index walking the flat node
        /// array in the bytes that shipped - rather than by walking the nested tree.
        ///
        /// Every test on the merge asserts against the nested tree, but the nested tree is the input
        /// to flattening, not the output. A branch can be perfectly placed there and still be
        /// unreachable on disk if the flattening writes the wrong child offsets, and nothing written
        /// so far would notice. This walks the array by ChildrenIdx exactly as the game does, for the
        /// mod's State and for a vanilla one side by side.
        /// </summary>
        [Test]
        public void WhetherTheArabyBranchResolvesTheWayWwiseWalksIt()
        {
            var replacementPack = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_music_replacing_vanilla_bank.pack");
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var pack = VanillaBankReader.OpenPack(provider, replacementPack, markAsCa: false);

            var bankFile = pack.GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var bnk = BnkFile.CreateFromBytes(bankFile.Value.DataSource.ReadData(), bankFile.Key, false);
            var hircIds = bnk.HircChunk.HircItems.Select(hirc => hirc.Id).ToHashSet();

            foreach (var container in bnk.HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>()
                .Where(container => container.Id is 26264058 or 698158058))
            {
                Console.WriteLine($"\n================ container {container.Id} ================");
                Console.WriteLine($"tree depth {container.TreeDepth}, declared tree data size {container.TreeDataSize}, " +
                    $"written {container.AkDecisionTree.GetSize()}, {container.AkDecisionTree.Nodes.Count} nodes");
                Console.WriteLine($"arguments: {string.Join(", ", container.Arguments.Select(argument => argument.GroupId))}");

                var childIds = container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds;
                var dangling = childIds.Where(childId => !hircIds.Contains(childId)).ToList();
                Console.WriteLine($"children: {childIds.Count}, naming {dangling.Count} hircs this bank does not hold" +
                    (dangling.Count == 0 ? "" : $": {string.Join(", ", dangling)}"));

                var treeBytes = container.AkDecisionTree.WriteData();

                foreach (var stateName in new[] { "araby", "cathay" })
                    ResolveLikeWwise(treeBytes, container.TreeDepth, WwiseHash.Compute(stateName), stateName, childIds, hircIds);
            }
        }

        /// <summary>
        /// Walks the flat node array from the root, taking the wanted key where a level offers it and
        /// the default otherwise, exactly as the runtime does. Everything is read straight out of the
        /// twelve byte records; nothing goes through the nested model.
        /// </summary>
        static void ResolveLikeWwise(byte[] treeBytes, uint treeDepth, uint wantedKey, string stateName, List<uint> childIds, HashSet<uint> hircIds)
        {
            const int recordSize = 12;
            var recordCount = treeBytes.Length / recordSize;

            Console.WriteLine($"\n  resolving '{stateName}' (key {wantedKey}) over {recordCount} records:");

            var index = 0;

            for (var depth = 0; depth < treeDepth; depth++)
            {
                var childrenIdx = BitConverter.ToUInt16(treeBytes, index * recordSize + 4);
                var childrenCount = BitConverter.ToUInt16(treeBytes, index * recordSize + 6);

                if (childrenCount == 0 || childrenIdx + childrenCount > recordCount)
                {
                    Console.WriteLine($"    depth {depth}: node {index} claims {childrenCount} children at {childrenIdx} - out of range, walk stops");
                    return;
                }

                var chosen = -1;
                var fallback = -1;

                for (var offset = 0; offset < childrenCount; offset++)
                {
                    var candidate = childrenIdx + offset;
                    var key = BitConverter.ToUInt32(treeBytes, candidate * recordSize);

                    if (key == wantedKey)
                        chosen = candidate;
                    else if (key == 0)
                        fallback = candidate;
                }

                var taken = chosen != -1 ? chosen : fallback;
                var how = chosen != -1 ? "exact" : fallback != -1 ? "default" : "nothing";

                if (taken == -1)
                {
                    Console.WriteLine($"    depth {depth}: node {index} has {childrenCount} children at {childrenIdx}, none match and no default - walk stops");
                    return;
                }

                Console.WriteLine($"    depth {depth}: node {index} -> child {taken} ({how} match) of {childrenCount} at {childrenIdx}");
                index = taken;
            }

            var audioNodeId = BitConverter.ToUInt32(treeBytes, index * recordSize + 4);
            Console.WriteLine($"    leaf: node {index} names audio node {audioNodeId} " +
                $"[{(hircIds.Contains(audioNodeId) ? "in bank" : "NOT IN BANK")}, " +
                $"{(childIds.Contains(audioNodeId) ? "claimed as child" : "NOT CLAIMED AS CHILD")}]");
        }

        /// <summary>
        /// Everything the mod's own .bnks hold, per .bnk.
        ///
        /// The replacement pack drops all of them to stop Wwise being handed two definitions of the
        /// same ids, on the assumption the replaced .bnk carries everything that matters. That is only
        /// true of the music hierarchy and the containers. Anything else in them - an Event, an Action
        /// that sets the State - would have gone with them, and nothing sets a State that no Action
        /// sets.
        /// </summary>
        [Test]
        public void WhatTheModsOwnBanksHold()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var pack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            foreach (var (path, file) in pack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file.Key))
            {
                var bnk = BnkFile.CreateFromBytes(file.DataSource.ReadData(), path, false);
                var hircs = bnk.HircChunk?.HircItems ?? [];

                Console.WriteLine($"\n{path}  ({hircs.Count} hircs)");
                foreach (var hirc in hircs)
                    Console.WriteLine($"    {hirc.HircType,-28} {hirc.Id}");
            }
        }

        /// <summary>
        /// Every root to leaf path in the merged containers against vanilla's, read out of the flat
        /// node array in the bytes that ship rather than out of the nested model.
        ///
        /// Replacing the two containers is what makes Wwise throw global_music__core.bnk away - the
        /// stage that only appends the mod's hierarchy leaves the game sounding normal, the stage that
        /// also swaps the containers silences it. Their bytes differ from vanilla's only by the one
        /// child the merge adds, so the damage is not in the size of anything. This enumerates what
        /// the tree actually resolves to, path by path, and says which of vanilla's own paths the
        /// merge changed or lost.
        /// </summary>
        [Test]
        public void WhetherVanillaPathsSurviveTheMergeInTheWrittenBytes()
        {
            var stagePack = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_stage_containers.pack");
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);

            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var merged = VanillaBankReader.OpenPack(provider, stagePack, markAsCa: false).GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var vanillaContainers = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().ToDictionary(container => container.Id);

            var mergedContainers = BnkFile.CreateFromBytes(merged.Value.DataSource.ReadData(), merged.Key, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().ToDictionary(container => container.Id);

            foreach (var containerId in new uint[] { 26264058, 698158058 })
            {
                var before = PathsOf(vanillaContainers[containerId]);
                var after = PathsOf(mergedContainers[containerId]);

                Console.WriteLine($"\n================ container {containerId} ================");
                Console.WriteLine($"vanilla {before.Count} paths, merged {after.Count} paths");

                var lost = before.Where(path => !after.ContainsKey(path.Key)).ToList();
                var changed = before.Where(path => after.TryGetValue(path.Key, out var now) && now != path.Value).ToList();
                var added = after.Where(path => !before.ContainsKey(path.Key)).ToList();

                foreach (var path in lost)
                    Console.WriteLine($"    LOST    [{path.Key}] -> {path.Value}");

                foreach (var path in changed)
                    Console.WriteLine($"    CHANGED [{path.Key}] {path.Value} -> {after[path.Key]}");

                foreach (var path in added)
                    Console.WriteLine($"    added   [{path.Key}] -> {path.Value}");

                Assert.Multiple(() =>
                {
                    Assert.That(lost, Is.Empty, $"container {containerId} lost vanilla paths");
                    Assert.That(changed, Is.Empty, $"container {containerId} changed where vanilla paths lead");
                });
            }
        }

        /// <summary>
        /// How many child blocks sit at each level, and how many of them offer key 0. A State with no
        /// branch survives a level only if the block it lands in has a default, so anything short of
        /// full coverage is a level where some paths dead end.
        /// </summary>
        static Dictionary<int, (int Blocks, int WithDefault)> DefaultCoveragePerLevel(CAkMusicSwitchCntr_V136 container)
        {
            const int recordSize = 12;
            var treeBytes = container.AkDecisionTree.WriteData();
            var coverage = new Dictionary<int, (int Blocks, int WithDefault)>();

            void Walk(int index, int depth)
            {
                if (depth == container.TreeDepth)
                    return;

                var childrenIdx = BitConverter.ToUInt16(treeBytes, index * recordSize + 4);
                var childrenCount = BitConverter.ToUInt16(treeBytes, index * recordSize + 6);

                var hasDefault = Enumerable.Range(0, childrenCount)
                    .Any(offset => BitConverter.ToUInt32(treeBytes, (childrenIdx + offset) * recordSize) == 0);

                coverage.TryGetValue(depth, out var current);
                coverage[depth] = (current.Blocks + 1, current.WithDefault + (hasDefault ? 1 : 0));

                for (var offset = 0; offset < childrenCount; offset++)
                    Walk(childrenIdx + offset, depth + 1);
            }

            Walk(0, 0);
            return coverage;
        }

        /// <summary>Every root to leaf path as the key sequence that reaches it and the audio node it
        /// names, walked over the twelve byte records the container writes.</summary>
        static Dictionary<string, uint> PathsOf(CAkMusicSwitchCntr_V136 container)
        {
            const int recordSize = 12;
            var treeBytes = container.AkDecisionTree.WriteData();
            var paths = new Dictionary<string, uint>();

            void Walk(int index, int depth, List<uint> keys)
            {
                if (depth == container.TreeDepth)
                {
                    paths[string.Join(" / ", keys)] = BitConverter.ToUInt32(treeBytes, index * recordSize + 4);
                    return;
                }

                var childrenIdx = BitConverter.ToUInt16(treeBytes, index * recordSize + 4);
                var childrenCount = BitConverter.ToUInt16(treeBytes, index * recordSize + 6);

                for (var offset = 0; offset < childrenCount; offset++)
                {
                    var child = childrenIdx + offset;
                    Walk(child, depth + 1, [.. keys, BitConverter.ToUInt32(treeBytes, child * recordSize)]);
                }
            }

            Walk(0, 0, []);
            return paths;
        }

        /// <summary>
        /// Two things the stage that only appends the mod's hierarchy never put to the test.
        ///
        /// First, the copy that rebuilds a container. Merging a container with itself adds nothing, so
        /// it has to come back as the bytes it went in as; anything the copy drops or reorders shows
        /// up here with no branch to confuse it.
        ///
        /// Second, the parent links. Appending a node nobody claims is free - Wwise never tries to
        /// attach it. The moment a container lists it as a child, the node's own parent has to point
        /// back at that container, and the two containers here are different containers.
        /// </summary>
        [Test]
        public void WhetherTheContainerCopyAndTheParentLinksHold()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);

            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var vanillaSections = WalkHircSections(vanilla.Bytes).ToDictionary(section => section.Id, section => section.Section);
            var vanillaHircs = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false).HircChunk.HircItems;
            var vanillaContainers = vanillaHircs.OfType<CAkMusicSwitchCntr_V136>().ToDictionary(container => container.Id);

            var mergeService = new Editors.Audio.Shared.Wwise.Generators.MusicSwitchContainerMergeService();

            Console.WriteLine("merging a container with itself and comparing to vanilla's bytes:\n");

            foreach (var containerId in new uint[] { 26264058, 698158058 })
            {
                var container = vanillaContainers[containerId];
                var copied = mergeService.MergeContainers(container, container);
                copied.UpdateSectionSize();

                var written = copied.WriteData();
                var original = vanillaSections[containerId];
                var identical = written.Length == original.Length && written.AsSpan().SequenceEqual(original);

                Console.WriteLine(identical
                    ? $"    {containerId}  identical"
                    : $"    {containerId}  DIFFERS: {written.Length} bytes against vanilla's {original.Length}, " +
                      $"body diverges at {FirstDifference(written[9..], original[9..])}");
            }

            Console.WriteLine("\nparent links of what each container claims as a child:\n");

            var stagePack = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_stage_containers.pack");
            var stageBank = VanillaBankReader.OpenPack(provider, stagePack, markAsCa: false).GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var stageHircs = BnkFile.CreateFromBytes(stageBank.Value.DataSource.ReadData(), stageBank.Key, false).HircChunk.HircItems;
            var byId = stageHircs.ToDictionary(hirc => hirc.Id);

            foreach (var container in stageHircs.OfType<CAkMusicSwitchCntr_V136>().Where(container => container.Id is 26264058 or 698158058))
            {
                Console.WriteLine($"  container {container.Id}");

                foreach (var childId in container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                {
                    if (!byId.TryGetValue(childId, out var child))
                    {
                        Console.WriteLine($"    {childId}  NOT IN BANK");
                        continue;
                    }

                    var parentId = ParentOf(child);
                    var isNew = !vanillaSections.ContainsKey(childId);
                    var note = parentId == container.Id ? "ok" : $"POINTS AT {parentId}";
                    Console.WriteLine($"    {child.HircType,-24} {childId,-12} parent {parentId,-12} {note}{(isNew ? "   <- added by the mod" : "")}");
                }
            }
        }

        /// <summary>
        /// Whether vanilla ever puts a hirc after something that claims it as a child.
        ///
        /// The splice appends the mod's hircs at the end of the HIRC list, which puts them after the
        /// containers that name them. That costs nothing while nobody claims them - the stage that only
        /// appends leaves the game normal - and it is exactly what changes when the containers start
        /// claiming them, which is the stage that goes silent. If vanilla never does this, the order
        /// is the difference.
        /// </summary>
        [Test]
        public void WhetherVanillaEverPutsAChildAfterItsParent()
        {
            foreach (var bankName in new[] { "global_music__core.bnk", "campaign_music__core.bnk" })
            {
                var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                    .First(bank => bank.Path.EndsWith(bankName, StringComparison.OrdinalIgnoreCase));

                var hircs = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false).HircChunk.HircItems;
                var positionOf = hircs.Select((hirc, position) => (hirc.Id, position))
                    .GroupBy(entry => entry.Id)
                    .ToDictionary(group => group.Key, group => group.First().position);

                var relations = hircs.Sum(hirc => ChildrenOf(hirc).Count(childId => positionOf.ContainsKey(childId)));
                var childAfterParent = ChildrenAfterTheirParents(hircs);

                Console.WriteLine($"{bankName}: {hircs.Count} hircs, {relations} parent-child relations inside the .bnk, " +
                    $"{childAfterParent.Count} where the child comes after the parent");

                foreach (var offender in childAfterParent.Take(10))
                    Console.WriteLine($"    {offender}");
            }
        }

        /// <summary>Every relation where a hirc sits later in the .bnk than something claiming it as
        /// a child.</summary>
        static List<string> ChildrenAfterTheirParents(List<HircItem> hircs)
        {
            var positionOf = hircs.Select((hirc, position) => (hirc.Id, position))
                .GroupBy(entry => entry.Id)
                .ToDictionary(group => group.Key, group => group.First().position);

            return
            [
                .. from entry in hircs.Select((hirc, position) => (hirc, position))
                   from childId in ChildrenOf(entry.hirc)
                   where positionOf.TryGetValue(childId, out var childPosition) && childPosition > entry.position
                   select $"{entry.hirc.HircType} {entry.hirc.Id} at {entry.position} claims {childId} at {positionOf[childId]}"
            ];
        }

        static IEnumerable<uint> ChildrenOf(HircItem hirc) => hirc switch
        {
            CAkMusicSwitchCntr_V136 switchCntr => switchCntr.MusicTransNodeParams.MusicNodeParams.Children.ChildIds,
            CAkMusicRanSeqCntr_V136 ranSeq => ranSeq.MusicTransNodeParams.MusicNodeParams.Children.ChildIds,
            CAkMusicSegment_V136 segment => segment.MusicNodeParams.Children.ChildIds,
            _ => []
        };

        static uint ParentOf(HircItem hirc) => hirc switch
        {
            CAkMusicRanSeqCntr_V136 ranSeq => ranSeq.MusicTransNodeParams.MusicNodeParams.NodeBaseParams.DirectParentId,
            CAkMusicSwitchCntr_V136 switchCntr => switchCntr.MusicTransNodeParams.MusicNodeParams.NodeBaseParams.DirectParentId,
            CAkMusicSegment_V136 segment => segment.MusicNodeParams.NodeBaseParams.DirectParentId,
            _ => uint.MaxValue
        };

        /// <summary>
        /// Every Music Switch container in every vanilla .bnk, by the State Group it branches on and
        /// whether a vanilla culture reaches a leaf through it.
        ///
        /// Prebattle plays the mod's audio and battle does not, so something the battle path needs is
        /// not being touched. The mod only ever merges the containers its testing .bnks are named
        /// after; if the container that answers to Battle_Music_WH3_Culture lives in a .bnk outside
        /// that set, no branch was ever added to it and battle has nothing to select.
        /// </summary>
        [Test]
        public void WhichBankHoldsTheContainerBattleBranchesOn()
        {
            var groupNames = new[]
            {
                "Battle_Music_WH3_Culture",
                "WH3_Campaign_Subcultures",
                "WH3_Campaign_Music_AMS_Fragments_Faction",
                "WH3_AMS_Pulse_Percussion_Options",
                "WH3_AMS_Pulse_Pitched_Ethnic_Options",
                "WH3_AMS_Pulse_Pitched_Orchestral_Options"
            };

            var nameByGroupId = groupNames.ToDictionary(WwiseHash.Compute, name => name);
            var cathayKey = WwiseHash.Compute("cathay");
            var arabyKey = WwiseHash.Compute("araby");

            foreach (var name in groupNames)
                Console.WriteLine($"{WwiseHash.Compute(name),-12} {name}");

            // Every audio pack, not just the two the earlier probes happened to name - a container
            // battle branches on is only out of scope if it is genuinely not there.
            foreach (var packPath in Directory.GetFiles(Path.Combine(GameDirectory, "data"), "audio*.pack"))
            {
                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    List<CAkMusicSwitchCntr_V136> containers;
                    try
                    {
                        containers = [.. (BnkFile.CreateFromBytes(bytes, path, false).HircChunk?.HircItems ?? [])
                            .OfType<CAkMusicSwitchCntr_V136>()];
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var container in containers)
                    {
                        var level = container.Arguments.FindIndex(argument => nameByGroupId.ContainsKey(argument.GroupId));
                        if (level == -1)
                            continue;

                        var groups = container.Arguments
                            .Select(argument => nameByGroupId.TryGetValue(argument.GroupId, out var name) ? name : argument.GroupId.ToString())
                            .ToList();

                        var keysAtGroupLevel = PathsOf(container).Keys
                            .Select(key => key.Split(" / "))
                            .Where(keys => keys.Length > level)
                            .Select(keys => uint.Parse(keys[level]))
                            .Distinct()
                            .ToList();

                        Console.WriteLine($"\n{Path.GetFileName(packPath)}  {Path.GetFileName(path),-32} container {container.Id}");
                        Console.WriteLine($"    depth {container.TreeDepth}: {string.Join(" / ", groups)}");
                        Console.WriteLine($"    at the culture level: {keysAtGroupLevel.Count} keys, " +
                            $"default {(keysAtGroupLevel.Contains(0) ? "yes" : "NO")}, " +
                            $"cathay {(keysAtGroupLevel.Contains(cathayKey) ? "yes" : "no")}, " +
                            $"araby {(keysAtGroupLevel.Contains(arabyKey) ? "yes" : "no")}");
                    }
                }
            }
        }

        /// <summary>
        /// What key each level of a container's tree actually offers.
        ///
        /// Wh3MusicHierarchyInformation leaves 67383790 out of the table on the grounds that it names
        /// the culture as the default - 'the culture level of that tree is a single key 0 node'. If
        /// that were so, a culture with no branch would take the default and play whatever vanilla
        /// plays there. Battle is silent for the new culture and not for a vanilla one, which is what
        /// a level offering named keys and no default does, so the claim is worth measuring.
        /// </summary>
        [Test]
        public void WhatKeysEachLevelOfTheBattleContainersOffer()
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var containers = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().ToDictionary(container => container.Id);

            var known = new[] { "Battle_Music_WH3_Culture", "WH3_Campaign_Subcultures" }.ToDictionary(WwiseHash.Compute, name => name);
            var cathayKey = WwiseHash.Compute("cathay");
            var arabyKey = WwiseHash.Compute("araby");

            foreach (var containerId in new uint[] { 26264058, 67383790, 145953291, 698158058 })
            {
                var container = containers[containerId];
                Console.WriteLine($"\n================ container {containerId} (depth {container.TreeDepth}) ================");

                var keysByLevel = PathsOf(container).Keys
                    .Select(path => path.Split(" / "))
                    .SelectMany(keys => keys.Select((key, level) => (level, key: uint.Parse(key))))
                    .GroupBy(entry => entry.level)
                    .ToDictionary(group => group.Key, group => group.Select(entry => entry.key).Distinct().ToList());

                // A level having a default somewhere is not the same as every block at that level
                // having one. A State falls through only if the particular block it lands in offers
                // key 0, so what matters is the coverage, not the presence.
                var coverage = DefaultCoveragePerLevel(container);

                for (var level = 0; level < container.TreeDepth; level++)
                {
                    var groupId = container.Arguments[level].GroupId;
                    var groupName = known.TryGetValue(groupId, out var name) ? name : groupId.ToString();
                    var keys = keysByLevel.TryGetValue(level, out var found) ? found : [];
                    var (blocks, withDefault) = coverage[level];

                    var notes = new List<string>();
                    if (keys.Contains(cathayKey)) notes.Add("names cathay");
                    if (keys.Contains(arabyKey)) notes.Add("names araby");

                    Console.WriteLine($"    level {level}  {groupName,-28} {keys.Count,4} keys, " +
                        $"{withDefault}/{blocks} blocks offer a default" +
                        (notes.Count == 0 ? "" : $"   {string.Join(", ", notes)}"));
                }
            }
        }

        /// <summary>
        /// Every culture-level block of the adaptive battle container, with the keys above it named.
        ///
        /// Araby's battle music is silent rather than falling back to another culture's, with the
        /// battle Event in battle_music__core.bnk and 26264058 merged. A State that falls through to a
        /// block without a default selects nothing at all, and 3 of this container's 45 culture
        /// blocks have no default - so this asks which ones, and whether they are where battle goes.
        /// Keys are named by hashing every string the battle script holds.
        /// </summary>
        [Test]
        public void WhereTheAdaptiveBattleContainerHasNoDefault()
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));
            var container = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().Single(hirc => hirc.Id == 67383790);

            var script = MusicDatParser.Parse(File.ReadAllBytes(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "..", "Data", "Music", "battle_music.dat")));
            var names = new Dictionary<uint, string> { [0] = "<default>" };
            foreach (var text in script.StringConstants.Concat(script.StringVariables.Select(variable => variable.InitialValue))
                         .Concat(script.StringVariables.Select(variable => variable.Name))
                         .Concat(["Battle_Music_WH3_Culture", "araby", "cathay"]))
                names.TryAdd(WwiseHash.Compute(text), text);

            string Name(uint key) => names.TryGetValue(key, out var name) ? name : key.ToString();

            var cultureLevel = container.Arguments.FindIndex(argument => argument.GroupId == WwiseHash.Compute("Battle_Music_WH3_Culture"));
            Console.WriteLine("levels: " + string.Join(" / ", container.Arguments.Select(argument => Name(argument.GroupId))));

            void Walk(AkDecisionTree_V136.Node_V136 node, int depth, List<string> above)
            {
                if (depth == cultureLevel)
                {
                    var keys = node.Nodes.Select(child => child.Key).ToList();
                    Console.WriteLine($"  {string.Join(" / ", above),-50} default: {(keys.Contains(0) ? "yes" : "NO "),-3}  " +
                        $"araby: {(keys.Contains(WwiseHash.Compute("araby")) ? "yes" : "no")}  {keys.Count} cultures: " +
                        string.Join(", ", keys.Where(key => key != 0).Select(Name)));
                    return;
                }

                foreach (var child in node.Nodes)
                    Walk(child, depth + 1, [.. above, Name(child.Key)]);
            }

            Walk(container.AkDecisionTree.DecisionTree, 0, []);
        }

        /// <summary>
        /// The three containers Araby has a branch in, out of the pack that ships: their transition
        /// rules and meter, and one leaf each for Araby and a vanilla culture walked down to the
        /// segments' markers.
        ///
        /// Araby's battle music stays silent with a branch in every culture block of 67383790, while
        /// the same hierarchy shape plays in prebattle through 698158058. A branch can be found and
        /// still never start - a transition timed against a cue or grid the target lacks never
        /// happens - so this sets what the battle container asks of its targets beside what the
        /// working one does.
        /// </summary>
        [Test]
        public void WhatTheBattleContainerAsksOfWhatItSwitchesTo()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            // The end to end run's own output. The playable copy RPFM re-saved with the culture tables
            // holds the same .bnks, but this loader reads no files out of it.
            var modPack = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false);

            var hircs = new Dictionary<uint, HircItem>();
            foreach (var (path, file) in modPack.GetAllFiles().Where(file => file.Key.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase)))
            {
                var items = BnkFile.CreateFromBytes(file.DataSource.ReadData(), path, false).HircChunk.HircItems;
                Console.WriteLine($"{path}: {file.DataSource.Size} bytes, {items.Count} hircs, " +
                    $"{items.OfType<CAkMusicSwitchCntr_V136>().Count()} music switches, {items.Count(item => item.Id == 67383790)} with id 67383790 " +
                    $"({string.Join(", ", items.Where(item => item.Id == 67383790).Select(item => item.GetType().Name))})");
                foreach (var hirc in items)
                    hircs.TryAdd(hirc.Id, hirc);
            }
            Assert.That(hircs, Does.ContainKey(67383790u), "the shipped pack has no global_music__core.bnk");

            var araby = WwiseHash.Compute("araby");
            var cathay = WwiseHash.Compute("cathay");

            static string Rules(MusicTransNodeParams_V136 parameters) => string.Join("\n", parameters.PlayList.Select(rule =>
                $"      src [{string.Join(",", rule.SrcIdList)}] -> dst [{string.Join(",", rule.DstIdList)}]: " +
                $"sync {rule.AkMusicTransSrcRule.SyncType}, src cue filter {rule.AkMusicTransSrcRule.CueFilterHash}, " +
                $"post-exit {rule.AkMusicTransSrcRule.PlayPostExit}, fade out {rule.AkMusicTransSrcRule.TransitionTime}ms | " +
                $"entry {rule.AkMusicTransDstRule.EntryType}, dst cue filter {rule.AkMusicTransDstRule.CueFilterHash}, " +
                $"match src cue {rule.AkMusicTransDstRule.DestMatchSourceCueName}, jump {rule.AkMusicTransDstRule.JumpToId}/{rule.AkMusicTransDstRule.JumpToType}, " +
                $"pre-entry {rule.AkMusicTransDstRule.PlayPreEntry}, fade in {rule.AkMusicTransDstRule.TransitionTime}ms" +
                (rule.AkMusicTransitionObject == null ? "" : $", transition segment {rule.AkMusicTransitionObject.SegmentId}")));

            static string Meter(MusicNodeParams_V136 parameters) =>
                $"flags {parameters.Flags}, meter flag {parameters.MeterInfoFlag}, grid {parameters.AkMeterInfo.GridPeriod}/{parameters.AkMeterInfo.GridOffset}, " +
                $"tempo {parameters.AkMeterInfo.Tempo}, {parameters.AkMeterInfo.TimeSigNumBeatsBar}/{parameters.AkMeterInfo.TimeSigBeatValue}, " +
                $"parent {parameters.NodeBaseParams.DirectParentId}, bus {parameters.NodeBaseParams.OverrideBusId}";

            void Describe(uint id, string indent, int depth)
            {
                if (!hircs.TryGetValue(id, out var hirc))
                {
                    Console.WriteLine($"{indent}{id}: NOT IN ANY SHIPPED .BNK");
                    return;
                }

                switch (hirc)
                {
                    case CAkMusicRanSeqCntr_V136 sequence:
                        Console.WriteLine($"{indent}RanSeq {id}: {Meter(sequence.MusicTransNodeParams.MusicNodeParams)}, " +
                            $"{sequence.PlayList.Count} playlist items, {sequence.MusicTransNodeParams.PlayList.Count} rules");
                        Console.WriteLine(Rules(sequence.MusicTransNodeParams));
                        if (depth < 3)
                            foreach (var childId in sequence.MusicTransNodeParams.MusicNodeParams.Children.ChildIds.Take(2))
                                Describe(childId, indent + "  ", depth + 1);
                        break;
                    case CAkMusicSegment_V136 segment:
                        Console.WriteLine($"{indent}Segment {id}: {Meter(segment.MusicNodeParams)}, duration {segment.Duration:0}ms, markers " +
                            string.Join(", ", segment.ArrayMarkersList.Select(marker => $"{marker.Id}@{marker.Position:0}{(string.IsNullOrEmpty(marker.MarkerName) ? "" : $"'{marker.MarkerName.TrimEnd('\0')}'")}")));
                        break;
                    case CAkMusicSwitchCntr_V136 inner:
                        Console.WriteLine($"{indent}Switch {id}: {Meter(inner.MusicTransNodeParams.MusicNodeParams)}, depth {inner.TreeDepth}");
                        break;
                    default:
                        Console.WriteLine($"{indent}{hirc.HircType} {id}");
                        break;
                }
            }

            static uint? LeafUnder(AkDecisionTree_V136.Node_V136 node, int depth, int cultureLevel, uint culture, bool matched)
            {
                foreach (var child in node.Nodes)
                {
                    var nowMatched = matched || (depth == cultureLevel && child.Key == culture);
                    if (depth == cultureLevel && child.Key != culture)
                        continue;
                    if (child.Nodes.Count == 0)
                        return nowMatched && child.AudioNodeId != 0 ? child.AudioNodeId : null;
                    var found = LeafUnder(child, depth + 1, cultureLevel, culture, nowMatched);
                    if (found != null)
                        return found;
                }
                return null;
            }

            foreach (var containerId in new uint[] { 698158058, 26264058, 67383790 })
            {
                var container = (CAkMusicSwitchCntr_V136)hircs[containerId];
                Console.WriteLine($"\n======== container {containerId}: mode {container.Mode}, continue playback {container.IsContinuePlayback}, " +
                    Meter(container.MusicTransNodeParams.MusicNodeParams));
                Console.WriteLine("    rules:");
                Console.WriteLine(Rules(container.MusicTransNodeParams));

                var cultureLevel = container.Arguments.FindIndex(argument =>
                    argument.GroupId == WwiseHash.Compute("Battle_Music_WH3_Culture") || argument.GroupId == WwiseHash.Compute("WH3_Campaign_Subcultures"));

                foreach (var (name, key) in new[] { ("cathay", cathay), ("araby", araby) })
                {
                    var leaf = LeafUnder(container.AkDecisionTree.DecisionTree, 0, cultureLevel, key, false);
                    Console.WriteLine($"  {name} leaf:");
                    if (leaf != null)
                        Describe(leaf.Value, "    ", 0);
                    else
                        Console.WriteLine("    none");
                }
            }
        }

        /// <summary>
        /// Which cue markers the segments under each culture container carry, and where.
        ///
        /// The battle script steers itself on markers: Wwise_cues routes update_dynamicstart to the
        /// code that ends a transition and update_dynamicend to the code that allows the next one. A
        /// generated segment without them leaves the script stuck in "transition is in progress",
        /// which is what the in-battle trace showed. Positions are given relative to the entry and
        /// exit cues and as a share of the segment, so a rule for a segment of any length can be read
        /// off them rather than copied from one example.
        /// </summary>
        [Test]
        public void WhatMarkersTheCultureContainersSegmentsCarry()
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));
            var hircs = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false).HircChunk.HircItems
                .GroupBy(hirc => hirc.Id).ToDictionary(group => group.Key, group => group.First());

            IEnumerable<CAkMusicSegment_V136> SegmentsUnder(uint id, HashSet<uint> seen)
            {
                if (!seen.Add(id) || !hircs.TryGetValue(id, out var hirc))
                    yield break;

                IEnumerable<uint> children = hirc switch
                {
                    CAkMusicSegment_V136 segment => [],
                    CAkMusicRanSeqCntr_V136 sequence => sequence.MusicTransNodeParams.MusicNodeParams.Children.ChildIds,
                    CAkMusicSwitchCntr_V136 inner => inner.MusicTransNodeParams.MusicNodeParams.Children.ChildIds,
                    _ => []
                };

                if (hirc is CAkMusicSegment_V136 found)
                    yield return found;

                foreach (var child in children)
                    foreach (var segment in SegmentsUnder(child, seen))
                        yield return segment;
            }

            foreach (var containerId in new uint[] { 698158058, 26264058, 67383790 })
            {
                var container = (CAkMusicSwitchCntr_V136)hircs[containerId];
                var seen = new HashSet<uint> { containerId };
                var segments = container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds
                    .SelectMany(child => SegmentsUnder(child, seen))
                    .ToList();

                Console.WriteLine($"\n======== container {containerId}: {segments.Count} segments");

                foreach (var signature in segments
                    .GroupBy(segment => string.Join(", ", segment.ArrayMarkersList.Select(marker =>
                        string.IsNullOrEmpty(marker.MarkerName) ? marker.Id.ToString() : $"{marker.Id}'{marker.MarkerName.TrimEnd('\0')}'")))
                    .OrderByDescending(group => group.Count()))
                    Console.WriteLine($"  {signature.Count(),4} x  {signature.Key}");

                // Every named marker, against the entry cue (the first marker) and the exit cue (the last).
                var named = segments
                    .SelectMany(segment => segment.ArrayMarkersList
                        .Where(marker => !string.IsNullOrEmpty(marker.MarkerName))
                        .Select(marker => (Name: marker.MarkerName!.TrimEnd('\0'),
                            FromEntry: marker.Position - segment.ArrayMarkersList.First().Position,
                            BeforeExit: segment.ArrayMarkersList.Last().Position - marker.Position,
                            Share: segment.Duration == 0 ? 0 : marker.Position / segment.Duration)))
                    .GroupBy(entry => entry.Name);

                foreach (var marker in named)
                {
                    static string Spread(IEnumerable<double> values)
                    {
                        var sorted = values.OrderBy(value => value).ToList();
                        return $"min {sorted[0]:0}, median {sorted[sorted.Count / 2]:0}, max {sorted[^1]:0}";
                    }

                    Console.WriteLine($"  '{marker.Key}' x{marker.Count()}: from entry {Spread(marker.Select(entry => entry.FromEntry))}; " +
                        $"before exit {Spread(marker.Select(entry => entry.BeforeExit))}; " +
                        $"share of segment {Spread(marker.Select(entry => entry.Share * 100))}%");
                }
            }
        }

        /// <summary>
        /// Both of Araby's chains walked to the wem, out of the pack that ships.
        ///
        /// Prebattle plays and battle does not, and the two go through different containers to
        /// different Random Sequences. The prebattle one is a working control: whatever the battle
        /// chain does differently from it is the difference between audio and silence.
        /// </summary>
        [Test]
        public void BothOfArabysChainsWalkedToTheWem()
        {
            var stagePack = Path.Combine(Path.GetDirectoryName(ModPackPath)!, "araby_stage_campaign.pack");
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var pack = VanillaBankReader.OpenPack(provider, stagePack, markAsCa: false);

            var wemsInPack = pack.GetAllFiles()
                .Where(file => file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(file => Path.GetFileNameWithoutExtension(file.Key), file => file.Value.DataSource.Size);

            var bankFile = pack.GetAllFiles()
                .Single(file => file.Key.EndsWith("global_music__core.bnk", StringComparison.OrdinalIgnoreCase));

            var hircs = BnkFile.CreateFromBytes(bankFile.Value.DataSource.ReadData(), bankFile.Key, false).HircChunk.HircItems;
            var byId = hircs.ToDictionary(hirc => hirc.Id);
            var arabyKey = WwiseHash.Compute("araby");

            Console.WriteLine($"wems in the pack: {string.Join(", ", wemsInPack.Select(wem => $"{wem.Key} ({wem.Value} bytes)"))}\n");

            foreach (var (containerId, label) in new (uint, string)[] { (698158058, "prebattle, plays"), (26264058, "battle, silent") })
            {
                var container = (CAkMusicSwitchCntr_V136)byId[containerId];
                var leaf = PathsOf(container)
                    .Where(path => path.Key.Split(" / ").Contains(arabyKey.ToString()))
                    .Select(path => path.Value)
                    .Distinct()
                    .ToList();

                Console.WriteLine($"container {containerId} ({label}) -> araby reaches {string.Join(", ", leaf)}");

                foreach (var ranSeqId in leaf)
                {
                    if (!byId.TryGetValue(ranSeqId, out var hirc) || hirc is not CAkMusicRanSeqCntr_V136 ranSeq)
                    {
                        Console.WriteLine($"    {ranSeqId} is not a Music Random Sequence in this .bnk");
                        continue;
                    }

                    foreach (var segmentId in ranSeq.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                    {
                        var segment = (CAkMusicSegment_V136)byId[segmentId];
                        Console.WriteLine($"    segment {segmentId}  duration {segment.Duration}ms, " +
                            $"{segment.ArrayMarkersList.Count} markers, parent {segment.MusicNodeParams.NodeBaseParams.DirectParentId}");

                        foreach (var trackId in segment.MusicNodeParams.Children.ChildIds)
                        {
                            var track = (CAkMusicTrack_V136)byId[trackId];

                            foreach (var source in track.SourceList)
                                Console.WriteLine($"      track {trackId} source {source.AkMediaInformation.SourceId} " +
                                    $"type {source.StreamType}, declared {source.AkMediaInformation.InMemoryMediaSize} bytes, " +
                                    $"wem in pack: {(wemsInPack.TryGetValue(source.AkMediaInformation.SourceId.ToString(), out var size) ? $"yes, {size} bytes" : "NO")}");

                            foreach (var item in track.PlaylistList)
                                Console.WriteLine($"      playlist item source {item.SourceId} " +
                                    $"playAt {item.PlayAt}, begin {item.BeginTrimOffset}, end {item.EndTrimOffset}, duration {item.SrcDuration}");
                        }
                    }
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Whether every vanilla decision tree is sorted the way the merge sorts, and whether every
        /// container survives being merged with itself byte for byte.
        ///
        /// A branch cannot just be tacked on the end of a block - the runtime looks a key up within a
        /// block rather than scanning it, so the order is part of the format. The merge sorts every
        /// block by key ascending, which is only correct if that is what vanilla does; where it is
        /// not, the merge silently reorders vanilla's own children and the damage is invisible to any
        /// check that compares paths, because a reordered block resolves the same paths.
        ///
        /// Merging a container with itself is the sharp version of the question: it adds nothing, so
        /// anything but the original bytes back is the flattening imposing an order of its own.
        /// </summary>
        [TestCase("global_music__core.bnk")]
        [TestCase("campaign_music__core.bnk")]
        public void WhetherEveryVanillaTreeIsSortedTheWayTheMergeSortsIt(string bankName)
        {
            var vanilla = VanillaBankReader.ReadMusicBanks(Path.Combine(GameDirectory, "data", "audio_base_bnk.pack"))
                .First(bank => bank.Path.EndsWith(bankName, StringComparison.OrdinalIgnoreCase));

            var sections = WalkHircSections(vanilla.Bytes).ToDictionary(section => section.Id, section => section.Section);
            var containers = BnkFile.CreateFromBytes(vanilla.Bytes, vanilla.Path, false)
                .HircChunk.HircItems.OfType<CAkMusicSwitchCntr_V136>().ToList();

            var mergeService = new Editors.Audio.Shared.Wwise.Generators.MusicSwitchContainerMergeService();
            var unsorted = new List<string>();
            var notIdentical = new List<string>();

            foreach (var container in containers)
            {
                var outOfOrder = UnsortedBlocks(container);
                if (outOfOrder.Count != 0)
                    unsorted.Add($"{container.Id}: {outOfOrder.Count} blocks out of ascending key order, first is {outOfOrder[0]}");

                var copied = mergeService.MergeContainers(container, container);
                copied.UpdateSectionSize();

                var written = copied.WriteData();
                var original = sections[container.Id];

                if (written.Length != original.Length || !written.AsSpan().SequenceEqual(original))
                    notIdentical.Add($"{container.Id}: {written.Length} bytes against vanilla's {original.Length}, " +
                        $"body diverges at {FirstDifference(written[9..], original[9..])}");
            }

            Console.WriteLine($"{bankName}: {containers.Count} Music Switch containers");
            Console.WriteLine($"  blocks not in ascending key order: {unsorted.Count} containers");
            foreach (var offender in unsorted)
                Console.WriteLine($"    {offender}");

            Console.WriteLine($"  do not survive a merge with themselves: {notIdentical.Count} containers");
            foreach (var offender in notIdentical)
                Console.WriteLine($"    {offender}");

            Assert.Multiple(() =>
            {
                Assert.That(unsorted, Is.Empty, "vanilla does not sort every block by ascending key");
                Assert.That(notIdentical, Is.Empty, "a container does not survive being merged with itself");
            });
        }

        /// <summary>Every child block whose keys are not in ascending order, as read from the bytes
        /// the container writes.</summary>
        static List<string> UnsortedBlocks(CAkMusicSwitchCntr_V136 container)
        {
            const int recordSize = 12;
            var treeBytes = container.AkDecisionTree.WriteData();
            var offenders = new List<string>();

            void Walk(int index, int depth)
            {
                if (depth == container.TreeDepth)
                    return;

                var childrenIdx = BitConverter.ToUInt16(treeBytes, index * recordSize + 4);
                var childrenCount = BitConverter.ToUInt16(treeBytes, index * recordSize + 6);

                var keys = Enumerable.Range(0, childrenCount)
                    .Select(offset => BitConverter.ToUInt32(treeBytes, (childrenIdx + offset) * recordSize))
                    .ToList();

                for (var position = 1; position < keys.Count; position++)
                {
                    if (keys[position] < keys[position - 1])
                    {
                        offenders.Add($"depth {depth}, block at {childrenIdx}: {keys[position - 1]} then {keys[position]}");
                        break;
                    }
                }

                for (var offset = 0; offset < childrenCount; offset++)
                    Walk(childrenIdx + offset, depth + 1);
            }

            Walk(0, 0);
            return offenders;
        }

        /// <summary>
        /// Whether every chain of every slot carries the araby arm, in the .dat files that ship.
        ///
        /// A slot is not one place in the file. The battle theme is decided in three separate
        /// functions and the ambient chain is repeated nine times, and a slot only works if every one
        /// of its chains answers - a culture the main dispatch knows but a second route does not is a
        /// culture that resolves down one path and falls off another. Prebattle plays and battle does
        /// not, so what matters is which chains actually got the arm.
        /// </summary>
        [Test]
        public void WhetherEveryChainOfEverySlotCarriesTheArabyArm()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var files = VanillaBankReader.OpenPack(provider, ModPackPath, markAsCa: false).GetAllFiles();

            foreach (var name in new[] { "battle_music.dat", "campaign_music.dat" })
            {
                var packFile = files.Single(file => file.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase)).Value;
                Console.WriteLine($"\n================ {name} ================");

                foreach (var slot in MusicDatCultureWiring.FindSlots(MusicDatParser.Parse(packFile)))
                {
                    var withArm = slot.Chains
                        .Where(chain => chain.Cases.Any(item => string.Equals(item.MatchKey, "araby", StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    Console.WriteLine($"\n-- {slot.Title}  (event prefix {slot.EventPrefix})");
                    Console.WriteLine($"   {withArm.Count} of {slot.Chains.Count} chains carry an araby arm");

                    foreach (var chain in slot.Chains)
                    {
                        var araby = chain.Cases.FirstOrDefault(item => string.Equals(item.MatchKey, "araby", StringComparison.OrdinalIgnoreCase));
                        var cathay = chain.Cases.FirstOrDefault(item => string.Equals(item.MatchKey, "cathay", StringComparison.OrdinalIgnoreCase));

                        Console.WriteLine($"      {chain.FunctionName} (matched against {chain.MatchedAgainst}, {chain.Cases.Count} arms)");
                        Console.WriteLine($"          cathay: {(cathay == null ? "ABSENT" : cathay.SoundEvent ?? $"<culture:{cathay.MusicalCulture}>")}");
                        Console.WriteLine($"          araby:  {(araby == null ? "ABSENT" : araby.SoundEvent ?? $"<culture:{araby.MusicalCulture}>")}");
                    }
                }
            }
        }

        /// <summary>
        /// Which vanilla .bnk holds each music Event, against which .bnk the mod puts its own in.
        ///
        /// The script posts music_b_faction_araby and the containers now resolve araby, so the piece
        /// left is whether the Event is somewhere battle can see it. Banks are loaded per context, and
        /// an Event that is not in a .bnk loaded at the time is an Event that goes nowhere. The mod
        /// puts all six of its Events in global_music__core.bnk; vanilla's own placement is the answer
        /// to where they should be.
        /// </summary>
        [Test]
        public void WhichVanillaBankHoldsEachMusicEvent()
        {
            var wanted = new[]
            {
                "music_b_faction_cathay", "music_b_faction_empire", "music_b_faction_kislev",
                "music_c_subculture_cathay", "music_c_ams_pulse_perc_cathay"
            }.ToDictionary(WwiseHash.Compute, name => name);

            var found = new Dictionary<string, List<string>>();

            foreach (var packPath in Directory.GetFiles(Path.Combine(GameDirectory, "data"), "audio*.pack"))
            {
                foreach (var (path, bytes) in VanillaBankReader.ReadMusicBanks(packPath))
                {
                    List<HircItem> hircs;
                    try
                    {
                        hircs = BnkFile.CreateFromBytes(bytes, path, false).HircChunk?.HircItems ?? [];
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var hirc in hircs.Where(hirc => wanted.ContainsKey(hirc.Id)))
                    {
                        var name = wanted[hirc.Id];
                        if (!found.TryGetValue(name, out var places))
                            found[name] = places = [];

                        places.Add($"{Path.GetFileName(packPath)}  {Path.GetFileName(path)}  ({hirc.HircType})");
                    }
                }
            }

            foreach (var (name, places) in found.OrderBy(entry => entry.Key))
            {
                Console.WriteLine($"\n{name} ({WwiseHash.Compute(name)})");
                foreach (var place in places)
                    Console.WriteLine($"    {place}");
            }

            foreach (var name in wanted.Values.Where(name => !found.ContainsKey(name)))
                Console.WriteLine($"\n{name}: not found in any .bnk");
        }

        static void Record(Dictionary<string, (int Total, int Mismatched, string FirstExample)> results, string type, bool identical, string example)
        {
            results.TryGetValue(type, out var current);
            results[type] = (current.Total + 1,
                current.Mismatched + (identical ? 0 : 1),
                current.FirstExample ?? example);
        }

        static string FirstDifference(byte[] left, byte[] right)
        {
            var shared = Math.Min(left.Length, right.Length);
            for (var offset = 0; offset < shared; offset++)
                if (left[offset] != right[offset])
                    return $"byte {offset} ({left[offset]:x2} vs {right[offset]:x2})";
            return $"byte {shared} (length only)";
        }

        /// <summary>Every hirc in the HIRC chunk as the raw bytes it occupies, header included.</summary>
        static IEnumerable<(uint Index, uint Id, byte[] Section)> WalkHircSections(byte[] bankBytes)
        {
            var offset = 0;
            var hircChunkStart = -1;

            while (offset + 8 <= bankBytes.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(bankBytes, offset, 4);
                var length = BitConverter.ToUInt32(bankBytes, offset + 4);

                if (tag == "HIRC")
                {
                    hircChunkStart = offset;
                    break;
                }

                offset += 8 + (int)length;
            }

            if (hircChunkStart == -1)
                throw new InvalidDataException("no HIRC chunk");

            var itemCount = BitConverter.ToUInt32(bankBytes, hircChunkStart + 8);
            var cursor = hircChunkStart + 12;

            for (uint index = 0; index < itemCount; index++)
            {
                var sectionSize = BitConverter.ToUInt32(bankBytes, cursor + 1);
                var id = BitConverter.ToUInt32(bankBytes, cursor + 5);
                var totalLength = 5 + (int)sectionSize;

                yield return (index, id, bankBytes[cursor..(cursor + totalLength)]);
                cursor += totalLength;
            }
        }
    }
}
