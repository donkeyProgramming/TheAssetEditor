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

        public void AddActionEvent(string actionEventGroupName, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var soundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventGroupName));
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            var actionEventGroup = Wh3ActionEventInformation.GetActionEventGroup(actionEventGroupName);
            var actionEvent = _actionEventFactory.Create(actionEventGroup, actionEventName, audioFiles, audioSettings);
            soundBank.InsertAlphabetically(actionEvent);
        }

        public void RemoveActionEvent(string actionEventNodeName, string actionEventName)
        {
            var soundBankName = Wh3SoundBankInformation.GetName(Wh3ActionEventInformation.GetSoundBank(actionEventNodeName));
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);
            var actionEvent = soundBank.GetActionEvent(actionEventName);
            soundBank.ActionEvents.Remove(actionEvent);
        }
    }
}
