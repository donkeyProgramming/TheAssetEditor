using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.Core.Misc;
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
    /// plays. See <see cref="Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds"/> for which
    /// State Groups can carry audio of their own.
    ///
    /// One project is meant to hold every culture: it is kept by whoever maintains the audio mixer
    /// mod, who adds a culture when its mod's team asks and supplies their wavs. Compiling it
    /// rebuilds the scripts from vanilla with every culture in it - see
    /// <see cref="IMusicScriptBuildService"/>.
    ///
    /// Everything here works on a project in memory; loading and saving it is the caller's.
    /// </summary>
    public interface IMusicAudioProjectService
    {
        /// <summary>An empty music project, ready for cultures.</summary>
        AudioProjectFile CreateNew();


        /// <summary>Adds the culture and the events it needs. A culture already in the project with
        /// the same Audio State and subculture key - or <paramref name="replacing"/>, when editing one
        /// changed either - is replaced in place, so adding it again updates it; events it no longer
        /// needs are removed, and ones it keeps keep their audio.</summary>
        MusicCultureAddResult AddCulture(AudioProjectFile audioProject, MusicCulture musicCulture, MusicCulture replacing = null);

        /// <summary>Removes the culture, and with it every event and branch no other culture in the
        /// project still needs.</summary>
        void RemoveCulture(AudioProjectFile audioProject, MusicCulture musicCulture);

        /// <summary>The audio the event's State currently selects, in playlist order.</summary>
        IReadOnlyList<AudioFile> GetAudio(AudioProjectFile audioProject, string eventName);

        /// <summary>Replaces the audio the event's State selects with the wavs at these pack paths,
        /// in playlist order. An empty list leaves the event with nothing of its own to play.</summary>
        void SetAudio(AudioProjectFile audioProject, string eventName, IReadOnlyList<string> wavPackFilePaths);
    }

    public class MusicAudioProjectService(
        IActionEventFactory actionEventFactory,
        IMusicHierarchyFactory musicHierarchyFactory,
        IAudioRepository audioRepository) : IMusicAudioProjectService
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

        private readonly ILogger _logger = Logging.Create<MusicAudioProjectService>();

        // Music always lives in the Sfx language folder regardless of what a project is otherwise
        // set to (global_music declares Sfx as its RequiredLanguage), so the project is Sfx outright
        // - that keeps the ID uniqueness scope, which is per language, the same one the SoundBank
        // will actually be compiled into.
        private static string Language => Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx);

        public AudioProjectFile CreateNew()
        {
            var audioProject = AudioProjectFile.CreateNew(Language, ProjectName);
            audioProject.MusicCultures = [];
            return audioProject;
        }

        public MusicCultureAddResult AddCulture(AudioProjectFile audioProject, MusicCulture musicCulture, MusicCulture replacing = null)
        {
            ArgumentNullException.ThrowIfNull(audioProject);
            ArgumentNullException.ThrowIfNull(musicCulture);

            // Populates the repository so GenerateActionEventId can see vanilla IDs and refuse a name
            // that already exists - adding a culture called "empire" would otherwise silently produce
            // a second music_b_faction_empire shadowing the real one.
            TryLoadAudioRepository();

            // Kept in its place, since the scripts are built with the cultures in project order.
            audioProject.MusicCultures ??= [];
            var index = audioProject.MusicCultures.FindIndex(other =>
                IsSameCulture(other, musicCulture) || (replacing != null && IsSameCulture(other, replacing)));
            var replacedCount = audioProject.MusicCultures.RemoveAll(other =>
                IsSameCulture(other, musicCulture) || (replacing != null && IsSameCulture(other, replacing)));
            audioProject.MusicCultures.Insert(index < 0 ? audioProject.MusicCultures.Count : index, musicCulture);

            // Removed only after the new entry is in, so an event both versions need is kept along
            // with any audio it was given.
            if (replacedCount != 0)
                RemoveEventsNoCultureNeeds(audioProject);

            var soundBank = GetOrCreateMusicSoundBank(audioProject);
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
                    AddMusicEvent(audioProject, soundBank, eventName);
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

            audioProject.MusicCultures?.RemoveAll(other => IsSameCulture(other, musicCulture));
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

            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(stateGroupName))
            {
                var sourceIds = soundBank.AmsPulses
                    .Where(amsPulse => IsFor(amsPulse.StateGroupName, amsPulse.StateName, stateGroupName, stateName))
                    .SelectMany(amsPulse => amsPulse.Clips)
                    .Select(clip => clip.SourceId);
                return GetAudioFiles(audioProject, sourceIds);
            }

            var amsFragment = soundBank.AmsFragments
                .FirstOrDefault(fragment => IsFor(fragment.StateGroupName, fragment.StateName, stateGroupName, stateName));
            if (amsFragment != null)
            {
                var sourceIds = amsFragment.SoundIds
                    .Select(soundBank.GetSound)
                    .Where(sound => sound != null)
                    .Select(sound => sound.SourceId);
                return GetAudioFiles(audioProject, sourceIds);
            }

            // A battle culture has a copy of its branch under each of the containers that branch on
            // it, all over the same audio, so one of them says what it plays.
            var musicRandomSequence = soundBank.MusicRandomSequences
                .FirstOrDefault(randomSequence => IsFor(randomSequence.StateGroupName, randomSequence.StateName, stateGroupName, stateName));
            if (musicRandomSequence == null)
                return [];

            return GetAudioFiles(audioProject, soundBank.GetMusicSegments(musicRandomSequence).Select(musicSegment => musicSegment.SourceId));
        }

        public void SetAudio(AudioProjectFile audioProject, string eventName, IReadOnlyList<string> wavPackFilePaths)
        {
            ArgumentNullException.ThrowIfNull(wavPackFilePaths);

            var soundBank = GetMusicSoundBank(audioProject);
            var setStateAction = GetSetStateAction(soundBank, eventName)
                ?? throw new InvalidOperationException($"The music project has no event called '{eventName}'.");

            var stateGroupName = setStateAction.StateGroupName;
            var stateName = setStateAction.StateName;

            if (!Wh3MusicHierarchyInformation.CanCarryOwnAudio(stateGroupName))
                throw new InvalidOperationException(
                    $"'{eventName}' sets {stateGroupName}, which nothing in the game branches on in a way a mod can extend, so audio given to it could never be reached.");

            TryLoadAudioRepository();

            // Resolved before the old branch goes, so a wav it shares with the new list keeps its id.
            var audioFiles = GetOrCreateAudioFiles(audioProject, wavPackFilePaths);

            RemoveMusicBranch(audioProject, soundBank, stateGroupName, stateName);
            if (audioFiles.Count != 0)
                AddMusicBranch(audioProject, soundBank, stateGroupName, stateName, audioFiles);
        }

        private List<AudioFile> GetOrCreateAudioFiles(AudioProjectFile audioProject, IReadOnlyList<string> wavPackFilePaths)
        {
            var usedSourceIds = IdGenerator.GetUsedSourceIds(_audioRepository, audioProject);

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

        private void AddMusicEvent(AudioProjectFile audioProject, SoundBank soundBank, string eventName)
        {
            if (!Wh3MusicEventInformation.TryResolveStateTarget(eventName, out var stateGroupName, out var stateName))
                throw new InvalidOperationException(
                    $"'{eventName}' does not follow any of the vanilla music event patterns ({string.Join(", ", Wh3MusicEventInformation.KnownEventPrefixes)}), " +
                    "so there is no way to tell which State Group it should set.");

            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, audioProject);

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

            var neededEventNames = (audioProject.MusicCultures ?? [])
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

        private void AddMusicBranch(AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles)
        {
            // The pulse Groups are not reached through a decision tree at all - a State there picks a
            // sub-track inside vanilla Music Tracks - so their audio is modelled and generated
            // differently even though a modder picks wavs for them the same way.
            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(stateGroupName))
            {
                AddAmsPulse(audioProject, soundBank, stateGroupName, stateName, audioFiles);
                return;
            }

            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, audioProject);

            var fragmentContainerId = Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(stateGroupName);
            if (fragmentContainerId != null)
            {
                AddAmsFragment(audioProject, soundBank, stateGroupName, stateName, audioFiles, usedHircIds, fragmentContainerId.Value);
                return;
            }

            // One hierarchy per container: a music node has a single parent, so battle culture music
            // needs its own copy under the results container and under the in-battle one, both
            // playing the same audio.
            foreach (var musicSwitchContainerId in Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds(stateGroupName))
            {
                var branch = _musicHierarchyFactory.CreateMusicBranch(usedHircIds, musicSwitchContainerId, stateGroupName, stateName, audioFiles, soundBank.Language);
                soundBank.MusicRandomSequences.Add(branch.MusicRandomSequence);
                soundBank.MusicSegments.AddRange(branch.MusicSegments);
            }

            foreach (var audioFile in audioFiles)
                AddAudioFile(audioProject, audioFile);
        }

        /// <summary>
        /// Audio for one of the pulse Groups. Every wav becomes a clip, and the clips are spread over
        /// the vanilla switch tracks at compile time rather than here - which track a clip lands in
        /// depends on how many the game has, and that is not something the project should record.
        /// </summary>
        private static void AddAmsPulse(AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles)
        {
            var amsPulse = new AmsPulse { StateGroupName = stateGroupName, StateName = stateName };
            foreach (var audioFile in audioFiles.DistinctBy(audioFile => audioFile.Id))
            {
                amsPulse.Clips.Add(new AmsPulseClip
                {
                    SourceId = audioFile.Id,
                    Language = soundBank.Language
                });
                AddAudioFile(audioProject, audioFile);
            }

            soundBank.AmsPulses.Add(amsPulse);
        }

        /// <summary>
        /// Audio for the ambient fragments Group. This one does build a hierarchy: the wavs become
        /// Sounds, a Random Sequence container over them when there is more than one, and a Switch
        /// container on the musical key Group that the vanilla faction container will point at.
        ///
        /// The Sounds go in the bank's own Sound list, which is where the compiler already looks for
        /// wem paths, so they need no special handling beyond being generated.
        /// </summary>
        private static void AddAmsFragment(
            AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles,
            HashSet<uint> usedHircIds, uint fragmentContainerId)
        {
            var sounds = new List<Sound>();
            foreach (var audioFile in audioFiles)
            {
                var soundIds = IdGenerator.GenerateIds(usedHircIds);
                sounds.Add(Sound.CreateContainerSound(
                    soundIds.Guid, soundIds.Id, directParentId: 0, playlistOrder: sounds.Count,
                    sourceId: audioFile.Id, language: soundBank.Language));
                AddAudioFile(audioProject, audioFile);
            }

            var keySwitchIds = IdGenerator.GenerateIds(usedHircIds);

            // One Sound needs nothing over it; several need a container to pick between them, which
            // is the same shape a Play Action Event uses for a set of variations, played at random.
            uint targetHircId;
            if (sounds.Count == 1)
            {
                targetHircId = sounds[0].Id;
            }
            else
            {
                var containerIds = IdGenerator.GenerateIds(usedHircIds);
                targetHircId = containerIds.Id;

                soundBank.RandomSequenceContainers.TryAdd(new RandomSequenceContainer(
                    containerIds.Guid, containerIds.Id, overrideBusId: 0, directParentId: keySwitchIds.Id,
                    hircSettings: new HircSettings(),
                    children: [.. sounds.Select(sound => sound.Id)]));
            }

            foreach (var sound in sounds)
            {
                sound.DirectParentId = sounds.Count == 1 ? keySwitchIds.Id : targetHircId;
                soundBank.Sounds.TryAdd(sound);

                var audioFile = audioProject.GetAudioFile(sound.SourceId);
                if (!audioFile.Sounds.Contains(sound.Id))
                    audioFile.Sounds.Add(sound.Id);
            }

            soundBank.AmsFragments.Add(new AmsFragment
            {
                StateGroupName = stateGroupName,
                StateName = stateName,
                FactionSwitchContainerId = fragmentContainerId,
                KeySwitchContainerId = keySwitchIds.Id,
                TargetHircId = targetHircId,
                SoundIds = [.. sounds.Select(sound => sound.Id)]
            });
        }

        /// <summary>Whatever the State selects, in every container that branches on it, and any
        /// audio file nothing else in the project plays.</summary>
        private static void RemoveMusicBranch(AudioProjectFile audioProject, SoundBank soundBank, string stateGroupName, string stateName)
        {
            soundBank.AmsPulses.RemoveAll(amsPulse => IsFor(amsPulse.StateGroupName, amsPulse.StateName, stateGroupName, stateName));

            foreach (var amsFragment in soundBank.AmsFragments.Where(fragment => IsFor(fragment.StateGroupName, fragment.StateName, stateGroupName, stateName)).ToList())
            {
                foreach (var sound in amsFragment.SoundIds.Select(soundBank.GetSound).Where(sound => sound != null))
                {
                    soundBank.Sounds.Remove(sound);
                    audioProject.GetAudioFile(sound.SourceId)?.Sounds.Remove(sound.Id);
                }

                var container = soundBank.GetRandomSequenceContainer(amsFragment.TargetHircId);
                if (container != null)
                    soundBank.RandomSequenceContainers.Remove(container);

                soundBank.AmsFragments.Remove(amsFragment);
            }

            foreach (var musicRandomSequence in soundBank.MusicRandomSequences.Where(randomSequence => IsFor(randomSequence.StateGroupName, randomSequence.StateName, stateGroupName, stateName)).ToList())
            {
                foreach (var musicSegment in soundBank.GetMusicSegments(musicRandomSequence))
                    soundBank.MusicSegments.Remove(musicSegment);

                soundBank.MusicRandomSequences.Remove(musicRandomSequence);
            }

            RemoveUnplayedAudioFiles(audioProject);
        }

        private static void RemoveUnplayedAudioFiles(AudioProjectFile audioProject)
        {
            var playedSourceIds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.MusicSegments.Select(musicSegment => musicSegment.SourceId)
                    .Concat(soundBank.AmsPulses.SelectMany(amsPulse => amsPulse.Clips).Select(clip => clip.SourceId)))
                .ToHashSet();

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

            // A saved project leaves out empty Groups, so a reopened one may not have it.
            if (stateGroup == null && Wh3StateGroupInformation.ModdableStateGroups.Contains(stateGroupName))
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

        // Failure here is not fatal: without game files the repository simply reports nothing used,
        // which costs the collision check but still yields a valid project.
        private void TryLoadAudioRepository()
        {
            try
            {
                _audioRepository.Load([Language]);
            }
            catch (Exception e)
            {
                _logger.Here().Warning($"Could not load audio data, so generated ids cannot be checked against vanilla: {e.Message}");
            }
        }
    }
}
