using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        void AddActionEvent(string actionEventGroupName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings);
        void RemoveActionEvent(string actionEventNodeName, string actionEventName);
    }

    public class ActionEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IActionEventFactory actionEventFactory) : IActionEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;

        public void AddActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, HircSettings hircSettings)
        {
            var usedHircIds = new HashSet<uint>();
            var usedSourceIds = new HashSet<uint>();

            var audioProject = _audioEditorStateService.AudioProject;
            var languageId = WwiseHash.Compute(audioProject.Language);

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            var languageHircIds = _audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
            usedHircIds.UnionWith(audioProjectGeneratableItemIds);
            usedHircIds.UnionWith(languageHircIds);

            var audioProjectSourceIds = audioProject.GetAudioFileIds();
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);

            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";
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

            if (soundBank.GameSoundBank == Wh3SoundBank.GlobalMusic)
            {
                var pauseActionEventResult = _actionEventFactory.CreatePauseActionEvent(usedHircIds, playActionEventResult.ActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(pauseActionEventResult.ActionEvent);

                var resumeActionEventResult = _actionEventFactory.CreateResumeActionEvent(usedHircIds, playActionEventResult.ActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(resumeActionEventResult.ActionEvent);

                var stopActionEventResult = _actionEventFactory.CreateStopActionEvent(usedHircIds, playActionEventResult.ActionEvent);
                soundBank.ActionEvents.InsertAlphabetically(stopActionEventResult.ActionEvent);
            }
        }

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";

            var soundBank = audioProject.GetSoundBank(soundBankName);
            var actionEvent = soundBank.GetActionEvent(actionEventName);
            soundBank.ActionEvents.Remove(actionEvent);

            if (soundBank.GameSoundBank == Wh3SoundBank.GlobalMusic)
            {
                var pauseActionEventName = string.Concat("Pause_", actionEvent.Name.AsSpan("Play_".Length));
                var pauseActionEvent = soundBank.GetActionEvent(pauseActionEventName);
                soundBank.ActionEvents.Remove(pauseActionEvent);

                var resumeActionEventName = string.Concat("Resume_", actionEvent.Name.AsSpan("Play_".Length));
                var resumeActionEvent = soundBank.GetActionEvent(resumeActionEventName);
                soundBank.ActionEvents.Remove(resumeActionEvent);

                var stopActionEventName = string.Concat("Stop_", actionEvent.Name.AsSpan("Play_".Length));
                var stopActionEvent = soundBank.GetActionEvent(stopActionEventName);
                soundBank.ActionEvents.Remove(stopActionEvent);
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
    }
}
