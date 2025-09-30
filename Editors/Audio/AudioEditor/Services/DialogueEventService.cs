using System.Collections.Generic;
using Editors.Audio.AudioEditor.Factories;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IDialogueEventService
    {
        void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, Dictionary<string, string> stateLookupByStateGroup);
        bool RemoveStatePath(string dialogueEventName, string statePathName);
    }

    public class DialogueEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IStatePathFactory statePathFactory) : IDialogueEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IStatePathFactory _statePathFactory = statePathFactory;

        public void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, AudioSettings audioSettings, Dictionary<string, string> stateLookupByStateGroup)
        {
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);

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

            var statePath = _statePathFactory.Create(stateLookupByStateGroup, audioFiles, audioSettings, usedHircIds, usedSourceIds, actorMixerId);
            dialogueEvent.InsertAlphabetically(statePath);
        }

        public bool RemoveStatePath(string dialogueEventName, string statePathName)
        {
            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
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
