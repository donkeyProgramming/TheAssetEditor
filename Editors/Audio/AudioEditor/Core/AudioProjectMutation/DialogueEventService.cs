using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using HircSettings = Editors.Audio.Shared.AudioProject.Models.HircSettings;

namespace Editors.Audio.AudioEditor.Core.AudioProjectMutation
{
    public interface IDialogueEventService
    {
        void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, HircSettings hircSettings, List<KeyValuePair<string, string>> statePathList);
        bool RemoveStatePath(string dialogueEventName, string statePathName);
    }

    public class DialogueEventService(IAudioEditorStateService audioEditorStateService, IAudioRepository audioRepository, IStatePathFactory statePathFactory) : IDialogueEventService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IStatePathFactory _statePathFactory = statePathFactory;

        public void AddStatePath(string dialogueEventName, List<AudioFile> audioFiles, HircSettings hircSettings, List<KeyValuePair<string, string>> statePathList)
        {
            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, _audioEditorStateService.AudioProject);
            var usedSourceIds = IdGenerator.GetUsedSourceIds(_audioRepository, _audioEditorStateService.AudioProject);

            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3DialogueEventInformation.GetSoundBank(dialogueEventName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";
            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);
            var statePathFactoryResult = _statePathFactory.Create(statePathList, audioFiles, hircSettings, usedHircIds, usedSourceIds, soundBank.Language, actorMixerId: actorMixerId);
            dialogueEvent.StatePaths.InsertAlphabetically(statePathFactoryResult.StatePath);

            if (statePathFactoryResult.StatePath.TargetHircTypeIsSound())
            {
                soundBank.Sounds.TryAdd(statePathFactoryResult.SoundTarget);

                var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(statePathFactoryResult.SoundTarget.SourceId);
                if (audioFile == null)
                {
                    audioFile = audioFiles.FirstOrDefault(audioFile => audioFile.Id == statePathFactoryResult.SoundTarget.SourceId);
                    _audioEditorStateService.AudioProject.AudioFiles.TryAdd(audioFile);
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

        public bool RemoveStatePath(string dialogueEventName, string statePathName)
        {
            var gameSoundBankName = Wh3SoundBankInformation.GetName(Wh3DialogueEventInformation.GetSoundBank(dialogueEventName));
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            var soundBankName = $"{gameSoundBankName}_{audioProjectNameWithoutExtension}";

            var soundBank = _audioEditorStateService.AudioProject.GetSoundBank(soundBankName);

            var dialogueEvent = _audioEditorStateService.AudioProject.GetDialogueEvent(dialogueEventName);
            var statePath = dialogueEvent.GetStatePath(statePathName);
            if (statePath != null)
            {
                dialogueEvent.StatePaths.Remove(statePath);

                if (statePath.TargetHircTypeIsSound())
                {
                    var sound = soundBank.GetSound(statePath.TargetHircId);
                    var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(sound.SourceId);
                    audioFile.Sounds.Remove(sound.Id);

                    soundBank.Sounds.Remove(sound);
                    audioFile.Sounds.Remove(sound.Id);

                    if (audioFile.Sounds.Count == 0)
                        _audioEditorStateService.AudioProject.AudioFiles.Remove(audioFile);
                }
                else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                {
                    var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
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

                return true;
            }
            else
                return false;
        }
    }
}
