using System;
using System.Collections.Generic;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameInformation.Warhammer3;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IActionEventService
    {
        void AddActionEvent(string actionEventGroupName, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
        void RemoveActionEvent(string actionEventNodeName, string actionEventName);
    }

    public class ActionEventService(IAudioEditorStateService audioEditorStateService, IActionEventFactory actionEventFactory) : IActionEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;

        public void AddActionEvent(string actionEventTypeName, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var soundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventTypeName));
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            var actionEventType = Wh3ActionEventInformation.GetActionEventType(actionEventTypeName);
            var actionEvent = _actionEventFactory.CreatePlayActionEvent(actionEventType, actionEventName, audioFiles, audioSettings);
            soundBank.InsertAlphabetically(actionEvent);

            if (soundBank.GameSoundBank == Wh3SoundBank.GlobalMusic)
            {
                var pauseActionEvent = _actionEventFactory.CreatePauseActionEvent(actionEvent);
                soundBank.InsertAlphabetically(pauseActionEvent);

                var resumeActionEvent = _actionEventFactory.CreateResumeActionEvent(actionEvent);
                soundBank.InsertAlphabetically(resumeActionEvent);

                var stopActionEvent = _actionEventFactory.CreateStopActionEvent(actionEvent);
                soundBank.InsertAlphabetically(stopActionEvent);
            }
        }

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var soundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
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
