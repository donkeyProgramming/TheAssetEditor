using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.Generators.Hirc;
using Editors.Audio.Shared.Wwise.Generators.Hirc.V136;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise.Generators
{
    public interface IMusicSoundBankGeneratorService
    {
        /// <summary>Splices a music project's events and hierarchy into the vanilla .bnks they belong
        /// in and saves those to the editable pack. Throws, before writing anything, if the game no
        /// longer has something the music relies on.</summary>
        void GenerateSoundBanks(SoundBank soundBank, MusicProjectData music);
    }

    /// <summary>
    /// The music counterpart of <see cref="SoundBankGeneratorService"/>. Kept apart because music
    /// ships nothing of its own: every hirc goes into a copy of the vanilla .bnk it belongs in.
    /// </summary>
    public class MusicSoundBankGeneratorService : IMusicSoundBankGeneratorService
    {
        private readonly IFileSaveService _fileSaveService;
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly HircGeneratorServiceFactory _hircGeneratorServiceFactory;
        private readonly IMusicSwitchContainerMergeService _musicSwitchContainerMergeService;
        private readonly IAmsPulseTrackMergeService _amsPulseTrackMergeService;
        private readonly IAmsFragmentMergeService _amsFragmentMergeService;
        private readonly IAmsMusicLayout _amsMusicLayout;

        private readonly ILogger _logger = Logging.Create<MusicSoundBankGeneratorService>();

        public MusicSoundBankGeneratorService(
            IFileSaveService fileSaveService,
            IPackFileService packFileService,
            ApplicationSettingsService applicationSettingsService,
            IAudioRepository audioRepository,
            IMusicSwitchContainerMergeService musicSwitchContainerMergeService,
            IAmsPulseTrackMergeService amsPulseTrackMergeService,
            IAmsFragmentMergeService amsFragmentMergeService,
            IAmsMusicLayout amsMusicLayout)
        {
            _fileSaveService = fileSaveService;
            _packFileService = packFileService;
            _audioRepository = audioRepository;
            _musicSwitchContainerMergeService = musicSwitchContainerMergeService;
            _amsPulseTrackMergeService = amsPulseTrackMergeService;
            _amsFragmentMergeService = amsFragmentMergeService;
            _amsMusicLayout = amsMusicLayout;

            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            _hircGeneratorServiceFactory = HircGeneratorServiceFactory.CreateFactory(bankGeneratorVersion);
        }

        /// <summary>Each Event with its Actions as children. Music Events are a single SetState each,
        /// with nothing for them to play.</summary>
        private Dictionary<ActionEvent, HircItem> GenerateEventHircs(SoundBank soundBank)
        {
            var eventHircs = new Dictionary<ActionEvent, HircItem>();
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var eventHirc = _hircGeneratorServiceFactory.GenerateHirc(actionEvent);
                eventHirc.HircChildren = [.. actionEvent.Actions.Select(action => _hircGeneratorServiceFactory.GenerateHirc(action, soundBank))];
                eventHircs[actionEvent] = eventHirc;
            }
            return eventHircs;
        }

        /// <summary>
        /// The music hierarchy under the sequences a container's branches reach: tracks before their
        /// segments, segments before the sequence over them. Vanilla never splits a music container
        /// from its children across .bnks, so these go in the container's .bnk.
        /// </summary>
        private static List<HircItem> GenerateMusicHierarchyHircs(MusicProjectData music, IEnumerable<MusicRandomSequence> musicRandomSequences)
        {
            var hircItems = new List<HircItem>();

            foreach (var musicRandomSequence in musicRandomSequences)
            {
                foreach (var musicSegment in music.GetSegments(musicRandomSequence))
                {
                    hircItems.Add(new CAkMusicTrackGenerator_V136().GenerateHirc(musicSegment));
                    hircItems.Add(new CAkMusicSegmentGenerator_V136().GenerateHirc(musicSegment, musicRandomSequence.DirectParentId));
                }

                hircItems.Add(new CAkMusicRanSeqCntrGenerator_V136().GenerateHirc(musicRandomSequence));
            }

            return hircItems;
        }

        /// <summary>The vanilla Music Switch containers the mod adds branches to.</summary>
        private static List<uint> GetMusicContainerIds(MusicProjectData music) =>
            [.. music.RandomSequences.Select(musicRandomSequence => musicRandomSequence.DirectParentId).Distinct()];

        /// <summary>
        /// The branches the mod adds to one container, and the sequences they reach.
        ///
        /// Each sequence is reached only by the paths its State's <see cref="MusicTree"/> gives its node
        /// in this container - see <see cref="MusicSwitchContainerMergeService.BuildTreeKeyPaths"/>.
        /// One the tree never leads to here, because a branch below it covers everything it would,
        /// is left out rather than shipped where nothing can reach it.
        /// </summary>
        private static (List<MusicBranch> Branches, List<MusicRandomSequence> Reached) GetMusicBranches(MusicProjectData music, CAkMusicSwitchCntr_V136 vanillaContainer)
        {
            var branches = new List<MusicBranch>();
            var reached = new List<MusicRandomSequence>();
            var pathsByTree = new Dictionary<MusicTree, Dictionary<int, List<List<uint>>>>();

            foreach (var musicRandomSequence in music.RandomSequences.Where(sequence => sequence.DirectParentId == vanillaContainer.Id))
            {
                var tree = music.Trees.FirstOrDefault(musicTree =>
                    musicTree.StateGroupName == musicRandomSequence.StateGroupName && musicTree.StateName == musicRandomSequence.StateName);
                if (tree == null)
                    continue;

                if (!pathsByTree.TryGetValue(tree, out var pathsByNodeId))
                    pathsByTree[tree] = pathsByNodeId = MusicSwitchContainerMergeService.BuildTreeKeyPaths(vanillaContainer, tree);

                if (!pathsByNodeId.TryGetValue(musicRandomSequence.TreeNodeId, out var keyPaths))
                    continue;

                branches.Add(new MusicBranch(musicRandomSequence.StateGroupName, musicRandomSequence.StateName, musicRandomSequence.Id, keyPaths));
                reached.Add(musicRandomSequence);
            }

            return (branches, reached);
        }

        private CAkMusicSwitchCntr_V136 GetVanillaMusicSwitchContainer(uint containerId) =>
            _audioRepository.GetHircs(AkBkHircType.Music_Switch)
                .FirstOrDefault(hircItem => hircItem.Id == containerId && hircItem.IsCA) as CAkMusicSwitchCntr_V136;

        /// <summary>
        /// Every vanilla .bnk the music touches, rebuilt from the game's own copy with the mod spliced
        /// in and shipped under the vanilla .bnk's name. A separate .bnk of the mod's own cannot
        /// override vanilla music hircs in any load order (tested in game), so this is the only way.
        ///
        /// Each edited vanilla hirc replaces its original in the .bnk it was read from: the Music
        /// Switch containers with the mod's branches merged in, the pulse switch tracks with the mod's
        /// sub-tracks, and the ambient fragments container with the mod's cultures. New hircs go into
        /// the .bnk of the vanilla hirc that names them. Each Action Event goes into the .bnk its
        /// vanilla siblings live in - see <see cref="GetVanillaSoundBankFilePath(ActionEvent, SoundBank, List{string})"/>.
        ///
        /// Everything this relies on in vanilla - container ids, switch tracks, the .bnks themselves -
        /// can move in a game update, so anything missing is collected and reported before any .bnk is
        /// written rather than leaving a half-built mod that silently plays nothing.
        /// </summary>
        public void GenerateSoundBanks(SoundBank soundBank, MusicProjectData music)
        {
            var problems = new List<string>();
            var plans = new Dictionary<string, (Dictionary<uint, HircItem> Replacements, List<HircItem> Additions)>(StringComparer.OrdinalIgnoreCase);

            (Dictionary<uint, HircItem> Replacements, List<HircItem> Additions) PlanFor(string vanillaBnkFilePath)
            {
                var key = vanillaBnkFilePath.Replace('/', '\\');
                if (!plans.TryGetValue(key, out var plan))
                    plans[key] = plan = ([], []);
                return plan;
            }

            var actionEventToHircLookup = GenerateEventHircs(soundBank);

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var eventHirc = actionEventToHircLookup[actionEvent];
                var additions = PlanFor(GetVanillaSoundBankFilePath(actionEvent, soundBank, problems)).Additions;

                // An Event and the Actions it fires travel together, since the Event claims them,
                // and the Actions go first so nothing claims a hirc that comes after it.
                if (eventHirc.HircChildren != null)
                    additions.AddRange(eventHirc.HircChildren.OrderBy(action => action.Id));
                additions.Add(eventHirc);
            }

            foreach (var containerId in GetMusicContainerIds(music))
            {
                var vanillaContainer = GetVanillaMusicSwitchContainer(containerId);
                if (vanillaContainer == null)
                {
                    problems.Add($"Music Switch container {containerId} is not in the game's banks any more, so its branches cannot be merged");
                    continue;
                }

                var (branches, reached) = GetMusicBranches(music, vanillaContainer);
                if (branches.Count == 0)
                    continue;

                var plan = PlanFor(vanillaContainer.BnkFilePath);
                plan.Additions.AddRange(GenerateMusicHierarchyHircs(music, reached));
                plan.Replacements[containerId] = _musicSwitchContainerMergeService.MergeBranches(vanillaContainer, branches);
            }

            foreach (var moddedTrack in GenerateModdedAmsPulseTracks(music, problems))
                PlanFor(moddedTrack.BnkFilePath).Replacements[moddedTrack.Id] = moddedTrack;

            foreach (var moddedFragmentContainer in GenerateModdedAmsFragmentContainers(music, problems))
            {
                var plan = PlanFor(moddedFragmentContainer.BnkFilePath);
                plan.Additions.AddRange(GenerateAmsFragmentHircs(soundBank, music.AmsFragments
                    .Where(fragment => fragment.FactionSwitchContainerId == moddedFragmentContainer.Id)));
                plan.Replacements[moddedFragmentContainer.Id] = moddedFragmentContainer;
            }

            var vanillaBytesByPath = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var (vanillaBnkFilePath, (_, additions)) in plans)
            {
                var vanillaBytes = ReadVanillaSoundBank(vanillaBnkFilePath);
                if (vanillaBytes == null)
                {
                    problems.Add($"{vanillaBnkFilePath} is not in the game's packs");
                    continue;
                }

                // A clash means the mod generated an id vanilla already uses for something else,
                // and splicing it in would hand Wwise two definitions in one .bnk.
                var vanillaIds = VanillaSoundBankSplicer.ReadHircIds(vanillaBytes);
                var clashes = additions.Where(addition => vanillaIds.Contains(addition.Id)).Select(addition => addition.Id).ToList();
                if (clashes.Count != 0)
                    problems.Add($"{vanillaBnkFilePath} already holds hirc id(s) {string.Join(", ", clashes)} that this project also generates");

                vanillaBytesByPath[vanillaBnkFilePath] = vanillaBytes;
            }

            if (problems.Count != 0)
                throw new InvalidOperationException(
                    "The music banks were not built. If the game has been updated since this project was set up, " +
                    "the vanilla music may have changed:\n  " + string.Join("\n  ", problems.Distinct()));

            foreach (var (vanillaBnkFilePath, (replacements, additions)) in plans)
            {
                var bytes = VanillaSoundBankSplicer.Splice(vanillaBytesByPath[vanillaBnkFilePath], replacements, additions);
                BnkFile.CreateFromBytes(bytes, vanillaBnkFilePath, false); // throws if the splice is unreadable

                _logger.Here().Information(
                    $"Generating SoundBank {vanillaBnkFilePath}: vanilla with {replacements.Count} hirc(s) replaced and {additions.Count} added");
                _fileSaveService.Save(vanillaBnkFilePath, bytes, false);
            }
        }

        /// <summary>The vanilla .bnk a bank is named after.</summary>
        private static string GetVanillaSoundBankFilePath(SoundBank soundBank) =>
            GetSoundBankFilePath($"{Wh3SoundBankInformation.GetName(soundBank.GameSoundBank)}__core.bnk", soundBank.Language);

        /// <summary>
        /// The vanilla .bnk holding the vanilla Events that share this one's prefix. Vanilla splits
        /// music Events by the context that posts them (music_b_faction_* in battle_music__core.bnk,
        /// music_c_* in campaign_music__core.bnk), and an Event in a .bnk that context never loads is
        /// never answered. Read from the game rather than listed here so it follows a patch moving them.
        /// </summary>
        private string GetVanillaSoundBankFilePath(ActionEvent actionEvent, SoundBank soundBank, List<string> problems)
        {
            var prefix = Wh3MusicEventInformation.KnownEventPrefixes
                .FirstOrDefault(knownPrefix => actionEvent.Name.StartsWith(knownPrefix, StringComparison.OrdinalIgnoreCase));
            if (prefix == null)
                return GetVanillaSoundBankFilePath(soundBank);

            var siblingBnkFilePath = _audioRepository.GetHircs(AkBkHircType.Event)
                .Where(hircItem => hircItem.IsCA)
                .Where(hircItem => _audioRepository.GetNameFromId(hircItem.Id, out var found) is var name && found
                    && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .GroupBy(hircItem => hircItem.BnkFilePath, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault();

            if (siblingBnkFilePath == null)
                problems.Add($"No vanilla Event starts with {prefix} any more, so there is no .bnk known to be loaded where {actionEvent.Name} is posted");

            return siblingBnkFilePath ?? GetVanillaSoundBankFilePath(soundBank);
        }

        /// <summary>
        /// The game's own copy of a .bnk, never a mod's - the edited pack may already hold a
        /// replacement from an earlier compile, and splicing into that would apply the mod twice.
        /// The last game pack holding it wins, the same as for any other file.
        /// </summary>
        private byte[] ReadVanillaSoundBank(string bnkFilePath)
        {
            var gameContainers = _packFileService.GetAllPackfileContainers()
                .Where(container => container.IsCaPackFile)
                .Reverse();

            foreach (var container in gameContainers)
            {
                var file = _packFileService.FindFile(bnkFilePath, container);
                if (file != null)
                    return file.DataSource.ReadData();
            }

            return null;
        }

        /// <summary>
        /// The vanilla pulse switch tracks re-emitted with this mod's sub-tracks added.
        ///
        /// Clips are spread across the tracks in order and cycled when a mod supplies fewer than the
        /// game has, so one wav means the same pulse under every segment - which is thin, but is the
        /// only thing that can be done without inventing material.
        /// </summary>
        private List<CAkMusicTrack_V136> GenerateModdedAmsPulseTracks(MusicProjectData music, List<string> problems)
        {
            var moddedTracks = new List<CAkMusicTrack_V136>();

            foreach (var pulsesByStateGroup in music.AmsPulses.GroupBy(amsPulse => amsPulse.StateGroupName))
            {
                var slots = _amsMusicLayout.GetPulseSlots(pulsesByStateGroup.Key);
                if (slots.Count == 0)
                {
                    problems.Add($"No vanilla switch track reads State Group {pulsesByStateGroup.Key} any more, so its audio cannot be reached");
                    continue;
                }

                var branchesByTrackId = slots.ToDictionary(slot => slot.Track.Id, _ => new List<AmsPulseBranch>());

                foreach (var amsPulse in pulsesByStateGroup)
                {
                    var tree = music.Trees.FirstOrDefault(musicTree =>
                        musicTree.StateGroupName == amsPulse.StateGroupName && musicTree.StateName == amsPulse.StateName);
                    if (tree == null)
                        continue;

                    // Each track is one key and one variation; the tree says which node plays there, and
                    // a node playing in several tracks hands them its wavs in turn, so one wav is a valid
                    // answer and a key's three wavs become its three variations.
                    var slotsByNode = slots
                        .Select(slot => (Slot: slot, Node: tree.Evaluate(level =>
                            level == Wh3MusicTreeLevelInformation.AmsKeyLevel ? slot.Key :
                            level == Wh3MusicTreeLevelInformation.PulseVariationLevel ? slot.Variation.ToString() :
                            null)))
                        .Where(entry => entry.Node != null)
                        .GroupBy(entry => entry.Node);

                    foreach (var nodeSlots in slotsByNode)
                    {
                        var clips = nodeSlots.Key.AudioFileIds
                            .Select(sourceId => amsPulse.Clips.FirstOrDefault(clip => clip.SourceId == sourceId))
                            .Where(clip => clip != null)
                            .ToList();
                        if (clips.Count == 0)
                            continue;

                        var index = 0;
                        foreach (var (slot, _) in nodeSlots)
                            branchesByTrackId[slot.Track.Id].Add(new AmsPulseBranch(amsPulse.StateName, clips[index++ % clips.Count]));
                    }
                }

                foreach (var slot in slots)
                {
                    var branches = branchesByTrackId[slot.Track.Id];
                    if (branches.Count == 0)
                        continue;

                    var moddedTrack = _amsPulseTrackMergeService.AddSubTracks(slot.Track, branches, GetSegmentDurationMs(slot.Track));

                    // Carried over so the track is spliced into the .bnk it was read from; the merged
                    // track is a new object and would otherwise have no provenance.
                    moddedTrack.BnkFilePath = slot.Track.BnkFilePath;
                    moddedTracks.Add(moddedTrack);
                }
            }

            return moddedTracks;
        }

        /// <summary>
        /// The ambient fragment hierarchy under a vanilla faction container: the Switch container on
        /// the musical key Group, and the Sounds and any container under it. All new ids - it is only
        /// the faction container above them that clashes with vanilla.
        /// </summary>
        private List<HircItem> GenerateAmsFragmentHircs(SoundBank soundBank, IEnumerable<AmsFragment> amsFragments)
        {
            var hircItems = new List<HircItem>();
            var lookup = new AudioRepositorySwitchLookup(_audioRepository);

            foreach (var amsFragment in amsFragments)
            {
                var vanillaContainer = GetVanillaAmsFragmentContainer(amsFragment.FactionSwitchContainerId);
                if (vanillaContainer == null)
                    continue;

                foreach (var soundId in amsFragment.SoundIds)
                {
                    var sound = soundBank.GetSound(soundId);
                    if (sound != null)
                        hircItems.Add(_hircGeneratorServiceFactory.GenerateHirc(sound, soundBank));
                }

                foreach (var container in amsFragment.ContainerIds.Select(soundBank.GetRandomSequenceContainer).Where(container => container != null))
                    hircItems.Add(_hircGeneratorServiceFactory.GenerateHirc(container, soundBank));

                foreach (var callResponse in amsFragment.CallResponseSwitches)
                {
                    hircItems.Add(_amsFragmentMergeService.CreateCallResponseSwitchContainer(
                        vanillaContainer, lookup, callResponse.Id, amsFragment.KeySwitchContainerId, callResponse.CallTargetHircId, callResponse.ResponseTargetHircId));
                }

                // The game's key values come in threes, C_01 to C_03; each is pointed at its key's switch.
                var switchesByKey = amsFragment.Keys.ToDictionary(key => key.Key, key => key.CallResponseSwitchId, StringComparer.OrdinalIgnoreCase);
                hircItems.Add(_amsFragmentMergeService.CreateKeySwitchContainer(
                    vanillaContainer, lookup, amsFragment.KeySwitchContainerId,
                    keySwitchId => AmsMusicLayout.GetKey(_audioRepository.GetNameFromId(keySwitchId)) is string key && switchesByKey.TryGetValue(key, out var target) ? target : 0));
            }

            return hircItems;
        }

        /// <summary>
        /// The vanilla ambient fragments containers re-emitted with this mod's cultures added. These
        /// keep their vanilla ids, so like the Music Switch containers they replace vanilla's in the
        /// .bnk they were read from.
        /// </summary>
        private List<CAkSwitchCntr_V136> GenerateModdedAmsFragmentContainers(MusicProjectData music, List<string> problems)
        {
            var moddedContainers = new List<CAkSwitchCntr_V136>();

            foreach (var fragmentsByContainer in music.AmsFragments.GroupBy(fragment => fragment.FactionSwitchContainerId))
            {
                var vanillaContainer = GetVanillaAmsFragmentContainer(fragmentsByContainer.Key);
                if (vanillaContainer == null)
                {
                    problems.Add($"Switch container {fragmentsByContainer.Key} is not in the game's banks any more, so its cultures cannot be merged");
                    continue;
                }

                var branches = fragmentsByContainer
                    .Select(fragment => new AmsFragmentBranch(fragment.StateName, fragment.KeySwitchContainerId))
                    .ToList();

                var moddedContainer = _amsFragmentMergeService.AddCultures(vanillaContainer, branches);
                moddedContainer.BnkFilePath = vanillaContainer.BnkFilePath;
                moddedContainers.Add(moddedContainer);
            }

            return moddedContainers;
        }

        private CAkSwitchCntr_V136 GetVanillaAmsFragmentContainer(uint containerId) =>
            _audioRepository.GetHircs(containerId)
                .OfType<CAkSwitchCntr_V136>()
                .FirstOrDefault(container => container.IsCA);

        /// <summary>The one lookup the fragment merge needs, so it does not take the whole
        /// repository interface to read a sibling's musical keys.</summary>
        private sealed class AudioRepositorySwitchLookup(IAudioRepository audioRepository) : IAudioRepositoryLookup
        {
            public CAkSwitchCntr_V136 FindSwitchContainer(uint id) =>
                audioRepository.GetHircs(id).OfType<CAkSwitchCntr_V136>().FirstOrDefault();
        }

        /// <summary>
        /// How long the segment above a switch track runs. Every vanilla pulse clip is trimmed to
        /// this rather than played whole, because a clip running past the segment overlaps the next.
        /// </summary>
        private double GetSegmentDurationMs(CAkMusicTrack_V136 vanillaTrack)
        {
            var segment = _audioRepository.GetHircs(vanillaTrack.NodeBaseParams.DirectParentId)
                .OfType<CAkMusicSegment_V136>()
                .FirstOrDefault();

            if (segment != null)
                return segment.Duration;

            _logger.Here().Warning(
                $"Music Track {vanillaTrack.Id} has no parent segment, so its clips cannot be trimmed to one");
            return double.MaxValue;
        }

        private static string GetSoundBankFilePath(string fileName, string language)
        {
            if (language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                return $"audio\\wwise\\{fileName}";

            return $"audio\\wwise\\{language}\\{fileName}";
        }
    }
}
