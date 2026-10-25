using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using HircSettings = Editors.Audio.Shared.AudioProject.Models.HircSettings;

namespace Editors.Audio.AudioEditor.Core.AudioProjectMutation
{
    public interface IActionEventService
    {
        void AddPlayActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings);
        void AddSetStateActionEvent(string actionEventTypeName, string actionEventName, string stateGroupName, string stateName, List<AudioFile> audioFiles);
        void AddPauseResumeStopActionEvent(string actionEventTypeName, string actionEventName);
        void RemoveActionEvent(string actionEventNodeName, string actionEventName);
    }

    public class ActionEventService(
        IAudioEditorStateService audioEditorStateService,
        IAudioRepository audioRepository,
        IActionEventFactory actionEventFactory,
        IMusicHierarchyFactory musicHierarchyFactory) : IActionEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;
        private readonly IMusicHierarchyFactory _musicHierarchyFactory = musicHierarchyFactory;

        private readonly ILogger _logger = Logging.Create<ActionEventService>();

        public void AddPlayActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings)
        {
            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, _audioEditorStateService.AudioProject);
            var usedSourceIds = IdGenerator.GetUsedSourceIds(_audioRepository, _audioEditorStateService.AudioProject);

            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var actionEventType = Wh3ActionEventInformation.GetActionEventType(actionEventTypeName);
            var playActionEventResult = _actionEventFactory.CreatePlayActionEvent(usedHircIds, usedSourceIds, actionEventType, actionEventName, audioFiles, hircSettings, soundBank.Id, soundBank.Language);
            soundBank.ActionEvents.InsertAlphabetically(playActionEventResult.ActionEvent);

            if (playActionEventResult.Actions.Count > 1)
                throw new NotSupportedException("Multiple Actions are not supported.");

            var action = playActionEventResult.Actions.FirstOrDefault();
            if (action.TargetHircTypeIsSound())
            {
                soundBank.Sounds.TryAdd(playActionEventResult.SoundTarget);

                var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(playActionEventResult.SoundTarget.SourceId);
                if (audioFile == null)
                {
                    audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == playActionEventResult.SoundTarget.SourceId);
                    _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
                }

                if (!audioFile.Sounds.Contains(playActionEventResult.SoundTarget.Id))
                    audioFile.Sounds.Add(playActionEventResult.SoundTarget.Id);
            }
            else if (action.TargetHircTypeIsRandomSequenceContainer())
            {
                soundBank.RandomSequenceContainers.TryAdd(playActionEventResult.RandomSequenceContainerTarget);
                soundBank.Sounds.AddRange(playActionEventResult.RandomSequenceContainerSounds);

                foreach (var sound in playActionEventResult.RandomSequenceContainerSounds)
                {
                    var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);
                    if (audioFile == null)
                    {
                        audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == sound.SourceId);
                        _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
                    }

                    if (!audioFile.Sounds.Contains(sound.Id))
                        audioFile.Sounds.Add(sound.Id);
                }
            }
        }

        /// <summary>
        /// A music Action Event, which sets a State instead of playing a Sound. The Event itself has
        /// no target objects, but setting a State only makes noise if the vanilla Music Switch
        /// container has a branch for it - so the audio picked in the Audio Files Explorer becomes
        /// the hierarchy under that branch rather than a Sound the Event points at.
        ///
        /// The State is also added to the project's State Group so it shows up in the Audio Explorer.
        /// </summary>
        public void AddSetStateActionEvent(string actionEventTypeName, string actionEventName, string stateGroupName, string stateName, List<AudioFile> audioFiles)
        {
            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, _audioEditorStateService.AudioProject);

            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var actionEventType = Wh3ActionEventInformation.GetActionEventType(actionEventTypeName);
            var result = _actionEventFactory.CreateSetStateActionEvent(usedHircIds, actionEventType, actionEventName, stateGroupName, stateName);
            soundBank.ActionEvents.InsertAlphabetically(result.ActionEvent);

            foreach (var stateGroupToRegisterIn in Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName))
                AddStateToStateGroup(stateGroupToRegisterIn, stateName);
            AddMusicBranch(soundBank, stateGroupName, stateName, audioFiles, usedHircIds);
        }

        /// <summary>
        /// The hierarchy the State selects. Nothing is added when no audio was picked - the Event is
        /// still worth having on its own, since it can set a State a branch added earlier already
        /// covers.
        /// </summary>
        private void AddMusicBranch(SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles, HashSet<uint> usedHircIds)
        {
            if (audioFiles == null || audioFiles.Count == 0)
                return;

            // The pulse Groups are not reached through a decision tree at all - a State there picks a
            // sub-track inside vanilla Music Tracks - so their audio is modelled and generated
            // differently even though a modder picks wavs for them the same way.
            if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(stateGroupName))
            {
                AddAmsPulse(soundBank, stateGroupName, stateName, audioFiles);
                return;
            }

            var fragmentContainerId = Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(stateGroupName);
            if (fragmentContainerId != null)
            {
                AddAmsFragment(soundBank, stateGroupName, stateName, audioFiles, usedHircIds, fragmentContainerId.Value);
                return;
            }

            var musicSwitchContainerIds = Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds(stateGroupName);
            if (musicSwitchContainerIds.Count == 0)
            {
                _logger.Here().Error($"State Group {stateGroupName} has no known Music Switch container, so its audio cannot be reached and was not added");
                return;
            }

            // One hierarchy per container: a music node has a single parent, so battle culture music
            // needs its own copy under the results container and under the in-battle one, both
            // playing the same audio.
            foreach (var musicSwitchContainerId in musicSwitchContainerIds)
                AddMusicBranch(soundBank, musicSwitchContainerId, stateGroupName, stateName, audioFiles, usedHircIds);
        }

        private void AddMusicBranch(SoundBank soundBank, uint musicSwitchContainerId, string stateGroupName, string stateName,
            List<AudioFile> audioFiles, HashSet<uint> usedHircIds)
        {
            // A branch per State and container, matched on its Group as well as its name - "Empire" is
            // a State in both the campaign subculture Group and the battle culture one, and they are
            // different branches in different containers.
            //
            // Picking more audio for a State that already has a branch extends its playlist rather
            // than starting a second one, since the decision tree can only point at one place.
            var existingRandomSequence = soundBank.MusicRandomSequences
                .FirstOrDefault(musicRandomSequence =>
                    musicRandomSequence.StateName == stateName && musicRandomSequence.StateGroupName == stateGroupName
                    && musicRandomSequence.DirectParentId == musicSwitchContainerId);

            if (existingRandomSequence != null)
            {
                var addedSegments = _musicHierarchyFactory
                    .CreateMusicBranch(usedHircIds, musicSwitchContainerId, stateGroupName, stateName, audioFiles, soundBank.Language)
                    .MusicSegments;

                foreach (var musicSegment in addedSegments)
                {
                    musicSegment.DirectParentId = existingRandomSequence.Id;
                    existingRandomSequence.Segments.Add(new MusicPlaylistEntry
                    {
                        SegmentId = musicSegment.Id,
                        PlaylistItemId = existingRandomSequence.PlaylistRootItemId + existingRandomSequence.Segments.Count + 1
                    });
                }

                AddMusicSegments(soundBank, addedSegments, audioFiles);
                return;
            }

            var branch = _musicHierarchyFactory.CreateMusicBranch(usedHircIds, musicSwitchContainerId, stateGroupName, stateName, audioFiles, soundBank.Language);
            soundBank.MusicRandomSequences.Add(branch.MusicRandomSequence);
            AddMusicSegments(soundBank, branch.MusicSegments, audioFiles);
        }

        /// <summary>
        /// Audio for one of the pulse Groups. Every wav becomes a clip, and the clips are spread over
        /// the vanilla switch tracks at compile time rather than here - which track a clip lands in
        /// depends on how many the game has, and that is not something the project should record.
        ///
        /// Picking more audio for a State that already has clips extends the list, the same as adding
        /// segments to an existing branch does.
        /// </summary>
        private void AddAmsPulse(SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles)
        {
            var amsPulse = soundBank.AmsPulses
                .FirstOrDefault(pulse => pulse.StateName == stateName && pulse.StateGroupName == stateGroupName);

            if (amsPulse == null)
            {
                amsPulse = new AmsPulse { StateGroupName = stateGroupName, StateName = stateName };
                soundBank.AmsPulses.Add(amsPulse);
            }

            foreach (var audioFile in audioFiles)
            {
                if (amsPulse.Clips.Any(clip => clip.SourceId == audioFile.Id))
                    continue;

                amsPulse.Clips.Add(new AmsPulseClip
                {
                    SourceId = audioFile.Id,
                    Language = soundBank.Language
                });

                if (_audioEditorStateService.AudioProject.GetAudioFile(audioFile.Id) == null)
                    _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
            }
        }

        /// <summary>
        /// Audio for the ambient fragments Group. This one does build a hierarchy: the wavs become
        /// Sounds, a Random Sequence container over them when there is more than one, and a Switch
        /// container on the musical key Group that the vanilla faction container will point at.
        ///
        /// The Sounds go in the bank's own Sound list, which is where the compiler already looks for
        /// wem paths, so they need no special handling beyond being generated.
        /// </summary>
        private void AddAmsFragment(
            SoundBank soundBank, string stateGroupName, string stateName, List<AudioFile> audioFiles,
            HashSet<uint> usedHircIds, uint fragmentContainerId)
        {
            var existing = soundBank.AmsFragments
                .FirstOrDefault(fragment => fragment.StateName == stateName && fragment.StateGroupName == stateGroupName);

            if (existing != null)
            {
                _logger.Here().Information(
                    $"'{stateName}' already has ambient fragments; remove the Event and add it again to change them");
                return;
            }

            var sounds = new List<Sound>();
            foreach (var audioFile in audioFiles)
            {
                var soundIds = IdGenerator.GenerateIds(usedHircIds);
                sounds.Add(Sound.CreateContainerSound(
                    soundIds.Guid, soundIds.Id, directParentId: 0, playlistOrder: sounds.Count,
                    sourceId: audioFile.Id, language: soundBank.Language));

                if (_audioEditorStateService.AudioProject.GetAudioFile(audioFile.Id) == null)
                    _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
            }

            if (sounds.Count == 0)
                return;

            var keySwitchIds = IdGenerator.GenerateIds(usedHircIds);

            // One Sound needs nothing over it; several need a container to pick between them, which
            // is the same shape a Play Action Event uses for a set of variations.
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
                    hircSettings: _audioEditorStateService.HircSettings,
                    children: [.. sounds.Select(sound => sound.Id)]));
            }

            foreach (var sound in sounds)
            {
                sound.DirectParentId = sounds.Count == 1 ? keySwitchIds.Id : targetHircId;
                soundBank.Sounds.TryAdd(sound);

                var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);
                if (audioFile != null && !audioFile.Sounds.Contains(sound.Id))
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

        /// <summary>The fragment hierarchy a removed Event's State built, dropped on the same
        /// condition as a music branch.</summary>
        private void RemoveAmsFragment(SoundBank soundBank, string stateGroupName, string stateName)
        {
            var amsFragment = soundBank.AmsFragments
                .FirstOrDefault(fragment => fragment.StateName == stateName && fragment.StateGroupName == stateGroupName);

            if (amsFragment == null)
                return;

            foreach (var soundId in amsFragment.SoundIds)
            {
                var sound = soundBank.GetSound(soundId);
                if (sound == null)
                    continue;

                soundBank.Sounds.Remove(sound);

                var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);
                if (audioFile == null)
                    continue;

                audioFile.Sounds.Remove(sound.Id);
                if (audioFile.Sounds.Count == 0)
                    _audioEditorStateService.AudioProject.AudioFiles.Remove(audioFile);
            }

            var container = soundBank.GetRandomSequenceContainer(amsFragment.TargetHircId);
            if (container != null)
                soundBank.RandomSequenceContainers.Remove(container);

            soundBank.AmsFragments.Remove(amsFragment);
        }

        /// <summary>The pulse clips a removed Event's State contributed, dropped on the same
        /// condition as a music branch: only when no remaining Event sets that State.</summary>
        private static void RemoveAmsPulse(SoundBank soundBank, string stateGroupName, string stateName)
        {
            var amsPulse = soundBank.AmsPulses
                .FirstOrDefault(pulse => pulse.StateName == stateName && pulse.StateGroupName == stateGroupName);

            if (amsPulse != null)
                soundBank.AmsPulses.Remove(amsPulse);
        }

        private void AddMusicSegments(SoundBank soundBank, List<MusicSegment> musicSegments, List<AudioFile> audioFiles)
        {
            foreach (var musicSegment in musicSegments)
            {
                soundBank.MusicSegments.Add(musicSegment);

                if (_audioEditorStateService.AudioProject.GetAudioFile(musicSegment.SourceId) == null)
                {
                    var audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == musicSegment.SourceId);
                    _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
                }
            }
        }

        private void AddStateToStateGroup(string stateGroupName, string stateName)
        {
            var stateGroup = _audioEditorStateService.AudioProject.StateGroups
                .FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);

            // Projects made before a Group became moddable do not have it yet.
            if (stateGroup == null && Wh3StateGroupInformation.ModdableStateGroups.Contains(stateGroupName))
            {
                stateGroup = StateGroup.CreateForAudioProjectFile(stateGroupName, []);
                _audioEditorStateService.AudioProject.StateGroups.TryAdd(stateGroup);
            }

            if (stateGroup == null)
                return;

            if (stateGroup.States.Any(state => state.Name == stateName))
                return;

            // Vanilla States are already known to the game, so re-adding one would only produce a
            // duplicate in the project's own list.
            if (_audioRepository.StatesByStateGroup.TryGetValue(stateGroupName, out var vanillaStates) && vanillaStates.Contains(stateName))
                return;

            stateGroup.States.InsertAlphabetically(new State(stateName));
        }

        public void AddPauseResumeStopActionEvent(string actionEventTypeName, string actionEventName)
        {
            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, _audioEditorStateService.AudioProject);
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var actionEventSuffix = TableHelpers.RemoveActionEventPrefix(actionEventName);
            var playActionEvent = soundBank.GetActionEvent($"Play_{actionEventSuffix}");

            if (actionEventName.StartsWith("Pause_"))
            {
                var pauseActionEventResult = _actionEventFactory.CreatePauseActionEvent(usedHircIds, playActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(pauseActionEventResult.ActionEvent);
            }
            else if (actionEventName.StartsWith("Resume_"))
            {
                var resumeActionEventResult = _actionEventFactory.CreateResumeActionEvent(usedHircIds, playActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(resumeActionEventResult.ActionEvent);
            }
            else if (actionEventName.StartsWith("Stop_"))
            {
                var stopActionEventResult = _actionEventFactory.CreateStopActionEvent(usedHircIds, playActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(stopActionEventResult.ActionEvent);
            }
        }

        /// <summary>
        /// The hierarchy a removed music Event's State selected. Another Event can set the same
        /// State, so the branch only goes when nothing is left pointing at it - otherwise removing
        /// one of two Events would silence both.
        /// </summary>
        private void RemoveMusicBranch(SoundBank soundBank, ActionEvent actionEvent)
        {
            var setStateActions = actionEvent.Actions
                .Where(action => action.ActionType == AkActionType.SetState)
                .ToList();

            foreach (var setStateAction in setStateActions)
            {
                var stillSet = soundBank.ActionEvents
                    .Any(otherActionEvent => otherActionEvent.Actions
                        .Any(action => action.ActionType == AkActionType.SetState
                            && action.StateName == setStateAction.StateName
                            && action.StateGroupName == setStateAction.StateGroupName));

                if (stillSet)
                    continue;

                RemoveAmsPulse(soundBank, setStateAction.StateGroupName, setStateAction.StateName);
                RemoveAmsFragment(soundBank, setStateAction.StateGroupName, setStateAction.StateName);

                var musicRandomSequence = soundBank.MusicRandomSequences
                    .FirstOrDefault(randomSequence =>
                        randomSequence.StateName == setStateAction.StateName
                        && randomSequence.StateGroupName == setStateAction.StateGroupName);

                if (musicRandomSequence == null)
                    continue;

                foreach (var musicSegment in soundBank.GetMusicSegments(musicRandomSequence))
                {
                    soundBank.MusicSegments.Remove(musicSegment);

                    var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(musicSegment.SourceId);
                    if (audioFile != null && audioFile.Sounds.Count == 0)
                        _audioEditorStateService.AudioProject.AudioFiles.Remove(audioFile);
                }

                soundBank.MusicRandomSequences.Remove(musicRandomSequence);
            }
        }

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";

            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            var actionEvent = soundBank.GetActionEvent(actionEventName);
            soundBank.ActionEvents.Remove(actionEvent);

            // We let the Play Action Event remove the target objects rather than Pause / Resume / Stop Action Events as otherwise they'd
            // be removing objects that the Play Action Event removal process has already removed.
            if (TableHelpers.IsPauseResumeStopActionEvent(actionEventName))
            {
                var actionEventSuffix = TableHelpers.RemoveActionEventPrefix(actionEventName);
                var playActionEventName = $"Play_{actionEventSuffix}";
                var playActionEvent = soundBank.GetActionEvent(playActionEventName);

                // To ensure the Play Action Event doesn't exist when it comes to handling the removal of Pause / Resume / Stop Action Events,
                // in the View Model when we send the rows to remove to the command we put the Play Action Events at the top of the list
                // so this should always be null but in case it somehow isn't we check here.
                if (playActionEvent != null)
                    throw new InvalidOperationException("Cannot remove Pause / Resume / Stop Action Event target objects while the Play Action Event still exists.");
                
                return;
            }

            RemoveMusicBranch(soundBank, actionEvent);

            foreach (var action in actionEvent.Actions)
            {
                if (action.TargetHircTypeIsSound())
                {
                    var sound = soundBank.GetSound(action.TargetHircId);
                    var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);

                    soundBank.Sounds.Remove(sound);
                    audioFile.Sounds.Remove(sound.Id);

                    if (audioFile.Sounds.Count == 0)
                        _audioEditorStateService.AudioProject.AudioFiles.Remove(audioFile);
                }
                else if (action.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(action.TargetHircId);
                    var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                    foreach (var sound in sounds)
                    {
                        var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);

                        soundBank.Sounds.Remove(sound);
                        audioFile.Sounds.Remove(sound.Id);

                        if (audioFile.Sounds.Count == 0)
                            _audioEditorStateService.AudioProject.AudioFiles.Remove(audioFile);
                    }

                    soundBank.RandomSequenceContainers.Remove(randomSequenceContainer);
                }
            }
        }
    }
}
