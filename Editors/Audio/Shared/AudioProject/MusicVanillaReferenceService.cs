using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Wem.V132;

namespace Editors.Audio.Shared.AudioProject
{
    /// <param name="Where">What part of the music the clip plays in, e.g. which container and
    /// which branch below the culture.</param>
    /// <param name="DurationMs">The source's length, or null where vanilla does not record it and
    /// the wem could not be read.</param>
    /// <param name="AlsoUsedBy">Other States in the same Group whose music plays this same file -
    /// vanilla shares stems between cultures in places, so a culture's file count is not all its own.</param>
    public record VanillaMusicClip(uint SourceId, string Where, double? DurationMs, IReadOnlyList<string> AlsoUsedBy);

    /// <summary>One level a <see cref="Models.MusicTree"/> can split on, with the values vanilla
    /// gives a culture's music at it.</summary>
    /// <param name="Name">The State Group, or <see cref="Wh3MusicTreeLevelInformation.ContainerLevel"/>.</param>
    /// <param name="ContainerIds">The containers that have the level; a split on it is passed over
    /// in the others.</param>
    public record MusicTreeLevel(string Name, IReadOnlyList<string> Values, IReadOnlyList<uint> ContainerIds)
    {
        public string DisplayName => Wh3MusicTreeLevelInformation.GetLevelName(Name);
    }

    /// <summary>
    /// What vanilla gives each culture for a music slot, read out of the game's banks, so a modder
    /// can see and hear how much music a slot usually gets and what it sounds like before supplying
    /// their own.
    ///
    /// The slots are reached by three different mechanisms, the same three the compile writes to -
    /// see <see cref="Wh3MusicHierarchyInformation"/>:
    ///
    ///   decision trees     the battle theme and the campaign map theme. Every path through a Music
    ///                      Switch container naming the culture is followed down to its tracks.
    ///   switch tracks      the three pulses. Each of nine vanilla tracks has a sub-track per culture.
    ///   a Switch container the ambient fragments, branching on the culture, then on the key, then on
    ///                      whether the fragment is a call or a response.
    ///
    /// Only the game's own hircs are read, so music the project has already been built into the
    /// editable pack does not show up as vanilla.
    ///
    /// The repository fills its caches on first use and is not safe to read from two threads, so
    /// this is meant to be called from the UI thread. Each Group is indexed once and then cached.
    /// </summary>
    public interface IMusicVanillaReferenceService
    {
        /// <summary>Every State vanilla gives audio of its own for the Group, by name.</summary>
        IReadOnlyList<string> GetStates(string stateGroupName);

        /// <summary>The clips vanilla plays for the State, grouped in the order the music is read.</summary>
        IReadOnlyList<VanillaMusicClip> GetClips(string stateGroupName, string stateName);

        /// <summary>What a decision tree for the Group can split on: the part of the game, where the
        /// Group's music plays in more than one, then every level of each container other than the
        /// culture, with the values vanilla gives cultures there. Empty for a Group no Music Switch
        /// container branches on.</summary>
        IReadOnlyList<MusicTreeLevel> GetTreeLevels(string stateGroupName);
    }

    public class MusicVanillaReferenceService(IAudioRepository audioRepository, IAmsMusicLayout amsMusicLayout) : IMusicVanillaReferenceService
    {
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IAmsMusicLayout _amsMusicLayout = amsMusicLayout;
        private readonly Dictionary<string, Dictionary<uint, List<IndexedClip>>> _indexByStateGroup = [];
        private readonly Dictionary<uint, double?> _wemDurationsMs = [];
        private readonly Dictionary<string, IReadOnlyList<MusicTreeLevel>> _treeLevelsByStateGroup = [];

        // Looking hircs up one id at a time is a database query each, which made indexing the battle
        // theme or the fragments take eight seconds. The types this walks are fetched whole instead,
        // once - Sounds included, which the music project service has already fetched by the time
        // the tool asks.
        private Dictionary<uint, HircItem> _vanillaMusicHircsById;

        private sealed record IndexedClip(uint SourceId, string Where, double? DurationMs);

        public IReadOnlyList<string> GetStates(string stateGroupName)
        {
            return GetIndex(stateGroupName).Keys
                .Select(_audioRepository.GetNameFromId)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<VanillaMusicClip> GetClips(string stateGroupName, string stateName)
        {
            var index = GetIndex(stateGroupName);
            if (!index.TryGetValue(WwiseHash.Compute(stateName), out var clips))
                return [];

            var statesBySourceId = index
                .SelectMany(entry => entry.Value.Select(clip => (clip.SourceId, StateId: entry.Key)))
                .GroupBy(item => item.SourceId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.StateId).Distinct().ToList());

            var stateId = WwiseHash.Compute(stateName);
            return clips
                .Select(clip => new VanillaMusicClip(
                    clip.SourceId,
                    clip.Where,
                    clip.DurationMs ?? GetWemDurationMs(clip.SourceId),
                    statesBySourceId[clip.SourceId]
                        .Where(otherStateId => otherStateId != stateId)
                        .Select(_audioRepository.GetNameFromId)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList()))
                .ToList();
        }

        public IReadOnlyList<MusicTreeLevel> GetTreeLevels(string stateGroupName)
        {
            if (_treeLevelsByStateGroup.TryGetValue(stateGroupName, out var cached))
                return cached;

            var levels = new List<MusicTreeLevel>();

            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(stateGroupName))
            {
                var slots = _amsMusicLayout.GetPulseSlots(stateGroupName);
                levels.Add(new MusicTreeLevel(Wh3MusicTreeLevelInformation.AmsKeyLevel, [.. slots.Select(slot => slot.Key).Where(key => key != null).Distinct()], []));
                levels.Add(new MusicTreeLevel(Wh3MusicTreeLevelInformation.PulseVariationLevel, [.. slots.Select(slot => slot.Variation.ToString()).Distinct().Order()], []));
                _treeLevelsByStateGroup[stateGroupName] = levels;
                return levels;
            }

            if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(stateGroupName) is uint fragmentContainerId)
            {
                levels.Add(new MusicTreeLevel(Wh3MusicTreeLevelInformation.AmsKeyLevel, _amsMusicLayout.GetFragmentKeys(fragmentContainerId), []));
                levels.Add(new MusicTreeLevel(AmsMusicLayout.CallResponseStateGroupName, [AmsMusicLayout.Call, AmsMusicLayout.Response], []));
                _treeLevelsByStateGroup[stateGroupName] = levels;
                return levels;
            }

            var stateGroupId = WwiseHash.Compute(stateGroupName);
            var statePathParser = new StatePathParser(_audioRepository);
            var containerIds = Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds(stateGroupName);

            if (containerIds.Count > 1)
                levels.Add(new MusicTreeLevel(Wh3MusicTreeLevelInformation.ContainerLevel, [.. containerIds.Select(id => id.ToString())], containerIds));

            // A level more than one container has - the faction, for the campaign map and its
            // resolution screens - is offered once, for all of them.
            var valuesByLevel = new Dictionary<string, (SortedSet<string> Values, List<uint> ContainerIds)>();
            var levelOrder = new List<string>();

            foreach (var containerId in containerIds)
            {
                if (GetVanillaHirc(containerId) is not CAkMusicSwitchCntr_V136 container)
                    continue;

                var cultureLevel = container.Arguments.FindIndex(argument => argument.GroupId == stateGroupId);
                if (cultureLevel < 0)
                    continue;

                // Only the paths for real cultures: the values there are the ones a culture's music
                // can actually be told apart by, and above the culture they are exactly the blocks a
                // new culture's branch goes into.
                var culturePaths = statePathParser.GetStatePaths(container).StatePaths
                    .Where(statePath => statePath.Items[cultureLevel].Value != 0)
                    .ToList();

                for (var level = 0; level < container.Arguments.Count; level++)
                {
                    if (level == cultureLevel)
                        continue;

                    var levelName = _audioRepository.GetNameFromId(container.Arguments[level].GroupId);
                    if (!valuesByLevel.TryGetValue(levelName, out var entry))
                    {
                        valuesByLevel[levelName] = entry = (new SortedSet<string>(StringComparer.OrdinalIgnoreCase), []);
                        levelOrder.Add(levelName);
                    }

                    entry.ContainerIds.Add(containerId);
                    foreach (var statePath in culturePaths.Where(statePath => statePath.Items[level].Value != 0))
                        entry.Values.Add(statePath.Items[level].DisplayName);
                }
            }

            foreach (var levelName in levelOrder)
            {
                var (values, levelContainerIds) = valuesByLevel[levelName];
                if (values.Count != 0)
                    levels.Add(new MusicTreeLevel(levelName, [.. values], levelContainerIds));
            }

            _treeLevelsByStateGroup[stateGroupName] = levels;
            return levels;
        }

        private Dictionary<uint, List<IndexedClip>> GetIndex(string stateGroupName)
        {
            if (_indexByStateGroup.TryGetValue(stateGroupName, out var index))
                return index;

            index = [];
            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(stateGroupName))
                IndexPulses(stateGroupName, index);
            else if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(stateGroupName) is uint fragmentContainerId)
                IndexFragments(fragmentContainerId, index);
            else
                IndexDecisionTrees(stateGroupName, index);

            _indexByStateGroup[stateGroupName] = index;
            return index;
        }

        private void IndexDecisionTrees(string stateGroupName, Dictionary<uint, List<IndexedClip>> index)
        {
            var stateGroupId = WwiseHash.Compute(stateGroupName);
            var statePathParser = new StatePathParser(_audioRepository);

            foreach (var containerId in Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds(stateGroupName))
            {
                if (GetVanillaHirc(containerId) is not CAkMusicSwitchCntr_V136 container)
                    continue;

                var depth = container.Arguments.FindIndex(argument => argument.GroupId == stateGroupId);
                if (depth < 0)
                    continue;

                foreach (var statePath in statePathParser.GetStatePaths(container).StatePaths)
                {
                    var stateId = statePath.Items[depth].Value;

                    // "Any" is the fallback for a culture with no branch, not a culture of its own.
                    if (stateId == 0)
                        continue;

                    // The other levels say which part of the music this is - the result, the battle
                    // phase, the key - and "Any" there adds nothing worth reading.
                    var otherLevels = statePath.Items
                        .Select((item, level) => (Item: item, Level: level))
                        .Where(entry => entry.Level != depth && entry.Item.Value != 0)
                        .Select(entry =>
                        {
                            var levelName = _audioRepository.GetNameFromId(container.Arguments[entry.Level].GroupId);
                            return $"{Wh3MusicTreeLevelInformation.GetLevelName(levelName)}: {Wh3MusicTreeLevelInformation.GetValueName(levelName, entry.Item.DisplayName)}";
                        });
                    var where = string.Join(" · ", new[] { Wh3MusicTreeLevelInformation.GetContainerName(containerId) }.Concat(otherLevels));

                    foreach (var (sourceId, durationMs) in GetTrackClips(statePath.ChildNodeId))
                        Add(index, stateId, new IndexedClip(sourceId, where, durationMs));
                }
            }
        }

        private void IndexPulses(string stateGroupName, Dictionary<uint, List<IndexedClip>> index)
        {
            foreach (var slot in _amsMusicLayout.GetPulseSlots(stateGroupName))
            {
                var where = $"Key: {slot.Key ?? "?"} · Variation: {slot.Variation}";
                var associations = slot.Track.SwitchParams!.SwitchAssoc;
                for (var subTrack = 0; subTrack < associations.Count; subTrack++)
                {
                    foreach (var clip in slot.Track.PlaylistList.Where(clip => clip.TrackId == subTrack))
                        Add(index, associations[subTrack], new IndexedClip(clip.SourceId, where, clip.SrcDuration));
                }
            }
        }

        private void IndexFragments(uint fragmentContainerId, Dictionary<uint, List<IndexedClip>> index)
        {
            if (GetVanillaHirc(fragmentContainerId) is not CAkSwitchCntr_V136 factionSwitch)
                return;

            foreach (var culturePackage in GetSwitchPackages(factionSwitch))
            {
                foreach (var keySwitch in culturePackage.NodeIdList.Select(GetVanillaHirc).OfType<CAkSwitchCntr_V136>())
                {
                    // Several keys usually share one branch, so they are listed together rather than
                    // the same files being shown once per key.
                    var keysByNodeId = GetSwitchPackages(keySwitch)
                        .SelectMany(keyPackage => keyPackage.NodeIdList.Select(nodeId => (NodeId: nodeId, Key: AmsMusicLayout.GetKey(_audioRepository.GetNameFromId(keyPackage.SwitchId)))))
                        .GroupBy(item => item.NodeId);

                    foreach (var keys in keysByNodeId)
                    {
                        // The same silent placeholder a "None" Call/Response branch holds.
                        if (keys.All(item => item.Key == null))
                            continue;

                        var where = $"Key: {string.Join(", ", keys.Select(item => item.Key).Where(key => key != null).Distinct())}";
                        foreach (var (sourceId, branch) in GetSoundSources(keys.Key, null))
                            Add(index, culturePackage.SwitchId, new IndexedClip(sourceId, branch == null ? where : $"{where} · {branch}", null));
                    }
                }
            }
        }

        private static IEnumerable<CAkSwitchCntr_V136.CAkSwitchPackage_V136> GetSwitchPackages(CAkSwitchCntr_V136 container) =>
            container.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>();

        private static void Add(Dictionary<uint, List<IndexedClip>> index, uint stateId, IndexedClip clip)
        {
            if (!index.TryGetValue(stateId, out var clips))
                index[stateId] = clips = [];

            // The same file under the same branch twice, e.g. two segments sharing a stem, says
            // nothing more about the music than once does.
            if (!clips.Any(existing => existing.SourceId == clip.SourceId && existing.Where == clip.Where))
                clips.Add(clip);
        }

        /// <summary>Every clip under a node of the music hierarchy, following random sequences,
        /// segments and nested switches down to the tracks.</summary>
        private IEnumerable<(uint SourceId, double DurationMs)> GetTrackClips(uint hircId)
        {
            var pending = new Stack<uint>([hircId]);
            var visited = new HashSet<uint>();

            while (pending.Count != 0)
            {
                var id = pending.Pop();
                if (!visited.Add(id))
                    continue;

                switch (GetVanillaHirc(id))
                {
                    case CAkMusicRanSeqCntr_V136 randomSequence:
                        foreach (var childId in randomSequence.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                            pending.Push(childId);
                        break;

                    case CAkMusicSegment_V136 segment:
                        foreach (var childId in segment.MusicNodeParams.Children.ChildIds)
                            pending.Push(childId);
                        break;

                    case CAkMusicSwitchCntr_V136 nestedSwitch:
                        foreach (var childId in nestedSwitch.MusicTransNodeParams.MusicNodeParams.Children.ChildIds)
                            pending.Push(childId);
                        break;

                    case CAkMusicTrack_V136 track:
                        foreach (var clip in track.PlaylistList.DistinctBy(clip => clip.SourceId))
                            yield return (clip.SourceId, clip.SrcDuration);
                        break;
                }
            }
        }

        /// <summary>The source of every Sound under a node of the plain hierarchy, with the switch
        /// branch it sits under (Call or Response, for the fragments). A "None" branch is a silent
        /// placeholder embedded in the bank rather than a file, so it is left out.</summary>
        private IEnumerable<(uint SourceId, string Branch)> GetSoundSources(uint hircId, string branch)
        {
            switch (GetVanillaHirc(hircId))
            {
                case ICAkSound sound:
                    if (branch != "None")
                        yield return (sound.GetSourceId(), branch);
                    break;

                case CAkRanSeqCntr_V136 container:
                    foreach (var childId in container.Children.ChildIds)
                        foreach (var source in GetSoundSources(childId, branch))
                            yield return source;
                    break;

                case CAkSwitchCntr_V136 nestedSwitch:
                    foreach (var package in GetSwitchPackages(nestedSwitch))
                        foreach (var nodeId in package.NodeIdList)
                            foreach (var source in GetSoundSources(nodeId, _audioRepository.GetNameFromId(package.SwitchId)))
                                yield return source;
                    break;
            }
        }

        private HircItem GetVanillaHirc(uint id)
        {
            _vanillaMusicHircsById ??= new[]
                {
                    AkBkHircType.Music_Switch, AkBkHircType.Music_Random_Sequence, AkBkHircType.Music_Segment, AkBkHircType.Music_Track,
                    AkBkHircType.SwitchContainer, AkBkHircType.RandomSequenceContainer, AkBkHircType.Sound
                }
                .SelectMany(_audioRepository.GetHircs)
                .Where(hirc => hirc.IsCA)
                .DistinctBy(hirc => hirc.Id)
                .ToDictionary(hirc => hirc.Id);

            return _vanillaMusicHircsById.TryGetValue(id, out var hirc)
                ? hirc
                : _audioRepository.GetHircs(id).FirstOrDefault(hirc => hirc.IsCA);
        }

        /// <summary>A Sound records no length, unlike a music clip, so for the fragments it is read off
        /// the wem itself - only for the State being shown, and once per file.</summary>
        private double? GetWemDurationMs(uint sourceId)
        {
            if (_wemDurationsMs.TryGetValue(sourceId, out var cached))
                return cached;

            double? durationMs = null;
            try
            {
                var wem = _audioRepository.FindWem(sourceId.ToString());
                if (wem != null)
                {
                    var fmt = WemFile.CreateFromWemBytes(wem.DataSource.ReadData()).FmtChunk;
                    durationMs = fmt.SampleCount / (double)fmt.SampleRate * 1000;
                }
            }
            catch (Exception)
            {
                // Only ever informational; an unreadable wem just shows no length.
            }

            _wemDurationsMs[sourceId] = durationMs;
            return durationMs;
        }
    }
}
