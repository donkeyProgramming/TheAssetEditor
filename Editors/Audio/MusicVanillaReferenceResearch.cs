using System.Diagnostics;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Test.Audio
{
    /// <summary>
    /// What the Music Project tool's vanilla reference panel shows, against a real install: how long
    /// each Group takes to index, which cultures it finds, and one culture's clips. Worth re-running
    /// after a game update, since everything it reads is vanilla.
    /// </summary>
    [Explicit("Research probe, needs a WH3 install, run manually")]
    internal class MusicVanillaReferenceResearch
    {
        const string GameDirectory = @"D:\SteamLibrary\steamapps\common\Total War WARHAMMER III";

        /// <summary>The levels of each culture container - the State Groups a branch is chosen by,
        /// outermost first - and the values vanilla lists at each, which is what the reference
        /// panel's "where it plays" is made of.</summary>
        [TestCase(432582457u)]
        [TestCase(26264058u)]
        [TestCase(67383790u)]
        [TestCase(698158058u)]
        [TestCase(145953291u)]
        public void WhatTheLevelsOfACultureContainerAre(uint containerId)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));
            var repository = provider.GetRequiredService<IAudioRepository>();
            repository.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)]);

            var container = repository.GetHircs(containerId).OfType<Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSwitchCntr_V136>().First(hirc => hirc.IsCA);
            var paths = new Editors.Audio.Shared.Wwise.HircExploration.StatePathParser(repository).GetStatePaths(container).StatePaths;
            Console.WriteLine($"{containerId}: {paths.Count} paths");
            if (paths.Count < 40)
                foreach (var path in paths)
                    Console.WriteLine($"    {path.GetAsString(" > ")} -> {path.ChildNodeId}");
            for (var level = 0; level < container.Arguments.Count; level++)
            {
                var values = paths.Select(path => path.Items[level].DisplayName).Distinct().ToList();
                Console.WriteLine($"  level {level + 1}: {repository.GetNameFromId(container.Arguments[level].GroupId)} - {values.Count} value(s): " +
                    string.Join(", ", values.Take(14)) + (values.Count > 14 ? ", ..." : ""));
            }
        }

        /// <summary>What the nine pulse slots are: each vanilla pulse track, walked up through its
        /// parents to the music it plays under, with how long its segment runs.</summary>
        [TestCase("WH3_AMS_Pulse_Percussion_Options")]
        [TestCase("WH3_AMS_Pulse_Pitched_Orchestral_Options")]
        public void WhatThePulseSlotsAre(string stateGroupName)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));
            var repository = provider.GetRequiredService<IAudioRepository>();
            repository.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)]);

            Shared.GameFormats.Wwise.Hirc.HircItem? Find(uint id) => repository.GetHircs(id).FirstOrDefault(hirc => hirc.IsCA);
            uint ParentOf(Shared.GameFormats.Wwise.Hirc.HircItem hirc) => hirc switch
            {
                Shared.GameFormats.Wwise.Hirc.V136.CAkMusicTrack_V136 track => track.NodeBaseParams.DirectParentId,
                Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSegment_V136 segment => segment.MusicNodeParams.NodeBaseParams.DirectParentId,
                Shared.GameFormats.Wwise.Hirc.V136.CAkMusicRanSeqCntr_V136 sequence => sequence.MusicTransNodeParams.MusicNodeParams.NodeBaseParams.DirectParentId,
                Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSwitchCntr_V136 musicSwitch => musicSwitch.MusicTransNodeParams.MusicNodeParams.NodeBaseParams.DirectParentId,
                _ => 0
            };

            var tracks = repository.GetVanillaAmsPulseTracks(stateGroupName).DistinctBy(track => track.Id).ToList();
            foreach (var track in tracks)
            {
                var chain = new List<string>();
                Shared.GameFormats.Wwise.Hirc.HircItem? hirc = track;
                for (var depth = 0; depth < 8 && hirc != null; depth++)
                {
                    var name = repository.GetNameFromId(hirc.Id, out var found);
                    var extra = hirc is Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSegment_V136 segment ? $" {segment.Duration / 1000:0.0}s, {segment.MusicNodeParams.Children.ChildIds.Count} tracks" : "";
                    var switchArgs = hirc is Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSwitchCntr_V136 sw ? " on " + string.Join(">", sw.Arguments.Select(argument => repository.GetNameFromId(argument.GroupId))) : "";
                    chain.Add($"{hirc.HircType}{(found ? $" '{name}'" : "")} {hirc.Id}{extra}{switchArgs}");
                    var parentId = ParentOf(hirc);
                    hirc = parentId == 0 ? null : Find(parentId);
                }
                Console.WriteLine($"track {track.Id} in {track.BnkFilePath}:\n    " + string.Join("\n    < ", chain));
            }
        }

        /// <summary>The named cues on vanilla segments under a container - what a generated segment
        /// there has to carry for the music scripts to sync against it.</summary>
        [TestCase(698158058u)]
        [TestCase(145953291u)]
        public void WhatCuesTheSegmentsOfAContainerCarry(uint containerId)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));
            var repository = provider.GetRequiredService<IAudioRepository>();
            repository.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)]);

            var container = repository.GetHircs(containerId).OfType<Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSwitchCntr_V136>().First(hirc => hirc.IsCA);
            var cueSets = new Dictionary<string, int>();
            var pending = new Stack<uint>(container.MusicTransNodeParams.MusicNodeParams.Children.ChildIds);
            var visited = new HashSet<uint>();
            while (pending.Count != 0)
            {
                var id = pending.Pop();
                if (!visited.Add(id))
                    continue;
                switch (repository.GetHircs(id).FirstOrDefault(hirc => hirc.IsCA))
                {
                    case Shared.GameFormats.Wwise.Hirc.V136.CAkMusicRanSeqCntr_V136 sequence:
                        foreach (var child in sequence.MusicTransNodeParams.MusicNodeParams.Children.ChildIds) pending.Push(child);
                        break;
                    case Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSwitchCntr_V136 nested:
                        foreach (var child in nested.MusicTransNodeParams.MusicNodeParams.Children.ChildIds) pending.Push(child);
                        break;
                    case Shared.GameFormats.Wwise.Hirc.V136.CAkMusicSegment_V136 segment:
                        var cues = string.Join(", ", segment.ArrayMarkersList.Select(marker => $"{marker.MarkerName?.TrimEnd('\0') ?? $"#{marker.Id}"}"));
                        cueSets[cues] = cueSets.GetValueOrDefault(cues) + 1;
                        break;
                }
            }

            foreach (var (cues, count) in cueSets.OrderByDescending(entry => entry.Value))
                Console.WriteLine($"{containerId}: {count} segment(s) with markers [{cues}]");
        }

        /// <summary>Which vanilla clips the player cannot parse, and how their bytes compare with
        /// the size their RIFF header claims.</summary>
        [Test]
        public void WhichClipWemsFailToParse()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));
            var repository = provider.GetRequiredService<IAudioRepository>();
            repository.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)]);
            var service = provider.GetRequiredService<IMusicVanillaReferenceService>();
            var packFileService = provider.GetRequiredService<IPackFileService>();

            foreach (var stateGroupName in new[] { "Battle_Music_WH3_Culture", "WH3_Campaign_Subcultures", "WH3_Campaign_Music_AMS_Fragments_Faction",
                         "WH3_AMS_Pulse_Percussion_Options", "WH3_AMS_Pulse_Pitched_Orchestral_Options", "WH3_AMS_Pulse_Pitched_Ethnic_Options" })
            {
                int total = 0, failed = 0, withTail = 0;
                foreach (var state in service.GetStates(stateGroupName))
                foreach (var clip in service.GetClips(stateGroupName, state).DistinctBy(clip => clip.SourceId))
                {
                    total++;
                    var wem = repository.FindWem(clip.SourceId.ToString());
                    if (wem == null) continue;
                    var bytes = wem.DataSource.ReadData();
                    try
                    {
                        var wemFile = Shared.GameFormats.Wwise.Wem.V132.WemFile.CreateFromWemBytes(bytes);
                        Shared.GameFormats.Audio.Containers.Ogg.OggFile.CreateFromWemBytes(bytes);
                        if (wemFile.DataChunk.TrailingBytes.Length != 0)
                        {
                            withTail++;
                            if (!wemFile.WriteData().SequenceEqual(bytes))
                                throw new InvalidOperationException("does not write back unchanged");
                        }
                    }
                    catch (Exception e)
                    {
                        failed++;
                        if (failed <= 4)
                        {
                            var riffSize = BitConverter.ToUInt32(bytes, 4) + 8;
                            Console.WriteLine($"  {stateGroupName} {state} {clip.SourceId}: {packFileService.GetFullPath(wem)} ({packFileService.GetPackFileContainer(wem)?.Name}) bytes {bytes.Length}, RIFF says {riffSize}: {e.GetType().Name}");
                        }
                    }
                }
                Console.WriteLine($"{stateGroupName}: {failed} of {total} fail, {withTail} have a trailing tail");
            }
        }

        /// <summary>The chunk layout of failing and working wems, and where walking the data
        /// chunk's length-prefixed packets runs out.</summary>
        [TestCase(178195182u)]
        [TestCase(948229231u)]
        [TestCase(833271226u)]
        [TestCase(632387060u)]
        [TestCase(297263506u)]
        public void WhatAWemLooksLike(uint sourceId)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
                VanillaBankReader.OpenPack(provider, Path.Combine(GameDirectory, "data", packName));
            var repository = provider.GetRequiredService<IAudioRepository>();
            var packFileService = provider.GetRequiredService<IPackFileService>();
            var wem = repository.FindWem(sourceId.ToString());
            var b = wem.DataSource.ReadData();
            Console.WriteLine($"{sourceId} in {packFileService.GetPackFileContainer(wem)?.Name}, {b.Length} bytes");

            var offset = 12;
            int dataStart = 0, dataSize = 0;
            while (offset + 8 <= b.Length)
            {
                var tag = System.Text.Encoding.ASCII.GetString(b, offset, 4);
                var size = BitConverter.ToInt32(b, offset + 4);
                Console.WriteLine($"  chunk '{tag}' at {offset} size {size}");
                if (tag == "fmt ")
                {
                    var f = offset + 8;
                    Console.WriteLine($"    channels {BitConverter.ToUInt16(b, f + 2)} rate {BitConverter.ToUInt32(b, f + 4)} extra {BitConverter.ToUInt16(b, f + 16)} " +
                        $"samples {BitConverter.ToInt32(b, f + 24)} setup {BitConverter.ToUInt32(b, f + 28)} audioOffset {BitConverter.ToUInt32(b, f + 32)} " +
                        $"seekTable {BitConverter.ToUInt32(b, f + 40)} firstPacket {BitConverter.ToUInt32(b, f + 44)} maxPacket {BitConverter.ToUInt16(b, f + 48)}");
                }
                if (tag == "data") { dataStart = offset + 8; dataSize = size; }
                offset += 8 + size + (size & 1);
            }

            var fmtAt = 12 + 8;
            var seek = (int)BitConverter.ToUInt32(b, fmtAt + 40);
            var first = (int)BitConverter.ToUInt32(b, fmtAt + 44);
            var p = dataStart + first;
            var end = dataStart + dataSize;
            int packets = 0;
            while (p + 2 <= end)
            {
                var len = BitConverter.ToUInt16(b, p);
                if (p + 2 + len > end)
                {
                    Console.WriteLine($"  packet {packets} at {p - dataStart} claims {len}, only {end - p - 2} left; next bytes {BitConverter.ToString(b, p, Math.Min(12, b.Length - p))}");
                    break;
                }
                p += 2 + len; packets++;
            }
            Console.WriteLine($"  walked {packets} packets, stopped {end - p} bytes before the end of data (seek table {seek}, first packet {first})");
        }

        [TestCase("cathay")]
        public void WhatVanillaGivesACulture(string culture)
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();
            var loader = provider.GetRequiredService<IPackFileContainerLoader>();
            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack", "audio_base_m.pack" })
            {
                var container = loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(GameDirectory, "data", packName), true);
                container.IsCaPackFile = true;
                packFileService.AddContainer(container);
            }

            // What the tool does as soon as a project is open, before the panel is ever shown.
            var warmUp = Stopwatch.StartNew();
            provider.GetRequiredService<IMusicAudioProjectService>().WarmUpAsync().Wait();
            Console.WriteLine($"Warm-up: {warmUp.ElapsedMilliseconds} ms");
            var service = provider.GetRequiredService<IMusicVanillaReferenceService>();

            foreach (var prefix in new[] { "music_b_faction_", "music_c_subculture_", "music_c_ams_", "music_c_ams_pulse_perc_", "music_c_ams_pulse_orch_", "music_c_ams_pulse_ethnic_" })
            {
                Wh3MusicEventInformation.TryResolveStateTarget(prefix + culture, out var stateGroupName, out var stateName);

                var stopwatch = Stopwatch.StartNew();
                var states = service.GetStates(stateGroupName);
                var indexMs = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();
                var clips = service.GetClips(stateGroupName, stateName);

                Console.WriteLine($"\n{stateGroupName}: indexed in {indexMs} ms, {states.Count} culture(s): {string.Join(", ", states)}");
                Console.WriteLine($"  '{stateName}': {clips.Count} clip(s), {clips.Select(clip => clip.SourceId).Distinct().Count()} file(s), " +
                    $"{clips.Count(clip => clip.AlsoUsedBy.Count != 0)} shared, read in {stopwatch.ElapsedMilliseconds} ms");

                foreach (var clip in clips.Take(8))
                    Console.WriteLine($"    {clip.SourceId,10}  {(clip.DurationMs is double ms ? $"{ms / 1000:0.0} s" : "?"),8}  {clip.Where}" +
                        (clip.AlsoUsedBy.Count != 0 ? $"  [also {string.Join(", ", clip.AlsoUsedBy.Take(4))}{(clip.AlsoUsedBy.Count > 4 ? ", ..." : "")}]" : ""));

                foreach (var level in service.GetTreeLevels(stateGroupName))
                    Console.WriteLine($"  tree level {level.DisplayName} ({level.Name}) in {string.Join(", ", level.ContainerIds)}: " +
                        string.Join(", ", level.Values.Take(12).Select(value => Wh3MusicTreeLevelInformation.GetValueName(level.Name, value))) + (level.Values.Count > 12 ? $", ... ({level.Values.Count})" : ""));

                var found = clips.Count(clip => provider.GetRequiredService<IAudioRepository>().FindWem(clip.SourceId.ToString()) != null);
                Console.WriteLine($"  {found} of {clips.Count} clip wems found in the loaded packs");
            }
        }
    }
}
