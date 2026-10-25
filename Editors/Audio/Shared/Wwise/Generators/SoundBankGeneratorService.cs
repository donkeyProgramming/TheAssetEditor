using System.Data;
using System.IO;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.Generators.Bkhd;
using Editors.Audio.Shared.Wwise.Generators.Hirc;
using Editors.Audio.Shared.Wwise.Generators.Hirc.V136;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators
{
    public interface ISoundBankGeneratorService
    {
        void GenerateSoundBankWithoutMergedHircs(SoundBank soundBank);
        void GenerateDialogueEventsForTestingSoundBank(SoundBank soundBank);
        void GenerateMusicReplacementSoundBanks(SoundBank soundBank);
        void GenerateMergingSoundBank(SoundBank soundBank);
        void GenerateMergedDialogueEventSoundBanks(List<string> moddedSoundBanks, string soundBankSuffix);
        void GenerateMergedMusicSoundBanks(List<string> moddedSoundBanks, string soundBankSuffix);
    }

    public class SoundBankGeneratorService : ISoundBankGeneratorService
    {
        private readonly IFileSaveService _fileSaveService;
        private readonly IPackFileService _packFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IAudioRepository _audioRepository;
        private readonly HircGeneratorServiceFactory _hircGeneratorServiceFactory;
        private readonly IAudioEditorIntegrityService _audioEditorIntegrityService;
        private readonly IMusicSwitchContainerMergeService _musicSwitchContainerMergeService;
        private readonly IAmsPulseTrackMergeService _amsPulseTrackMergeService;
        private readonly IAmsFragmentMergeService _amsFragmentMergeService;

        private readonly ILogger _logger = Logging.Create<SoundBankGeneratorService>();

        public SoundBankGeneratorService(
            IFileSaveService fileSaveService,
            IPackFileService packFileService,
            ApplicationSettingsService applicationSettingsService,
            IAudioRepository audioRepository,
            IAudioEditorIntegrityService audioEditorIntegrityService,
            IMusicSwitchContainerMergeService musicSwitchContainerMergeService,
            IAmsPulseTrackMergeService amsPulseTrackMergeService,
            IAmsFragmentMergeService amsFragmentMergeService)
        {
            _fileSaveService = fileSaveService;
            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;
            _audioRepository = audioRepository;
            _audioEditorIntegrityService = audioEditorIntegrityService;
            _musicSwitchContainerMergeService = musicSwitchContainerMergeService;
            _amsPulseTrackMergeService = amsPulseTrackMergeService;
            _amsFragmentMergeService = amsFragmentMergeService;

            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            _hircGeneratorServiceFactory = HircGeneratorServiceFactory.CreateFactory(bankGeneratorVersion);
        }

        /// <summary>
        /// The .bnk the modder keeps. It leaves out everything that shares an id with vanilla -
        /// Dialogue Events and Music Switch containers - because those cannot simply be shipped
        /// alongside vanilla, they have to be merged into it. They live in the testing and merging
        /// .bnks instead.
        /// </summary>
        public void GenerateSoundBankWithoutMergedHircs(SoundBank soundBank)
        {
            var actionEventToHircLookup = new Dictionary<ActionEvent, HircItem>();
            var hircItems = new List<HircItem>();

            if (soundBank.ActionEvents.Count != 0)
            {
                var actionEventHircs = GenerateActionEventHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionEventHircs);

                var actionHircs = GenerateActionHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionHircs);

                var sourceHircsFromPlay = GenerateSourceHircsFromPlayActions(soundBank);
                hircItems.AddRange(sourceHircsFromPlay);
            }

            if (soundBank.DialogueEvents.Count != 0)
            {
                // We don't generate the Dialogue Events in this .bnk, we keep them in the merging SoundBank instead
                var sourceHircsFromDialogue = GenerateSourceHircsFromDialogueEvents(soundBank);
                hircItems.AddRange(sourceHircsFromDialogue);
            }

            SortHircs(hircItems);

            // Appended after sorting rather than run through it. SortHircs orders Play targets and
            // Events, and music is neither - it is reached through the decision tree, so it has its
            // own ordering: tracks before their segments, segments before the sequence over them.
            hircItems.AddRange(GenerateMusicHierarchyHircs(soundBank));
            hircItems.AddRange(GenerateAmsFragmentHircs(soundBank));

            WriteSoundBank(soundBank.Id, soundBank.LanguageId, soundBank.FileName, soundBank.FilePath, hircItems);
        }

        /// <summary>
        /// The music hierarchy this bank contributes. These are all new ids so they can ship as they
        /// are - it is only the Music Switch container above them that clashes with vanilla.
        /// </summary>
        private List<HircItem> GenerateMusicHierarchyHircs(SoundBank soundBank)
            => GenerateMusicHierarchyHircs(soundBank, soundBank.MusicRandomSequences);

        /// <summary>
        /// The same hierarchy for a chosen few of the sequences, so a testing .bnk can carry exactly
        /// the ones the container it overrides names as children. Vanilla never splits a music
        /// container from its children across two .bnks - every one of the 32 children of the two
        /// containers this touches sits in the same .bnk as its parent - so a testing .bnk holding a
        /// container alone is not the shape the game is built to load.
        /// </summary>
        private List<HircItem> GenerateMusicHierarchyHircs(SoundBank soundBank, IEnumerable<MusicRandomSequence> musicRandomSequences)
        {
            var hircItems = new List<HircItem>();

            foreach (var musicRandomSequence in musicRandomSequences)
            {
                foreach (var musicSegment in soundBank.GetMusicSegments(musicRandomSequence))
                {
                    hircItems.Add(new CAkMusicTrackGenerator_V136().GenerateHirc(musicSegment));
                    hircItems.Add(_hircGeneratorServiceFactory.GenerateHirc(musicSegment, soundBank));
                }

                hircItems.Add(_hircGeneratorServiceFactory.GenerateHirc(musicRandomSequence));
            }

            return hircItems;
        }

        /// <summary>
        /// The branches the mod adds, grouped by the vanilla Music Switch container they belong in.
        /// One container per State Group.
        /// </summary>
        private static Dictionary<uint, List<MusicBranch>> GetMusicBranchesByContainerId(SoundBank soundBank)
        {
            var branchesByContainerId = new Dictionary<uint, List<MusicBranch>>();

            foreach (var musicRandomSequence in soundBank.MusicRandomSequences)
            {
                var containerId = musicRandomSequence.DirectParentId;
                if (!branchesByContainerId.TryGetValue(containerId, out var branches))
                    branchesByContainerId[containerId] = branches = [];

                branches.Add(new MusicBranch(musicRandomSequence.StateGroupName, musicRandomSequence.StateName, musicRandomSequence.Id));
            }

            return branchesByContainerId;
        }

        private CAkMusicSwitchCntr_V136 GetVanillaMusicSwitchContainer(uint containerId)
        {
            var vanillaContainer = _audioRepository.GetHircs(AkBkHircType.Music_Switch)
                .FirstOrDefault(hircItem => hircItem.Id == containerId && hircItem.IsCA) as CAkMusicSwitchCntr_V136;

            if (vanillaContainer == null)
                _logger.Here().Error($"Music Switch container {containerId} was not found in the vanilla data, so its branches cannot be merged");

            return vanillaContainer;
        }

        /// <summary>
        /// Every vanilla .bnk the music touches, rebuilt from the game's own copy with the mod spliced
        /// in and shipped under the vanilla .bnk's name.
        ///
        /// A .bnk of the mod's own cannot do this. Measured in game (October 2026) with Cathay's
        /// branches removed from the two culture containers: a separate .bnk holding the edited
        /// containers changed nothing whether it loaded before or after global_music__core.bnk, or
        /// carried the containers' parents with it, while the same edit spliced into
        /// global_music__core.bnk itself silenced Cathay's prebattle music.
        ///
        /// Each edited vanilla hirc replaces its original in the .bnk it was read from: the Music
        /// Switch containers with the mod's branches merged in, the pulse switch tracks with the mod's
        /// sub-tracks, and the ambient fragments container with the mod's cultures. New hircs go into
        /// the same .bnk as the vanilla hirc that names them, since vanilla never splits a music
        /// container from its children across two .bnks. Each Action Event goes into the .bnk its
        /// vanilla siblings live in - see <see cref="GetVanillaSoundBankFilePath(ActionEvent, SoundBank)"/>.
        /// This is the layout of the August 2026 "eventbanks" pack, which played the mod's music in
        /// prebattle.
        /// </summary>
        public void GenerateMusicReplacementSoundBanks(SoundBank soundBank)
        {
            var plans = new Dictionary<string, (Dictionary<uint, HircItem> Replacements, List<HircItem> Additions)>(StringComparer.OrdinalIgnoreCase);

            (Dictionary<uint, HircItem> Replacements, List<HircItem> Additions) PlanFor(string vanillaBnkFilePath)
            {
                var key = vanillaBnkFilePath.Replace('/', '\\');
                if (!plans.TryGetValue(key, out var plan))
                    plans[key] = plan = ([], []);
                return plan;
            }

            var actionEventToHircLookup = new Dictionary<ActionEvent, HircItem>();
            GenerateActionEventHircs(soundBank, actionEventToHircLookup);
            GenerateActionHircs(soundBank, actionEventToHircLookup);

            var playTargets = GenerateSourceHircsFromPlayActions(soundBank);
            if (playTargets.Count != 0)
                PlanFor(GetVanillaSoundBankFilePath(soundBank)).Additions.AddRange(playTargets);

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var eventHirc = actionEventToHircLookup[actionEvent];
                var additions = PlanFor(GetVanillaSoundBankFilePath(actionEvent, soundBank)).Additions;

                // An Event and the Actions it fires travel together, since the Event claims them,
                // and the Actions go first so nothing claims a hirc that comes after it.
                if (eventHirc.HircChildren != null)
                    additions.AddRange(eventHirc.HircChildren.OrderBy(action => action.Id));
                additions.Add(eventHirc);
            }

            foreach (var (containerId, branches) in GetMusicBranchesByContainerId(soundBank))
            {
                var vanillaContainer = GetVanillaMusicSwitchContainer(containerId);
                if (vanillaContainer == null)
                    continue;

                var plan = PlanFor(vanillaContainer.BnkFilePath);
                plan.Additions.AddRange(GenerateMusicHierarchyHircs(soundBank, soundBank.MusicRandomSequences
                    .Where(musicRandomSequence => musicRandomSequence.DirectParentId == containerId)));
                plan.Replacements[containerId] = _musicSwitchContainerMergeService.MergeBranches(vanillaContainer, branches);
            }

            foreach (var moddedTrack in GenerateModdedAmsPulseTracks(soundBank))
                PlanFor(moddedTrack.BnkFilePath).Replacements[moddedTrack.Id] = moddedTrack;

            foreach (var moddedFragmentContainer in GenerateModdedAmsFragmentContainers(soundBank))
            {
                var plan = PlanFor(moddedFragmentContainer.BnkFilePath);
                plan.Additions.AddRange(GenerateAmsFragmentHircs(soundBank, soundBank.AmsFragments
                    .Where(fragment => fragment.FactionSwitchContainerId == moddedFragmentContainer.Id)));
                plan.Replacements[moddedFragmentContainer.Id] = moddedFragmentContainer;
            }

            foreach (var (vanillaBnkFilePath, (replacements, additions)) in plans)
            {
                var vanillaBytes = ReadVanillaSoundBank(vanillaBnkFilePath);
                if (vanillaBytes == null)
                {
                    _logger.Here().Error($"{vanillaBnkFilePath} was not found in the game's packs, so the music meant for it cannot be compiled");
                    continue;
                }

                // A clash means the mod generated an id vanilla already uses for something else,
                // and splicing it in would hand Wwise two definitions in one .bnk.
                var vanillaIds = VanillaSoundBankSplicer.ReadHircIds(vanillaBytes);
                var clashes = additions.Where(addition => vanillaIds.Contains(addition.Id)).Select(addition => addition.Id).ToList();
                if (clashes.Count != 0)
                    throw new InvalidOperationException(
                        $"{vanillaBnkFilePath} already holds hirc id(s) {string.Join(", ", clashes)} that this project also generates");

                var bytes = VanillaSoundBankSplicer.Splice(vanillaBytes, replacements, additions);
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
        /// The vanilla .bnk holding the vanilla Events of the same family as this one - the ones
        /// sharing its prefix. Vanilla splits music Events by the context that posts them:
        /// music_b_faction_* in battle_music__core.bnk, music_c_* in campaign_music__core.bnk. An
        /// Event in a .bnk that context never loads is never answered, which is why the August 2026
        /// packs played the mod's music in prebattle but not in battle until this was done. Read
        /// from the game rather than listed here so it follows a patch moving them.
        /// </summary>
        private string GetVanillaSoundBankFilePath(ActionEvent actionEvent, SoundBank soundBank)
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

            if (siblingBnkFilePath != null)
                return siblingBnkFilePath;

            _logger.Here().Warning($"No vanilla Event starts with {prefix}, so {actionEvent.Name} goes in the .bnk its SoundBank is named after");
            return GetVanillaSoundBankFilePath(soundBank);
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
        /// Unlike the Music Switch containers there is no mod-only form of these. A sub-track is
        /// identified by its position in the association array, so a track holding only the mod's
        /// sub-tracks would number them from zero and mean something entirely different. Both the
        /// testing and the merging .bnk therefore get the full merged track, and the merger works out
        /// what each mod added by diffing against vanilla.
        ///
        /// Clips are spread across the tracks in order and cycled when a mod supplies fewer than the
        /// game has, so one wav means the same pulse under every segment - which is thin, but is the
        /// only thing that can be done without inventing material.
        /// </summary>
        private List<CAkMusicTrack_V136> GenerateModdedAmsPulseTracks(SoundBank soundBank)
        {
            var moddedTracks = new List<CAkMusicTrack_V136>();
            if (soundBank.AmsPulses.Count == 0)
                return moddedTracks;

            foreach (var pulsesByStateGroup in soundBank.AmsPulses.GroupBy(amsPulse => amsPulse.StateGroupName))
            {
                var vanillaTracks = _audioRepository.GetVanillaAmsPulseTracks(pulsesByStateGroup.Key);
                if (vanillaTracks.Count == 0)
                {
                    _logger.Here().Error(
                        $"No vanilla switch track reads State Group {pulsesByStateGroup.Key}, so its audio cannot be reached");
                    continue;
                }

                for (var trackIndex = 0; trackIndex < vanillaTracks.Count; trackIndex++)
                {
                    var vanillaTrack = vanillaTracks[trackIndex];

                    var branches = pulsesByStateGroup
                        .Where(amsPulse => amsPulse.Clips.Count != 0)
                        .Select(amsPulse => new AmsPulseBranch(
                            amsPulse.StateName,
                            amsPulse.Clips[trackIndex % amsPulse.Clips.Count]))
                        .ToList();

                    if (branches.Count == 0)
                        continue;

                    var moddedTrack = _amsPulseTrackMergeService.AddSubTracks(
                        vanillaTrack, branches, GetSegmentDurationMs(vanillaTrack));

                    // Carried over so the testing .bnk can be named after the .bnk it overrides; the
                    // merged track is a new object and would otherwise have no provenance.
                    moddedTrack.BnkFilePath = vanillaTrack.BnkFilePath;
                    moddedTracks.Add(moddedTrack);
                }
            }

            return moddedTracks;
        }

        /// <summary>
        /// The ambient fragment hierarchy this bank contributes: the Switch container on the musical
        /// key Group, and the Sounds and any container under it. All new ids, so these ship in the
        /// mod's own .bnk - it is only the vanilla faction container above them that clashes.
        /// </summary>
        private List<HircItem> GenerateAmsFragmentHircs(SoundBank soundBank)
            => GenerateAmsFragmentHircs(soundBank, soundBank.AmsFragments);

        private List<HircItem> GenerateAmsFragmentHircs(SoundBank soundBank, IEnumerable<AmsFragment> amsFragments)
        {
            var hircItems = new List<HircItem>();

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

                var container = soundBank.GetRandomSequenceContainer(amsFragment.TargetHircId);
                if (container != null)
                    hircItems.Add(_hircGeneratorServiceFactory.GenerateHirc(container, soundBank));

                hircItems.Add(_amsFragmentMergeService.CreateKeySwitchContainer(
                    vanillaContainer, new AudioRepositorySwitchLookup(_audioRepository),
                    amsFragment.KeySwitchContainerId, amsFragment.TargetHircId));
            }

            return hircItems;
        }

        /// <summary>
        /// The vanilla ambient fragments containers re-emitted with this mod's cultures added. These
        /// keep their vanilla ids, so like the Music Switch containers they belong in the testing and
        /// merging .bnks rather than the mod's own.
        /// </summary>
        private List<CAkSwitchCntr_V136> GenerateModdedAmsFragmentContainers(SoundBank soundBank)
        {
            var moddedContainers = new List<CAkSwitchCntr_V136>();

            foreach (var fragmentsByContainer in soundBank.AmsFragments.GroupBy(fragment => fragment.FactionSwitchContainerId))
            {
                var vanillaContainer = GetVanillaAmsFragmentContainer(fragmentsByContainer.Key);
                if (vanillaContainer == null)
                    continue;

                var branches = fragmentsByContainer
                    .Select(fragment => new AmsFragmentBranch(fragment.StateName, fragment.KeySwitchContainerId))
                    .ToList();

                var moddedContainer = _amsFragmentMergeService.AddCultures(vanillaContainer, branches);
                moddedContainer.BnkFilePath = vanillaContainer.BnkFilePath;
                moddedContainers.Add(moddedContainer);
            }

            return moddedContainers;
        }

        private CAkSwitchCntr_V136 GetVanillaAmsFragmentContainer(uint containerId)
        {
            var vanillaContainer = _audioRepository.GetHircs(containerId)
                .OfType<CAkSwitchCntr_V136>()
                .FirstOrDefault(container => container.IsCA);

            if (vanillaContainer == null)
                _logger.Here().Error(
                    $"Switch container {containerId} was not found in the vanilla data, so its cultures cannot be merged");

            return vanillaContainer;
        }

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

        /// <summary>
        /// The Music Switch containers carrying only the mod's own branches, for the merging .bnk.
        /// Vanilla is deliberately left out: the merger folds it in once, after it has combined the
        /// branches from every mod it was given.
        /// </summary>
        private List<HircItem> GenerateModdedMusicSwitchContainers(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            foreach (var (containerId, branches) in GetMusicBranchesByContainerId(soundBank))
            {
                var vanillaContainer = GetVanillaMusicSwitchContainer(containerId);
                if (vanillaContainer == null)
                    continue;

                hircItems.Add(_musicSwitchContainerMergeService.CreateModdedContainer(vanillaContainer, branches));
            }

            return hircItems;
        }

        /// <summary>
        /// The name a modded .bnk has to be built on to override the vanilla one, taken from the
        /// vanilla .bnk the hirc was read out of. Vanilla names its .bnks '{base}__core', so the base
        /// is what comes before the double underscore.
        /// </summary>
        private static string GetSoundBankNameBase(string bnkFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(bnkFilePath);
            var separatorIndex = fileName.IndexOf("__");
            return separatorIndex == -1 ? fileName : fileName[..separatorIndex];
        }

        private static string GetSoundBankFilePath(string fileName, string language)
        {
            if (language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                return $"audio\\wwise\\{fileName}";

            return $"audio\\wwise\\{language}\\{fileName}";
        }

        public void GenerateDialogueEventsForTestingSoundBank(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            var dialogueEventHircs = GenerateDialogueEventHircs(soundBank);
            hircItems.AddRange(dialogueEventHircs);

            var vanillaDialogueEvents = _audioRepository.GetHircs(AkBkHircType.Dialogue_Event)
                .Where(hircItem => hircItem.IsCA == true);

            foreach (var hircItem in hircItems)
            {
                var vanillaDialogueEvent = vanillaDialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Id == hircItem.Id) as CAkDialogueEvent_V136;
                var vanillaDecisionTree = vanillaDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                var compilerDialogueEvent = hircItem as CAkDialogueEvent_V136;
                var compilerDecisionTree = compilerDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                // Merge the vanilla decision tree into the compiled decision tree so the compiled decision tree takes priority
                var decisionTree = AkDecisionTree_V136.MergeDecisionTrees(compilerDecisionTree.DecisionTree, vanillaDecisionTree.DecisionTree);
                var nodes = AkDecisionTree_V136.FlattenDecisionTree(decisionTree);
                var mergedDecisionTree = new AkDecisionTree_V136
                {
                    DecisionTree = decisionTree,
                    Nodes = nodes
                };
                compilerDialogueEvent.AkDecisionTree = mergedDecisionTree;
                compilerDialogueEvent.TreeDataSize = mergedDecisionTree.GetSize();
                compilerDialogueEvent.UpdateSectionSize();
            }

            SortHircs(hircItems);

            WriteSoundBank(soundBank.TestingId, soundBank.LanguageId, soundBank.TestingFileName, soundBank.TestingFilePath, hircItems);
        }

        public void GenerateMergingSoundBank(SoundBank soundBank)
        {
            // We generate all hircs in the merging SoundBank as when merging we check for ID clashes so we can report them to modders
            var actionEventToHircLookup = new Dictionary<ActionEvent, HircItem>();
            var hircItems = new List<HircItem>();

            if (soundBank.ActionEvents.Count != 0)
            {
                var actionEventHircs = GenerateActionEventHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionEventHircs);

                var actionHircs = GenerateActionHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionHircs);

                var sourceHircsFromPlay = GenerateSourceHircsFromPlayActions(soundBank);
                hircItems.AddRange(sourceHircsFromPlay);
            }

            if (soundBank.DialogueEvents.Count != 0)
            {
                var dialogueEventHircs = GenerateDialogueEventHircs(soundBank);
                hircItems.AddRange(dialogueEventHircs);

                var sourceHircsFromDialogueEvents = GenerateSourceHircsFromDialogueEvents(soundBank);
                hircItems.AddRange(sourceHircsFromDialogueEvents);
            }

            SortHircs(hircItems);

            // Same ordering exception as the modder's .bnk. The containers go in carrying the mod's
            // branches alone so the merger can combine several mods before folding vanilla in.
            hircItems.AddRange(GenerateMusicHierarchyHircs(soundBank));
            hircItems.AddRange(GenerateModdedMusicSwitchContainers(soundBank));
            hircItems.AddRange(GenerateModdedAmsPulseTracks(soundBank));
            hircItems.AddRange(GenerateAmsFragmentHircs(soundBank));
            hircItems.AddRange(GenerateModdedAmsFragmentContainers(soundBank));

            WriteSoundBank(soundBank.MergingId, soundBank.LanguageId, soundBank.MergingFileName, soundBank.MergingFilePath, hircItems);
        }

        public void GenerateMergedDialogueEventSoundBanks(List<string> moddedSoundBanks, string soundBankSuffix)
        {
            _audioEditorIntegrityService.CheckMergingSoundBanksIdIntegrity();

            var vanillaDialogueEventsByBnkByLanguage = _audioRepository.GetVanillaDialogueEventsByBnkByLanguage();
            var moddedDialogueEventsByLanguage = _audioRepository.GetModdedDialogueEventsByLanguage(moddedSoundBanks);

            var soundBanksToGenerateByLanguage = moddedDialogueEventsByLanguage
                .ToDictionary(
                    languageEntry => languageEntry.Key,
                    languageEntry => languageEntry.Value
                        .Select(hircItem => _audioRepository.GetNameFromId(hircItem.Id))
                        .Select(dialogueEventName => Wh3DialogueEventInformation.GetSoundBank(dialogueEventName))
                        .Distinct()
                        .ToList());

            foreach (var bnkByLanguage in vanillaDialogueEventsByBnkByLanguage)
            {
                var language = bnkByLanguage.Key;
                if (!moddedDialogueEventsByLanguage.ContainsKey(language))
                    continue;

                var soundBanksToGenerate = soundBanksToGenerateByLanguage[language];
                foreach (var hircsByBnk in bnkByLanguage.Value)
                {
                    var firstDialogueEventName = hircsByBnk.Value.Select(hircItem => _audioRepository.GetNameFromId(hircItem.Id)).FirstOrDefault();
                    var currentSoundBank = Wh3DialogueEventInformation.GetSoundBank(firstDialogueEventName);
                    if (!soundBanksToGenerate.Contains(currentSoundBank))
                        continue;

                    _logger.Here().Information($"Merging SoundBanks for language {language}");

                    GenerateMergedDialogueEventSoundBank(hircsByBnk.Value, moddedDialogueEventsByLanguage, language, currentSoundBank, soundBankSuffix);
                }
            }
        }

        /// <summary>
        /// The music counterpart of <see cref="GenerateMergedDialogueEventSoundBanks"/>: takes the
        /// merging .bnks from several mods and writes one .bnk per vanilla .bnk holding the Music
        /// Switch containers with everyone's branches in them.
        /// </summary>
        public void GenerateMergedMusicSoundBanks(List<string> moddedSoundBanks, string soundBankSuffix)
        {
            var moddedContainers = _audioRepository.GetModdedMusicSwitchContainers(moddedSoundBanks);
            var moddedTracks = _audioRepository.GetModdedMusicTracks(moddedSoundBanks)
                .OfType<CAkMusicTrack_V136>()
                .Where(track => track.SwitchParams != null)
                .ToList();

            // Only the faction containers matter here: a mod's own key switches are new ids that ship
            // in its own .bnk and need no merging.
            var moddedFragmentContainers = _audioRepository.GetHircs(AkBkHircType.SwitchContainer)
                .OfType<CAkSwitchCntr_V136>()
                .Where(container => !container.IsCA && moddedSoundBanks.Contains(container.BnkFilePath))
                .Where(container => _audioRepository.GetHircs(container.Id).Any(hirc => hirc.IsCA))
                .ToList();

            if (moddedContainers.Count == 0 && moddedTracks.Count == 0 && moddedFragmentContainers.Count == 0)
                return;

            var mergedTracksByBnk = MergeAmsPulseTracks(moddedTracks);
            foreach (var (bnkFilePath, mergedFragments) in MergeAmsFragmentContainers(moddedFragmentContainers))
            {
                if (!mergedTracksByBnk.TryGetValue(bnkFilePath, out var hircsForBnk))
                    mergedTracksByBnk[bnkFilePath] = hircsForBnk = [];

                hircsForBnk.AddRange(mergedFragments);
            }

            foreach (var (vanillaBnkFilePath, vanillaContainers) in _audioRepository.GetVanillaMusicSwitchContainersByBnk())
            {
                var mergedContainers = new List<HircItem>();

                if (mergedTracksByBnk.TryGetValue(vanillaBnkFilePath, out var tracksForThisBnk))
                {
                    mergedContainers.AddRange(tracksForThisBnk);
                    mergedTracksByBnk.Remove(vanillaBnkFilePath);
                }

                foreach (var vanillaHirc in vanillaContainers)
                {
                    var vanillaContainer = vanillaHirc as CAkMusicSwitchCntr_V136;
                    var matchingModdedContainers = moddedContainers
                        .Where(moddedHirc => moddedHirc.Id == vanillaContainer.Id)
                        .Cast<CAkMusicSwitchCntr_V136>()
                        .ToList();

                    if (matchingModdedContainers.Count == 0)
                        continue;

                    _logger.Here().Information($"Merging Music Switch container {_audioRepository.GetNameFromId(vanillaContainer.Id)}");

                    // Vanilla first, then each mod in turn, so vanilla wins a State two of them
                    // claim - the same tie break the Dialogue Event merge uses. A branch adds a
                    // subculture vanilla does not have, so in normal use nothing collides and the
                    // order does not come up.
                    var mergedContainer = vanillaContainer;
                    foreach (var moddedContainer in matchingModdedContainers)
                    {
                        _logger.Here().Information($"Merging decision tree from {Path.GetFileName(moddedContainer.BnkFilePath)}");
                        mergedContainer = _musicSwitchContainerMergeService.MergeContainers(mergedContainer, moddedContainer);
                    }

                    mergedContainers.Add(mergedContainer);
                }

                if (mergedContainers.Count == 0)
                    continue;

                var soundBankNameBase = GetSoundBankNameBase(vanillaBnkFilePath);
                var soundBankNameWithoutExtension = $"{soundBankNameBase}_0_{soundBankSuffix}";
                var fileName = $"{soundBankNameWithoutExtension}.bnk";
                var language = _audioRepository.GetNameFromId(vanillaContainers[0].LanguageId);
                var filePath = GetSoundBankFilePath(fileName, language);

                _logger.Here().Information($"Merging Music Switch containers for SoundBank {filePath}");
                WriteSoundBank(WwiseHash.Compute(soundBankNameWithoutExtension), vanillaContainers[0].LanguageId, fileName, filePath, mergedContainers);
            }
        }

        /// <summary>
        /// Combines every mod's pulse switch tracks, keyed by the vanilla .bnk they override.
        ///
        /// Each mod's merging .bnk holds the whole track - vanilla plus its own sub-tracks - because
        /// a sub-track has no identity apart from its position. So the merge starts from vanilla and
        /// re-appends what each mod added in turn, which gives every mod's culture an index of its
        /// own rather than letting the second mod overwrite the first.
        /// </summary>
        private Dictionary<string, List<HircItem>> MergeAmsPulseTracks(List<CAkMusicTrack_V136> moddedTracks)
        {
            var mergedTracksByBnk = new Dictionary<string, List<HircItem>>();

            foreach (var tracksById in moddedTracks.GroupBy(track => track.Id))
            {
                var vanillaTrack = _audioRepository.GetHircs(tracksById.Key)
                    .OfType<CAkMusicTrack_V136>()
                    .FirstOrDefault(track => track.IsCA && track.SwitchParams != null);

                if (vanillaTrack == null)
                {
                    _logger.Here().Error(
                        $"Music Track {tracksById.Key} was not found in the vanilla data, so its sub-tracks cannot be merged");
                    continue;
                }

                _logger.Here().Information($"Merging sub-tracks into Music Track {vanillaTrack.Id}");

                var mergedTrack = vanillaTrack;
                foreach (var moddedTrack in tracksById)
                {
                    _logger.Here().Information($"Merging sub-tracks from {Path.GetFileName(moddedTrack.BnkFilePath)}");
                    mergedTrack = _amsPulseTrackMergeService.MergeTracks(mergedTrack, vanillaTrack, moddedTrack);
                }

                if (!mergedTracksByBnk.TryGetValue(vanillaTrack.BnkFilePath, out var tracksForBnk))
                    mergedTracksByBnk[vanillaTrack.BnkFilePath] = tracksForBnk = [];

                tracksForBnk.Add(mergedTrack);
            }

            return mergedTracksByBnk;
        }

        /// <summary>
        /// Combines every mod's ambient fragment containers, keyed by the vanilla .bnk they override.
        ///
        /// Easier than the switch tracks: a Switch container names its branches by switch id, so this
        /// merges by key the way a decision tree does and two mods adding different cultures cannot
        /// collide however many came before.
        /// </summary>
        private Dictionary<string, List<HircItem>> MergeAmsFragmentContainers(List<CAkSwitchCntr_V136> moddedContainers)
        {
            var mergedByBnk = new Dictionary<string, List<HircItem>>();

            foreach (var containersById in moddedContainers.GroupBy(container => container.Id))
            {
                var vanillaContainer = _audioRepository.GetHircs(containersById.Key)
                    .OfType<CAkSwitchCntr_V136>()
                    .FirstOrDefault(container => container.IsCA);

                if (vanillaContainer == null)
                    continue;

                _logger.Here().Information($"Merging cultures into Switch container {vanillaContainer.Id}");

                var mergedContainer = vanillaContainer;
                foreach (var moddedContainer in containersById)
                {
                    _logger.Here().Information($"Merging cultures from {Path.GetFileName(moddedContainer.BnkFilePath)}");
                    mergedContainer = _amsFragmentMergeService.MergeContainers(mergedContainer, moddedContainer);
                }

                if (!mergedByBnk.TryGetValue(vanillaContainer.BnkFilePath, out var containersForBnk))
                    mergedByBnk[vanillaContainer.BnkFilePath] = containersForBnk = [];

                containersForBnk.Add(mergedContainer);
            }

            return mergedByBnk;
        }

        private List<HircItem> GenerateActionEventHircs(SoundBank soundBank, Dictionary<ActionEvent, HircItem> actionEventToHircLookup)
        {
            var hircItems = new List<HircItem>();
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var actionEventHirc = _hircGeneratorServiceFactory.GenerateHirc(actionEvent);
                hircItems.Add(actionEventHirc);
                actionEventToHircLookup[actionEvent] = actionEventHirc;
            }
            return hircItems;
        }

        private List<HircItem> GenerateActionHircs(SoundBank soundBank, Dictionary<ActionEvent, HircItem> actionEventToHircLookup)
        {
            var hircItems = new List<HircItem>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (actionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported");

                var actionEventHirc = actionEventToHircLookup[actionEvent];

                foreach (var action in actionEvent.Actions)
                {
                    var actionHirc = _hircGeneratorServiceFactory.GenerateHirc(action, soundBank);
                    hircItems.Add(actionHirc);

                    if (actionEventHirc.HircChildren == null)
                        actionEventHirc.HircChildren = [];
                    actionEventHirc.HircChildren.Add(actionHirc);
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateSourceHircsFromPlayActions(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                foreach (var action in actionEvent.Actions)
                {
                    if (action.ActionType == AkActionType.Play)
                    {
                        if (action.TargetHircTypeIsSound())
                        {
                            var sound = soundBank.GetSound(action.TargetHircId);
                            var soundTargetHircs = GenerateSoundTargetHircs(soundBank, sound);
                            hircItems.AddRange(soundTargetHircs);
                        }
                        else if (action.TargetHircTypeIsRandomSequenceContainer())
                        {
                            var randomSequenceContainer = soundBank.GetRandomSequenceContainer(action.TargetHircId);
                            var randomSequenceContainerTargetHircs = GenerateRandomSequenceContainerTargetHircs(soundBank, randomSequenceContainer);
                            hircItems.AddRange(randomSequenceContainerTargetHircs);
                        }
                    }
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateDialogueEventHircs(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();
            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                var dialogueEventHirc = _hircGeneratorServiceFactory.GenerateHirc(dialogueEvent);
                hircItems.Add(dialogueEventHirc);
            }
            return hircItems;
        }

        private List<HircItem> GenerateSourceHircsFromDialogueEvents(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                foreach (var statePath in dialogueEvent.StatePaths)
                {

                    if (statePath.TargetHircTypeIsSound())
                    {
                        var sound = soundBank.GetSound(statePath.TargetHircId);
                        var soundTargetHircs = GenerateSoundTargetHircs(soundBank, sound);
                        hircItems.AddRange(soundTargetHircs);
                    }
                    else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                    {
                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                        var randomSequenceContainerTargetHircs = GenerateRandomSequenceContainerTargetHircs(soundBank, randomSequenceContainer);
                        hircItems.AddRange(randomSequenceContainerTargetHircs);
                    }
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateSoundTargetHircs(SoundBank soundBank, Sound sound)
        {
            var hircItems = new List<HircItem>();

            var soundHirc = _hircGeneratorServiceFactory.GenerateHirc(sound);
            soundHirc.IsTarget = true;
            hircItems.Add(soundHirc);
            return hircItems;
        }

        private List<HircItem> GenerateRandomSequenceContainerTargetHircs(SoundBank soundBank, RandomSequenceContainer randomSequenceContainer)
        {
            var hircItems = new List<HircItem>();

            var randomSequenceContainerHirc = _hircGeneratorServiceFactory.GenerateHirc(randomSequenceContainer, soundBank);
            hircItems.Add(randomSequenceContainerHirc);

            randomSequenceContainerHirc.IsTarget = true;
            if (randomSequenceContainerHirc.HircChildren == null)
                randomSequenceContainerHirc.HircChildren = [];

            var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
            foreach (var sound in sounds)
            {
                var soundHirc = _hircGeneratorServiceFactory.GenerateHirc(sound);
                hircItems.Add(soundHirc);
                randomSequenceContainerHirc.HircChildren.Add(soundHirc);
            }

            return hircItems;
        }

        private void GenerateMergedDialogueEventSoundBank(
            List<HircItem> vanillaHircs,
            Dictionary<string, List<HircItem>> moddedDialogueEventsByLanguage,
            string language,
            Wh3SoundBank currentSoundBank, 
            string soundBankSuffix)
        {
            var dialogueEvents = new List<HircItem>();

            var soundBankNameBase = Wh3SoundBankInformation.GetName(currentSoundBank);
            var soundBankNameWithoutExtension = $"{soundBankNameBase}_0_{soundBankSuffix}";
            var soundBankFileName = $"{soundBankNameWithoutExtension}.bnk";

            var soundBankFilePath = $"audio\\wwise\\{language}\\{soundBankFileName}";
            if (language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                soundBankFilePath = $"audio\\wwise\\{soundBankFileName}";

            var soundBankId = WwiseHash.Compute(soundBankNameWithoutExtension);
            var languageId = WwiseHash.Compute(language);

            _logger.Here().Information($"Merging Dialogue Events for SoundBank {soundBankFilePath}");

            foreach (var vanillaHirc in vanillaHircs)
            {
                // Clone it as we shouldn't modify the original
                var vanillaDialogueEvent = vanillaHirc as CAkDialogueEvent_V136;
                var mergedDialogueEvent = vanillaDialogueEvent.Clone();

                var matchingModdedDialogueEvents = moddedDialogueEventsByLanguage[language]
                    .Where(moddedHircItem => moddedHircItem.Id == vanillaDialogueEvent.Id)
                    .ToList();

                if (matchingModdedDialogueEvents.Count == 0)
                    continue;

                _logger.Here().Information($"Merging Dialogue Event {_audioRepository.GetNameFromId(vanillaHirc.Id)}");

                foreach (var moddedDialogueEventHirc in matchingModdedDialogueEvents)
                {
                    _logger.Here().Information($"Merging decision tree from {Path.GetFileName(moddedDialogueEventHirc.BnkFilePath)}");

                    var currentDecisionTree = mergedDialogueEvent.AkDecisionTree as AkDecisionTree_V136;
                    var moddedDialogueEvent = moddedDialogueEventHirc as CAkDialogueEvent_V136;
                    var moddedDecisionTree = moddedDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                    var mergedDecisionTree = new AkDecisionTree_V136();
                    mergedDecisionTree.DecisionTree = AkDecisionTree_V136.MergeDecisionTrees(currentDecisionTree.DecisionTree, moddedDecisionTree.DecisionTree);
                    mergedDecisionTree.Nodes = AkDecisionTree_V136.FlattenDecisionTree(mergedDecisionTree.DecisionTree);

                    mergedDialogueEvent.AkDecisionTree = mergedDecisionTree;
                    mergedDialogueEvent.TreeDataSize = mergedDecisionTree.GetSize();
                    mergedDialogueEvent.UpdateSectionSize();
                }

                dialogueEvents.Add(mergedDialogueEvent);
            }

            SortHircs(dialogueEvents);

            WriteSoundBank(soundBankId, languageId, soundBankFileName, soundBankFilePath, dialogueEvents);
        }

        private static void SortHircs(List<HircItem> hircItems)
        {
            var targetHircs = hircItems
                .Where(hircItem => hircItem.IsTarget)
                .ToList();

            var eventHircs = hircItems
                .Where(hircItem => hircItem.HircType == AkBkHircType.Event || hircItem.HircType == AkBkHircType.Dialogue_Event)
                .ToList();

            var sortedHircItems = new List<HircItem>();

            foreach (var targetHirc in targetHircs.OrderBy(sourceHirc => sourceHirc.Id))
            {
                if (targetHirc.HircType == AkBkHircType.RandomSequenceContainer)
                {
                    var soundHircs = targetHirc.HircChildren.OrderBy(soundHirc => soundHirc.Id);
                    sortedHircItems.AddRange(soundHircs);
                    sortedHircItems.Add(targetHirc);
                }
                else
                    sortedHircItems.Add(targetHirc);
            }

            foreach (var eventHirc in eventHircs.OrderBy(eventHirc => eventHirc.Id))
            {
                if (eventHirc.HircType == AkBkHircType.Event)
                {
                    var actionHircs = eventHirc.HircChildren.OrderBy(eventHirc => eventHirc.Id);
                    sortedHircItems.AddRange(actionHircs);
                    sortedHircItems.Add(eventHirc);
                }
                else
                    sortedHircItems.Add(eventHirc);
            }

            hircItems.Clear();
            hircItems.AddRange(sortedHircItems);
        }

        private void WriteSoundBank(uint id, uint languageId, string fileName, string filePath, List<HircItem> hircItems)
        {
            var gameInformation = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame);
            var bankGeneratorVersion = (uint)gameInformation.BankGeneratorVersion;
            var wwiseProjectId = (uint)gameInformation.WwiseProjectId;

            var bkhdChunk = BkhdChunkGenerator.GenerateBkhdChunk(bankGeneratorVersion, id, languageId, wwiseProjectId);
            var bkhdChunkBytes = BkhdChunk.WriteData(bkhdChunk);

            var hircChunk = HircChunkGenerator.GenerateHircChunk(hircItems);
            var hircChunkBytes = HircChunk.WriteData(hircChunk, bankGeneratorVersion);

            using var memStream = new MemoryStream();
            memStream.Write(bkhdChunkBytes);
            memStream.Write(hircChunkBytes);
            var bytes = memStream.ToArray();

            var bnkPackFile = new PackFile(fileName, new MemorySource(bytes));
            var reparsedSanityFile = BnkFile.CreateFromBytes(bnkPackFile.DataSource.ReadData(), "test\\fakefilename.bnk", true);

            _fileSaveService.Save(filePath, bnkPackFile.DataSource.ReadData(), false);
        }
    }
}
