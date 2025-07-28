using System.Collections.Generic;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IActionEventService
    {
        void AddActionEvent(string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, string soundBankName);
        void RemoveActionEvent(string soundBankName, string actionEventName);
    }

    public class ActionEventService(IAudioEditorService audioEditorService, IActionEventFactory actionEventFactory) : IActionEventService
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;

        public void AddActionEvent(string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, string soundBankName)
        {
            var actionEvent = _actionEventFactory.Create(actionEventName, audioFiles, audioSettings);
            var soundBank = _audioEditorService.AudioProject.GetSoundBank(soundBankName);
            soundBank.InsertAlphabetically(actionEvent);
        }

        public void RemoveActionEvent(string soundBankName, string actionEventName)
        {
            var soundBank = _audioEditorService.AudioProject.GetSoundBank(soundBankName);
            var actionEvent = soundBank.GetActionEvent(actionEventName);
            soundBank.ActionEvents.Remove(actionEvent);
        }
    }
}
