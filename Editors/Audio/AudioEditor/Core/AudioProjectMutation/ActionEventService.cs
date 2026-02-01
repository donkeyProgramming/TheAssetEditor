using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise;
using HircSettings = Editors.Audio.Shared.AudioProject.Models.HircSettings;

namespace Editors.Audio.AudioEditor.Core.AudioProjectMutation
{
    public interface IActionEventService
    {
        void AddPlayActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings);
        void AddPauseResumeStopActionEvent(string actionEventTypeName, string actionEventName);
        void RemoveActionEvent(string actionEventNodeName, string actionEventName);
    }

    public class ActionEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IActionEventFactory actionEventFactory) : IActionEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;

        public void AddPlayActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings)
        {
            var usedHircIds = GetUsedHircIds();
            var usedSourceIds = GetUsedSourceIds();

            var audioProject = _audioEditorStateService.AudioProject;
            var languageId = WwiseHash.Compute(audioProject.Language);

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

                var audioFile = audioProject.GetAudioFile(playActionEventResult.SoundTarget.SourceId);
                if (audioFile == null)
                {
                    audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == playActionEventResult.SoundTarget.SourceId);
                    audioProject.AudioFiles.TryAdd(audioFile);
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
                    var audioFile = audioProject.GetAudioFile(sound.SourceId);
                    if (audioFile == null)
                    {
                        audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == sound.SourceId);
                        audioProject.AudioFiles.TryAdd(audioFile);
                    }

                    if (!audioFile.Sounds.Contains(sound.Id))
                        audioFile.Sounds.Add(sound.Id);
                }
            }
        }

        public void AddPauseResumeStopActionEvent(string actionEventTypeName, string actionEventName)
        {
            var usedHircIds = GetUsedHircIds();
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

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";

            var soundBank = audioProject.GetSoundBank(soundBankName);
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

            foreach (var action in actionEvent.Actions)
            {
                if (action.TargetHircTypeIsSound())
                {
                    var sound = soundBank.GetSound(action.TargetHircId);
                    var audioFile = audioProject.GetAudioFile(sound.SourceId);

                    soundBank.Sounds.Remove(sound);
                    audioFile.Sounds.Remove(sound.Id);

                    if (audioFile.Sounds.Count == 0)
                        audioProject.AudioFiles.Remove(audioFile);
                }
                else if (action.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(action.TargetHircId);
                    var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                    foreach (var sound in sounds)
                    {
                        var audioFile = audioProject.GetAudioFile(sound.SourceId);

                        soundBank.Sounds.Remove(sound);
                        audioFile.Sounds.Remove(sound.Id);

                        if (audioFile.Sounds.Count == 0)
                            audioProject.AudioFiles.Remove(audioFile);
                    }

                    soundBank.RandomSequenceContainers.Remove(randomSequenceContainer);
                }
            }
        }

        private HashSet<uint> GetUsedHircIds()
        {
            var usedHircIds = new HashSet<uint>();
            var languageId = WwiseHash.Compute(_audioEditorStateService.AudioProject.Language);
            var audioProjectGeneratableItemIds = _audioEditorStateService.AudioProject.GetGeneratableItemIds();
            var languageHircIds = _audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
            usedHircIds.UnionWith(audioProjectGeneratableItemIds);
            usedHircIds.UnionWith(languageHircIds);
            return usedHircIds;
        }

        private HashSet<uint> GetUsedSourceIds()
        {
            var usedSourceIds = new HashSet<uint>();
            var languageId = WwiseHash.Compute(_audioEditorStateService.AudioProject.Language);
            var audioProjectSourceIds = _audioEditorStateService.AudioProject.GetAudioFileIds();
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);
            return usedSourceIds;
        }
    }
}
