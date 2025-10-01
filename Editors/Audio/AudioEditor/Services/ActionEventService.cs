using System;
using System.Collections.Generic;
using System.IO;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IActionEventService
    {
        void AddActionEvent(string actionEventGroupName, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
        void RemoveActionEvent(string actionEventNodeName, string actionEventName);
    }

    public class ActionEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IActionEventFactory actionEventFactory) : IActionEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;

        public void AddActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName)); 
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            var actionEventType = Wh3ActionEventInformation.GetActionEventType(actionEventTypeName);

            var audioProject = _audioEditorStateService.AudioProject;

            var usedHircIds = new HashSet<uint>();
            var usedSourceIds = new HashSet<uint>();

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            var audioProjectSourceIds = audioProject.GetSourceIds();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var gameLanguageHircIds = _audioRepository.GetUsedHircIdsByLanguageId(languageId);
            var gameLanguageSourceIds = _audioRepository.GetUsedSourceIdsByLanguageId(languageId);

            usedHircIds.UnionWith(audioProjectGeneratableItemIds);
            usedHircIds.UnionWith(gameLanguageHircIds);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(gameLanguageSourceIds);

            var actionEvent = _actionEventFactory.CreatePlayActionEvent(usedHircIds, usedSourceIds, actionEventType, actionEventName, audioFiles, audioSettings);
            soundBank.InsertAlphabetically(actionEvent);

            if (soundBank.GameSoundBank == Wh3SoundBank.GlobalMusic)
            {
                var pauseActionEvent = _actionEventFactory.CreatePauseActionEvent(usedHircIds, actionEvent);
                soundBank.InsertAlphabetically(pauseActionEvent);

                var resumeActionEvent = _actionEventFactory.CreateResumeActionEvent(usedHircIds, actionEvent);
                soundBank.InsertAlphabetically(resumeActionEvent);

                var stopActionEvent = _actionEventFactory.CreateStopActionEvent(usedHircIds, actionEvent);
                soundBank.InsertAlphabetically(stopActionEvent);
            }
        }

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";

            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
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
        }
    }
}
