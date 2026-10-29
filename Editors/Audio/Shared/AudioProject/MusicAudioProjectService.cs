using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject
{
    public record MusicCultureAddResult(
        IReadOnlyList<string> CreatedEvents,
        IReadOnlyList<(string EventName, string Reason)> SkippedEvents);

    /// <summary>
    /// Keeps the music project: the cultures it wires into the music scripts, one music Action
    /// Event per event those cultures post, and the audio each event's State selects.
    ///
    /// A vanilla music event is a single SetState action against one of the music State Groups -
    /// music_b_faction_empire sets Battle_Music_WH3_Culture to Empire - and holds no Sound, no
    /// container and no audio file. What makes a State audible is the music hierarchy branching on
    /// it, which lives in the game's own banks; the event's whole job is to select a branch. So the
    /// audio given to an event becomes the branch its State selects rather than something the event
    /// plays. See <see cref="Wh3MusicHierarchyInformation.CanCarryOwnAudio"/> for which State Groups
    /// can carry audio of their own.
    ///
    /// One project holds every culture (see <see cref="MusicCulture"/>). Everything here works on
    /// a project in memory; loading and saving it is the caller's.
    /// </summary>
    public interface IMusicAudioProjectService
    {
        /// <summary>An empty music project, ready for cultures.</summary>
        AudioProjectFile CreateNew();

        /// <summary>Loads the game's audio data and the vanilla ids new items must not reuse, which
        /// takes a while, so a UI can start it early off the UI thread. Everything else here waits
        /// for it if it is still running, and does it itself if it was never started.</summary>
        Task WarmUpAsync();

        /// <summary>Adds the culture and the events it needs. A culture already in the project with
        /// the same Audio State and subculture key - or <paramref name="replacing"/>, when editing one
        /// changed either - is replaced in place, so adding it again updates it; events it no longer
        /// needs are removed, and ones it keeps keep their audio.</summary>
        MusicCultureAddResult AddCulture(AudioProjectFile audioProject, MusicCulture musicCulture, MusicCulture replacing = null);

        /// <summary>Removes the culture, and with it every event and branch no other culture in the
        /// project still needs.</summary>
        void RemoveCulture(AudioProjectFile audioProject, MusicCulture musicCulture);

        /// <summary>The audio at the root of the event's <see cref="MusicTree"/>, in playlist order.</summary>
        IReadOnlyList<AudioFile> GetAudio(AudioProjectFile audioProject, string eventName);

        /// <summary>Replaces the audio at the root of the event's <see cref="MusicTree"/> with the wavs at
        /// these pack paths, in playlist order.</summary>
        void SetAudio(AudioProjectFile audioProject, string eventName, IReadOnlyList<string> wavPackFilePaths);

        /// <summary>The event's decision tree, saying which of its audio plays when; null for an event
        /// that cannot carry audio of its own. An event that has none yet gets an empty root.
        ///
        /// Every slot has one, though only the battle and campaign themes compile to a Wwise decision
        /// tree: the pulses and the fragments are laid out differently in the game, and their trees
        /// split by what those layouts tell apart - see <see cref="AmsMusicLayout"/>.</summary>
        MusicTree GetTree(AudioProjectFile audioProject, string eventName);

        IReadOnlyList<AudioFile> GetNodeAudio(AudioProjectFile audioProject, string eventName, int nodeId);

        /// <summary>Replaces one tree node's audio and regenerates the music it plays.</summary>
        void SetNodeAudio(AudioProjectFile audioProject, string eventName, int nodeId, IReadOnlyList<string> wavPackFilePaths);

        /// <summary>Adds a branch for <paramref name="value"/> of <paramref name="level"/> under a node,
        /// which then splits on that level; null <paramref name="value"/> adds its "any other value"
        /// branch. A new branch has no audio, so it plays its parent's until it is given some.
        /// Returns its id.</summary>
        int AddBranch(AudioProjectFile audioProject, string eventName, int parentNodeId, string level, string value);

        /// <summary>Removes a branch and everything below it. The root cannot be removed.</summary>
        void RemoveBranch(AudioProjectFile audioProject, string eventName, int nodeId);
    }

    public class MusicAudioProjectService(
        IActionEventFactory actionEventFactory,
        IMusicHierarchyFactory musicHierarchyFactory,
        IAudioRepository audioRepository,
        IAmsMusicLayout amsMusicLayout) : IMusicAudioProjectService
    {
        /// <summary>The one music project, named so its SoundBank is global_music_music.</summary>
        public const string ProjectName = "music";

        /// <summary>Its own extension rather than .aproj, so the Audio Editor - which is for
        /// Dialogue Events and plain Action Events - never offers to open it.</summary>
        public const string FileExtension = ".mproj";

        public const string ProjectFolder = @"audio\audio_projects";
        public const string ProjectFilePath = $@"{ProjectFolder}\{ProjectName}{FileExtension}";

        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;
        private readonly IMusicHierarchyFactory _musicHierarchyFactory = musicHierarchyFactory;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IAmsMusicLayout _amsMusicLayout = amsMusicLayout;

        private readonly ILogger _logger = Logging.Create<MusicAudioProjectService>();

        // The vanilla ids, fetched once. Reading them out of the audio cache takes over ten seconds
        // and the game's own packs do not change while a project is open, so asking again for every
        // event made adding one culture take a minute.
        private readonly object _vanillaIdsLock = new();
        private HashSet<uint> _vanillaHircIds;
        private HashSet<uint> _vanillaSourceIds;

        // Music always lives in the Sfx language folder regardless of what a project is otherwise
        // set to (global_music declares Sfx as its RequiredLanguage), so the project is Sfx outright
        // - that keeps the ID uniqueness scope, which is per language, the same one the SoundBank
        // will actually be compiled into.
        private static string Language => Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx);

        public AudioProjectFile CreateNew()
        {
            var audioProject = AudioProjectFile.CreateNew(Language, ProjectName);
            audioProject.Music = new MusicProjectData();
            return audioProject;
        }

        public Task WarmUpAsync() => Task.Run(EnsureVanillaIds);

        public MusicCultureAddResult AddCulture(AudioProjectFile audioProject, MusicCulture musicCulture, MusicCulture replacing = null)
        {
            ArgumentNullException.ThrowIfNull(audioProject);
            ArgumentNullException.ThrowIfNull(musicCulture);

            // Lets GenerateActionEventId see vanilla IDs and refuse a name that already exists -
            // adding a culture called "empire" would otherwise silently produce a second
            // music_b_faction_empire shadowing the real one.
            EnsureVanillaIds();

            // Kept in its place, since the scripts are built with the cultures in project order.
            var cultures = Music(audioProject).Cultures;
            var index = cultures.FindIndex(other =>
                IsSameCulture(other, musicCulture) || (replacing != null && IsSameCulture(other, replacing)));
            var replacedCount = cultures.RemoveAll(other =>
                IsSameCulture(other, musicCulture) || (replacing != null && IsSameCulture(other, replacing)));
            cultures.Insert(index < 0 ? cultures.Count : index, musicCulture);

            // Removed only after the new entry is in, so an event both versions need is kept along
            // with any audio it was given.
            if (replacedCount != 0)
                RemoveEventsNoCultureNeeds(audioProject);

            var soundBank = GetOrCreateMusicSoundBank(audioProject);
            var usedHircIds = GetUsedHircIds(audioProject);
            var created = new List<string>();
            var skipped = new List<(string, string)>();

            foreach (var eventName in musicCulture.GetOwnEventNames())
            {
                // Already there from an earlier run for this culture, or shared with another
                // subculture of the same culture.
                if (soundBank.GetActionEvent(eventName) != null)
                    continue;

                try
                {
                    AddMusicEvent(audioProject, soundBank, eventName, usedHircIds);
                    created.Add(eventName);
                }
                catch (Exception e)
                {
                    // Most likely GenerateActionEventId rejecting a name that already exists in
                    // vanilla. One unusable event should not cost the modder the other five, so it
                    // is reported back rather than aborting the whole culture.
                    _logger.Here().Error($"Could not add music event '{eventName}': {e.Message}");
                    skipped.Add((eventName, e.Message));
                }
            }

            return new MusicCultureAddResult(created, skipped);
        }

        public void RemoveCulture(AudioProjectFile audioProject, MusicCulture musicCulture)
        {
            ArgumentNullException.ThrowIfNull(audioProject);
            ArgumentNullException.ThrowIfNull(musicCulture);

            audioProject.Music?.Cultures.RemoveAll(other => IsSameCulture(other, musicCulture));
            RemoveEventsNoCultureNeeds(audioProject);
        }

        public IReadOnlyList<AudioFile> GetAudio(AudioProjectFile audioProject, string eventName)
        {
            var soundBank = GetMusicSoundBank(audioProject);
            var setStateAction = GetSetStateAction(soundBank, eventName);
            if (setStateAction == null)
                return [];

            var stateGroupName = setStateAction.StateGroupName;
            var stateName = setStateAction.StateName;

            return Wh3MusicHierarchyInformation.CanCarryOwnAudio(stateGroupName)
                ? GetNodeAudio(audioProject, eventName, GetTree(audioProject, eventName).Root.Id)
                : [];
        }

        public void SetAudio(AudioProjectFile audioProject, string eventName, IReadOnlyList<string> wavPackFilePaths)
        {
            ArgumentNullException.ThrowIfNull(wavPackFilePaths);

            var soundBank = GetMusicSoundBank(audioProject);
            var setStateAction = GetSetStateAction(soundBank, eventName)
                ?? throw new InvalidOperationException($"The music project has no event called '{eventName}'.");

            var stateGroupName = setStateAction.StateGroupName;

            if (!Wh3MusicHierarchyInformation.CanCarryOwnAudio(stateGroupName))
                throw new InvalidOperationException(
                    $"'{eventName}' sets {stateGroupName}, which nothing in the game branches on in a way a mod can extend, so audio given to it could never be reached.");

            SetNodeAudio(audioProject, eventName, GetTree(audioProject, eventName).Root.Id, wavPackFilePaths);
        }

        public MusicTree GetTree(AudioProjectFile audioProject, string eventName)
        {
            var soundBank = GetMusicSoundBank(audioProject);
            var setStateAction = GetSetStateAction(soundBank, eventName);
            if (setStateAction == null || !HasTree(setStateAction.StateGroupName))
                return null;

            var stateGroupName = setStateAction.StateGroupName;
            var stateName = setStateAction.StateName;

            var tree = Music(audioProject).Trees.FirstOrDefault(musicTree => IsFor(musicTree.StateGroupName, musicTree.StateName, stateGroupName, stateName));
            if (tree == null)
            {
                tree = new MusicTree { StateGroupName = stateGroupName, StateName = stateName };
                Music(audioProject).Trees.Add(tree);
            }

            return tree;
        }

        public IReadOnlyList<AudioFile> GetNodeAudio(AudioProjectFile audioProject, string eventName, int nodeId)
        {
            var node = RequireNode(audioProject, eventName, nodeId, out _, out _);
            return GetAudioFiles(audioProject, node.AudioFileIds);
        }

        public void SetNodeAudio(AudioProjectFile audioProject, string eventName, int nodeId, IReadOnlyList<string> wavPackFilePaths)
        {
            ArgumentNullException.ThrowIfNull(wavPackFilePaths);

            var node = RequireNode(audioProject, eventName, nodeId, out var soundBank, out var tree);
            EnsureVanillaIds();

            var audioFiles = GetOrCreateAudioFiles(audioProject, wavPackFilePaths);
            node.AudioFileIds = [.. audioFiles.Select(audioFile => audioFile.Id)];
            RegenerateTreeHierarchy(audioProject, soundBank, tree, audioFiles);
        }

        public int AddBranch(AudioProjectFile audioProject, string eventName, int parentNodeId, string level, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(level);

            var parent = RequireNode(audioProject, eventName, parentNodeId, out _, out var tree);

            if (parent.SplitBy != null && parent.SplitBy != level)
                throw new InvalidOperationException($"This part of the tree already splits by {Wh3MusicTreeLevelInformation.GetLevelName(parent.SplitBy)}.");

            // Splitting on a level an ancestor already split on can only ever see the one value that
            // ancestor's branch is for, so it would say nothing.
            for (var ancestor = tree.GetParent(parent); ancestor != null; ancestor = tree.GetParent(ancestor))
            {
                if (ancestor.SplitBy == level)
                    throw new InvalidOperationException($"A branch above this one already splits by {Wh3MusicTreeLevelInformation.GetLevelName(level)}.");
            }

            if (parent.Branches.Any(branch => branch.Value == value))
                throw new InvalidOperationException($"This part of the tree already has a branch for {Wh3MusicTreeLevelInformation.GetValueName(level, value)}.");

            var node = new MusicTreeNode { Id = tree.NextNodeId(), Value = value };
            parent.SplitBy = level;
            parent.Branches.Add(node);

            // Nothing to regenerate: with no audio of its own the branch plays its parent's, which is
            // what played there before it existed.
            return node.Id;
        }

        public void RemoveBranch(AudioProjectFile audioProject, string eventName, int nodeId)
        {
            var node = RequireNode(audioProject, eventName, nodeId, out var soundBank, out var tree);
            var parent = tree.GetParent(node)
                ?? throw new InvalidOperationException("The root of the tree cannot be removed; remove its audio instead.");

            parent.Branches.Remove(node);
            if (parent.Branches.Count == 0)
                parent.SplitBy = null;

            EnsureVanillaIds();
            RegenerateTreeHierarchy(audioProject, soundBank, tree, []);
        }

        private static bool HasTree(string stateGroupName) => Wh3MusicHierarchyInformation.CanCarryOwnAudio(stateGroupName);

        private MusicTreeNode RequireNode(AudioProjectFile audioProject, string eventName, int nodeId, out SoundBank soundBank, out MusicTree tree)
        {
            tree = GetTree(audioProject, eventName)
                ?? throw new InvalidOperationException($"'{eventName}' is not a music event with a decision tree.");
            soundBank = GetMusicSoundBank(audioProject);
            return tree.GetNode(nodeId)
                ?? throw new InvalidOperationException($"'{eventName}' has no tree node {nodeId}.");
        }

        /// <summary>
        /// What a State plays, regenerated from its tree. The three layers are built differently,
        /// because the game lays them out differently:
        ///
        ///   decision trees  a random sequence per node that has audio, in each container that node can
        ///                   play in. Which paths reach each is worked out at compile time - see
        ///                   <see cref="Wwise.Generators.MusicSwitchContainerMergeService.BuildTreeKeyPaths"/>.
        ///   pulses          only the clips; which vanilla track each goes in is decided at compile time
        ///                   from the tree, since the tracks are the game's.
        ///   fragments       the whole hierarchy below the faction container - see <see cref="AmsFragment"/>.
        /// </summary>
        private void RegenerateTreeHierarchy(AudioProjectFile audioProject, SoundBank soundBank, MusicTree tree, IEnumerable<AudioFile> newAudioFiles)
        {
            // Taken before the old hierarchy goes, since that drops audio files nothing plays any more.
            var audioFilesById = audioProject.AudioFiles
                .Concat(newAudioFiles)
                .DistinctBy(audioFile => audioFile.Id)
                .ToDictionary(audioFile => audioFile.Id);

            List<AudioFile> AudioFilesOf(MusicTreeNode node) => [.. node.AudioFileIds
                .Select(audioFileId => audioFilesById.GetValueOrDefault(audioFileId))
                .Where(audioFile => audioFile != null)];

            RemoveMusicBranch(audioProject, soundBank, tree.StateGroupName, tree.StateName);

            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(tree.StateGroupName))
            {
                RegenerateAmsPulse(audioProject, soundBank, tree, AudioFilesOf);
                return;
            }

            if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(tree.StateGroupName) is uint fragmentContainerId)
            {
                RegenerateAmsFragment(audioProject, soundBank, tree, AudioFilesOf, fragmentContainerId);
                return;
            }

            var usedHircIds = GetUsedHircIds(audioProject);
            foreach (var node in tree.GetNodes().Where(node => node.AudioFileIds.Count != 0))
            {
                var audioFiles = AudioFilesOf(node);
                if (audioFiles.Count == 0)
                    continue;

                // A music node has one parent, so a node playing in both battle containers needs a
                // copy of its hierarchy under each.
                foreach (var containerId in Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds(tree.StateGroupName))
                {
                    if (!CanPlayIn(tree, node, containerId))
                        continue;

                    var branch = _musicHierarchyFactory.CreateMusicBranch(usedHircIds, containerId, tree.StateGroupName, tree.StateName, audioFiles, soundBank.Language);
                    branch.MusicRandomSequence.TreeNodeId = node.Id;
                    Music(audioProject).RandomSequences.Add(branch.MusicRandomSequence);
                    Music(audioProject).Segments.AddRange(branch.MusicSegments);
                }

                foreach (var audioFile in audioFiles)
                    AddAudioFile(audioProject, audioFile);
            }
        }

        /// <summary>Whether a node is on the container's side of every split on the part of the game
        /// above it. Its paths are worked out at compile time; this only saves generating a hierarchy
        /// in a container it can never play in.</summary>
        private static bool CanPlayIn(MusicTree tree, MusicTreeNode node, uint containerId)
        {
            var containerValue = containerId.ToString();
            for (var child = node; tree.GetParent(child) is { } parent; child = parent)
            {
                if (parent.SplitBy != Wh3MusicTreeLevelInformation.ContainerLevel)
                    continue;

                if (child.Value != null && child.Value != containerValue)
                    return false;

                if (child.Value == null && parent.Branches.Any(sibling => sibling.Value == containerValue))
                    return false;
            }

            return true;
        }

        private List<AudioFile> GetOrCreateAudioFiles(AudioProjectFile audioProject, IReadOnlyList<string> wavPackFilePaths)
        {
            var usedSourceIds = new HashSet<uint>(_vanillaSourceIds);
            usedSourceIds.UnionWith(audioProject.GetAudioFileIds());

            var audioFiles = new List<AudioFile>();
            foreach (var wavPackFilePath in wavPackFilePaths)
            {
                var audioFile = audioProject.GetAudioFile(wavPackFilePath)
                    ?? audioFiles.FirstOrDefault(other => string.Equals(other.WavPackFilePath, wavPackFilePath, StringComparison.OrdinalIgnoreCase));
                if (audioFile == null)
                {
                    var ids = IdGenerator.GenerateIds(usedSourceIds);
                    audioFile = new AudioFile(ids.Guid, ids.Id, Path.GetFileName(wavPackFilePath), wavPackFilePath);
                }
                audioFiles.Add(audioFile);
            }

            return audioFiles;
        }

        private static bool IsSameCulture(MusicCulture left, MusicCulture right) =>
            string.Equals(left.AudioState, right.AudioState, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.SubcultureKey, right.SubcultureKey, StringComparison.OrdinalIgnoreCase);

        // Matched on the State Group as well as the State: "Empire" is a State in both the campaign
        // subculture Group and the battle culture one, and they are different branches in different
        // containers.
        private static bool IsFor(string itemStateGroupName, string itemStateName, string stateGroupName, string stateName) =>
            itemStateName == stateName && itemStateGroupName == stateGroupName;

        private static List<AudioFile> GetAudioFiles(AudioProjectFile audioProject, IEnumerable<uint> sourceIds)
        {
            return sourceIds
                .Distinct()
                .Select(audioProject.GetAudioFile)
                .Where(audioFile => audioFile != null)
                .ToList();
        }

        // A project opened from an older or hand-made file may not have one yet.
        private static MusicProjectData Music(AudioProjectFile audioProject) => audioProject.Music ??= new MusicProjectData();

        private static SoundBank GetMusicSoundBank(AudioProjectFile audioProject) =>
            audioProject.SoundBanks.FirstOrDefault(soundBank => soundBank.GameSoundBank == Wh3SoundBank.GlobalMusic);

        private static Models.Action GetSetStateAction(SoundBank soundBank, string eventName) =>
            soundBank?.GetActionEvent(eventName)?.Actions.FirstOrDefault(action => action.ActionType == AkActionType.SetState);

        // A saved project leaves out empty SoundBanks, so a reopened one may not have it.
        private static SoundBank GetOrCreateMusicSoundBank(AudioProjectFile audioProject)
        {
            var soundBank = GetMusicSoundBank(audioProject);
            if (soundBank != null)
                return soundBank;

            var soundBankName = $"{Wh3SoundBankInformation.GetName(Wh3SoundBank.GlobalMusic)}_{ProjectName}";
            soundBank = new SoundBank(soundBankName, Wh3SoundBank.GlobalMusic, Language);
            audioProject.SoundBanks.Add(soundBank);
            return soundBank;
        }

        private void AddMusicEvent(AudioProjectFile audioProject, SoundBank soundBank, string eventName, HashSet<uint> usedHircIds)
        {
            if (!Wh3MusicEventInformation.TryResolveStateTarget(eventName, out var stateGroupName, out var stateName))
                throw new InvalidOperationException(
                    $"'{eventName}' does not follow any of the vanilla music event patterns ({string.Join(", ", Wh3MusicEventInformation.KnownEventPrefixes)}), " +
                    "so there is no way to tell which State Group it should set.");

            var result = _actionEventFactory.CreateSetStateActionEvent(
                usedHircIds,
                Wh3ActionEventType.Music,
                eventName,
                stateGroupName,
                stateName);

            soundBank.ActionEvents.InsertAlphabetically(result.ActionEvent);
            foreach (var stateGroupToRegisterIn in Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName))
                AddStateToStateGroup(audioProject, stateGroupToRegisterIn, stateName);
        }

        /// <summary>
        /// Drops every event no culture in the project posts any more, along with the branch its
        /// State selected and the State itself, so removing or reshaping a culture leaves nothing
        /// behind for the compile to ship.
        /// </summary>
        private static void RemoveEventsNoCultureNeeds(AudioProjectFile audioProject)
        {
            var soundBank = GetMusicSoundBank(audioProject);
            if (soundBank == null)
                return;

            var neededEventNames = (audioProject.Music?.Cultures ?? [])
                .SelectMany(musicCulture => musicCulture.GetOwnEventNames())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unneededActionEvents = soundBank.ActionEvents
                .Where(actionEvent => !neededEventNames.Contains(actionEvent.Name))
                .ToList();

            foreach (var actionEvent in unneededActionEvents)
            {
                soundBank.ActionEvents.Remove(actionEvent);

                foreach (var setStateAction in actionEvent.Actions.Where(action => action.ActionType == AkActionType.SetState))
                {
                    // Another event can set the same State, so its branch and State only go when
                    // nothing is left pointing at them - otherwise removing one of two events would
                    // silence both.
                    if (IsStateStillSet(soundBank, setStateAction.StateGroupName, setStateAction.StateName))
                        continue;

                    RemoveMusicBranch(audioProject, soundBank, setStateAction.StateGroupName, setStateAction.StateName);
                    Music(audioProject).Trees.RemoveAll(tree => IsFor(tree.StateGroupName, tree.StateName, setStateAction.StateGroupName, setStateAction.StateName));
                    RemoveStateFromStateGroups(audioProject, soundBank, setStateAction.StateGroupName, setStateAction.StateName);
                }
            }
        }

        private static bool IsStateStillSet(SoundBank soundBank, string stateGroupName, string stateName)
        {
            return soundBank.ActionEvents
                .SelectMany(actionEvent => actionEvent.Actions)
                .Any(action => action.ActionType == AkActionType.SetState && IsFor(action.StateGroupName, action.StateName, stateGroupName, stateName));
        }

        /// <summary>
        /// A pulse's clips: every wav anywhere in its tree, once. Each vanilla pulse track is one key and
        /// one variation, and the compile asks the tree which node plays in each and hands the track one
        /// of that node's wavs - see <see cref="Wwise.Generators.MusicSoundBankGeneratorService"/>.
        /// </summary>
        private static void RegenerateAmsPulse(AudioProjectFile audioProject, SoundBank soundBank, MusicTree tree, Func<MusicTreeNode, List<AudioFile>> audioFilesOf)
        {
            var audioFiles = tree.GetNodes().SelectMany(audioFilesOf).DistinctBy(audioFile => audioFile.Id).ToList();
            if (audioFiles.Count == 0)
                return;

            var amsPulse = new AmsPulse { StateGroupName = tree.StateGroupName, StateName = tree.StateName };
            foreach (var audioFile in audioFiles)
            {
                amsPulse.Clips.Add(new AmsPulseClip { SourceId = audioFile.Id, Language = soundBank.Language });
                AddAudioFile(audioProject, audioFile);
            }

            Music(audioProject).AmsPulses.Add(amsPulse);
        }

        /// <summary>
        /// The fragment hierarchy, in vanilla's shape: per key, a Switch on Call or Response over what
        /// the tree plays for each. Keys that would play the same pair share a switch, the way vanilla's
        /// C_01, C_02 and C_03 share one branch. A key the tree gives nothing plays nothing.
        ///
        /// A Sound has one parent, so a node playing under two switches gets its Sounds twice.
        /// </summary>
        private void RegenerateAmsFragment(
            AudioProjectFile audioProject, SoundBank soundBank, MusicTree tree, Func<MusicTreeNode, List<AudioFile>> audioFilesOf, uint fragmentContainerId)
        {
            var usedHircIds = GetUsedHircIds(audioProject);
            var amsFragment = new AmsFragment
            {
                StateGroupName = tree.StateGroupName,
                StateName = tree.StateName,
                FactionSwitchContainerId = fragmentContainerId,
                KeySwitchContainerId = IdGenerator.GenerateIds(usedHircIds).Id
            };

            uint CreateTarget(MusicTreeNode node, uint parentId)
            {
                var audioFiles = node == null ? [] : audioFilesOf(node);
                if (audioFiles.Count == 0)
                    return 0;

                uint containerId = 0;
                if (audioFiles.Count > 1)
                {
                    var containerIds = IdGenerator.GenerateIds(usedHircIds);
                    containerId = containerIds.Id;
                    amsFragment.ContainerIds.Add(containerId);
                    soundBank.RandomSequenceContainers.TryAdd(new RandomSequenceContainer(
                        containerIds.Guid, containerIds.Id, overrideBusId: 0, directParentId: parentId, hircSettings: new HircSettings(), children: []));
                }

                foreach (var audioFile in audioFiles)
                {
                    var soundIds = IdGenerator.GenerateIds(usedHircIds);
                    var sound = Sound.CreateContainerSound(
                        soundIds.Guid, soundIds.Id, directParentId: containerId == 0 ? parentId : containerId,
                        playlistOrder: amsFragment.SoundIds.Count, sourceId: audioFile.Id, language: soundBank.Language);
                    soundBank.Sounds.TryAdd(sound);
                    amsFragment.SoundIds.Add(sound.Id);
                    soundBank.GetRandomSequenceContainer(containerId)?.Children.Add(sound.Id);

                    AddAudioFile(audioProject, audioFile);
                    var projectAudioFile = audioProject.GetAudioFile(audioFile.Id);
                    if (!projectAudioFile.Sounds.Contains(sound.Id))
                        projectAudioFile.Sounds.Add(sound.Id);
                }

                return containerId != 0 ? containerId : amsFragment.SoundIds[^1];
            }

            var switchesByNodes = new Dictionary<(int? Call, int? Response), AmsFragmentCallResponse>();
            foreach (var key in _amsMusicLayout.GetFragmentKeys(fragmentContainerId))
            {
                MusicTreeNode NodeFor(string callOrResponse) => tree.Evaluate(level =>
                    level == Wh3MusicTreeLevelInformation.AmsKeyLevel ? key :
                    level == AmsMusicLayout.CallResponseStateGroupName ? callOrResponse :
                    null);

                var call = NodeFor(AmsMusicLayout.Call);
                var response = NodeFor(AmsMusicLayout.Response);
                if (call == null && response == null)
                    continue;

                if (!switchesByNodes.TryGetValue((call?.Id, response?.Id), out var callResponse))
                {
                    callResponse = new AmsFragmentCallResponse { Id = IdGenerator.GenerateIds(usedHircIds).Id };
                    callResponse.CallTargetHircId = CreateTarget(call, callResponse.Id);

                    // The same node under both shares one target, which still has the one parent.
                    callResponse.ResponseTargetHircId = response == call ? callResponse.CallTargetHircId : CreateTarget(response, callResponse.Id);

                    switchesByNodes[(call?.Id, response?.Id)] = callResponse;
                    amsFragment.CallResponseSwitches.Add(callResponse);
                }

                amsFragment.Keys.Add(new AmsFragmentKey { Key = key, CallResponseSwitchId = callResponse.Id });
            }

            if (amsFragment.Keys.Count != 0)
                Music(audioProject).AmsFragments.Add(amsFragment);
        }

        /// <summary>Whatever the State selects, in every container that branches on it, and any
        /// audio file nothing else in the project plays.</summary>
        private static void RemoveMusicBranch(AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName)
        {
            Music(audioProject).AmsPulses.RemoveAll(amsPulse => IsFor(amsPulse.StateGroupName, amsPulse.StateName, stateGroupName, stateName));

            foreach (var amsFragment in Music(audioProject).AmsFragments.Where(fragment => IsFor(fragment.StateGroupName, fragment.StateName, stateGroupName, stateName)).ToList())
            {
                foreach (var sound in amsFragment.SoundIds.Select(soundBank.GetSound).Where(sound => sound != null))
                {
                    soundBank.Sounds.Remove(sound);
                    audioProject.GetAudioFile(sound.SourceId)?.Sounds.Remove(sound.Id);
                }

                foreach (var container in amsFragment.ContainerIds.Select(soundBank.GetRandomSequenceContainer).Where(container => container != null))
                    soundBank.RandomSequenceContainers.Remove(container);

                Music(audioProject).AmsFragments.Remove(amsFragment);
            }

            foreach (var musicRandomSequence in Music(audioProject).RandomSequences.Where(randomSequence => IsFor(randomSequence.StateGroupName, randomSequence.StateName, stateGroupName, stateName)).ToList())
            {
                foreach (var musicSegment in Music(audioProject).GetSegments(musicRandomSequence))
                    Music(audioProject).Segments.Remove(musicSegment);

                Music(audioProject).RandomSequences.Remove(musicRandomSequence);
            }

            RemoveUnplayedAudioFiles(audioProject);
        }

        private static void RemoveUnplayedAudioFiles(AudioProjectFile audioProject)
        {
            var playedSourceIds = Music(audioProject).GetDirectSourceIds().ToHashSet();

            audioProject.AudioFiles.RemoveAll(audioFile => audioFile.Sounds.Count == 0 && !playedSourceIds.Contains(audioFile.Id));
        }

        private static void AddAudioFile(AudioProjectFile audioProject, AudioFile audioFile)
        {
            if (audioProject.GetAudioFile(audioFile.Id) == null)
                audioProject.AudioFiles.TryAdd(audioFile);
        }

        /// <summary>
        /// Registers the State the event selects, so it reaches the generated .dat. Vanilla States
        /// are already known to the game, so re-adding one would only produce a duplicate.
        /// </summary>
        private void AddStateToStateGroup(AudioProjectFile audioProject, string stateGroupName, string stateName)
        {
            var stateGroup = audioProject.GetStateGroup(stateGroupName);

            // Music Groups are added on first use, and a saved project leaves out empty Groups.
            if (stateGroup == null && Wh3StateGroupInformation.IsMusicProjectStateGroup(stateGroupName))
            {
                stateGroup = StateGroup.CreateForAudioProjectFile(stateGroupName, []);
                audioProject.StateGroups.TryAdd(stateGroup);
            }

            if (stateGroup == null)
                return;

            if (stateGroup.States.Any(state => StringComparer.OrdinalIgnoreCase.Equals(state.Name, stateName)))
                return;

            if (_audioRepository.StatesByStateGroup.TryGetValue(stateGroupName, out var vanillaStates) &&
                vanillaStates.Contains(stateName, StringComparer.OrdinalIgnoreCase))
                return;

            stateGroup.States.InsertAlphabetically(new State(stateName));
        }

        /// <summary>The reverse of <see cref="AddStateToStateGroup"/>, for a State no remaining event
        /// sets. A State is registered in more than one Group for some events, so each Group keeps it
        /// while any remaining event would still register it there.</summary>
        private static void RemoveStateFromStateGroups(AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName)
        {
            foreach (var stateGroupToRegisterIn in Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName))
            {
                var stillRegistered = soundBank.ActionEvents
                    .SelectMany(actionEvent => actionEvent.Actions)
                    .Where(action => action.ActionType == AkActionType.SetState
                        && StringComparer.OrdinalIgnoreCase.Equals(action.StateName, stateName))
                    .Any(action => Wh3StateGroupInformation.GetStateGroupsToRegisterIn(action.StateGroupName).Contains(stateGroupToRegisterIn));

                if (!stillRegistered)
                    audioProject.GetStateGroup(stateGroupToRegisterIn)?.States.RemoveAll(state => StringComparer.OrdinalIgnoreCase.Equals(state.Name, stateName));
            }
        }

        /// <summary>The same set <see cref="IdGenerator.GetUsedHircIds"/> builds, from the vanilla
        /// ids fetched once rather than every time, plus the music's own.</summary>
        private HashSet<uint> GetUsedHircIds(AudioProjectFile audioProject)
        {
            var usedHircIds = new HashSet<uint>(_vanillaHircIds);
            usedHircIds.UnionWith(audioProject.GetGeneratableItemIds());
            usedHircIds.UnionWith(Music(audioProject).GetUsedHircIds());
            return usedHircIds;
        }

        // Failure here is not fatal: without game files the repository simply reports nothing used,
        // which costs the collision check but still yields a valid project.
        private void EnsureVanillaIds()
        {
            lock (_vanillaIdsLock)
            {
                if (_vanillaHircIds != null)
                    return;

                try
                {
                    _audioRepository.Load([Language]);
                    var languageId = WwiseHash.Compute(Language);
                    _vanillaHircIds = _audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
                    _vanillaSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);
                }
                catch (Exception e)
                {
                    _logger.Here().Warning($"Could not load audio data, so generated ids cannot be checked against vanilla: {e.Message}");
                    _vanillaHircIds = [];
                    _vanillaSourceIds = [];
                }
            }
        }
    }
}
