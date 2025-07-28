using System.Collections.Generic;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IDialogueEventService
    {
        void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, Dictionary<string, string> stateLookupByStateGroup);
        bool RemoveStatePath(string dialogueEventName, string statePathName);
    }

    public class DialogueEventService(IAudioEditorService audioEditorService, IStatePathFactory statePathFactory) : IDialogueEventService
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;
        private readonly IStatePathFactory _statePathFactory = statePathFactory;

        public void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, Dictionary<string, string> stateLookupByStateGroup)
        {
            var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePath = _statePathFactory.Create(stateLookupByStateGroup, audioFiles, audioSettings);
            dialogueEvent.InsertAlphabetically(statePath);
        }

        public bool RemoveStatePath(string dialogueEventName, string statePathName)
        {
            var dialogueEvent = _audioEditorService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePath = dialogueEvent.GetStatePath(statePathName);
            if (statePath != null)
            {
                dialogueEvent.StatePaths.Remove(statePath);
                return true;
            }
            else
                return false;
        }
    }
}
