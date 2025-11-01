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
    public interface IDialogueEventService
    {
        void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, HircSettings hircSettings, Dictionary<string, string> stateLookupByStateGroup);
        bool RemoveStatePath(string dialogueEventName, string statePathName);
    }

    public class DialogueEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IStatePathFactory statePathFactory) : IDialogueEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IStatePathFactory _statePathFactory = statePathFactory;

        public void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, HircSettings hircSettings, Dictionary<string, string> stateLookupByStateGroup)
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

            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3DialogueEventInformation.GetSoundBank(dialogueEventName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);
            var statePathFactoryResult = _statePathFactory.Create(stateLookupByStateGroup, audioFiles, hircSettings, usedHircIds, usedSourceIds, soundBank.Language, actorMixerId: actorMixerId);
            dialogueEvent.StatePaths.InsertAlphabetically(statePathFactoryResult.StatePath);

            if (statePathFactoryResult.StatePath.TargetHircTypeIsSound())
            {
                soundBank.Sounds.TryAdd(statePathFactoryResult.SoundTarget);

                var audioFile = audioProject.GetAudioFile(statePathFactoryResult.SoundTarget.SourceId);
                if (audioFile == null)
                {
                    audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == statePathFactoryResult.SoundTarget.SourceId);
                    audioProject.AudioFiles.TryAdd(audioFile);
                }
                
                if (!audioFile.Sounds.Contains(statePathFactoryResult.SoundTarget.Id))
                    audioFile.Sounds.Add(statePathFactoryResult.SoundTarget.Id);
            }
            else if (statePathFactoryResult.StatePath.TargetHircTypeIsRandomSequenceContainer())
            {
                soundBank.RandomSequenceContainers.TryAdd(statePathFactoryResult.RandomSequenceContainerTarget);
                soundBank.Sounds.AddRange(statePathFactoryResult.RandomSequenceContainerSounds);

                foreach (var sound in statePathFactoryResult.RandomSequenceContainerSounds)
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

        public bool RemoveStatePath(string dialogueEventName, string statePathName)
        {
            var audioProject = _audioEditorStateService.AudioProject;
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3DialogueEventInformation.GetSoundBank(dialogueEventName));
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";

            var soundBank = audioProject.GetSoundBank(soundBankName);

            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePath = dialogueEvent.GetStatePath(statePathName);
            if (statePath != null)
            {
                dialogueEvent.StatePaths.Remove(statePath);

                if (statePath.TargetHircTypeIsSound())
                {
                    var sound = soundBank.GetSound(statePath.TargetHircId);
                    var audioFile = audioProject.GetAudioFile(sound.SourceId);
                    audioFile.Sounds.Remove(sound.Id);

                    soundBank.Sounds.Remove(sound);
                    audioFile.Sounds.Remove(sound.Id);

                    if (audioFile.Sounds.Count == 0)
                        audioProject.AudioFiles.Remove(audioFile);
                }
                else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
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

                return true;
            }
            else
                return false;
        }
    }
}
