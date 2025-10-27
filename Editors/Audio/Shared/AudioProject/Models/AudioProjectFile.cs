using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.Core.Settings;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3StateGroupInformation;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class AudioProjectFile
    {
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; }
        public List<StateGroup> StateGroups { get; set; }
        public List<AudioFile> AudioFiles { get; set; }

        public static AudioProjectFile Create(GameTypeEnum currentGame, string language, string nameWithoutExtension, List<AudioFile> audioFiles = null)
        {
            if (currentGame == GameTypeEnum.Warhammer3)
                return new AudioProjectFile
                {
                    Language = language,
                    SoundBanks = CreateSoundBanks(nameWithoutExtension, language),
                    StateGroups = CreateStateGroups(),
                    AudioFiles = audioFiles ?? []
                };

            return null;
        }

        public static AudioProjectFile Create(AudioProjectFile cleanAudioProject, GameTypeEnum currentGame, string nameWithoutExtension)
        {
            var dirtyAudioProject = Create(currentGame, cleanAudioProject.Language, nameWithoutExtension, cleanAudioProject.AudioFiles);
            MergeCleanIntoDirtySoundBanks(dirtyAudioProject.SoundBanks, cleanAudioProject.SoundBanks);
            AddCleanToDirtyStateGroups(dirtyAudioProject.StateGroups, cleanAudioProject.StateGroups);
            return dirtyAudioProject;
        }

        public void Merge(AudioProjectFile mergingAudioProject, string baseAudioProjectFileName, string mergingAudioProjectFileName)
        {
            foreach (var mergingSoundBank in mergingAudioProject.SoundBanks)
            {
                if (mergingSoundBank == null)
                    continue;

                var baseAudioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseAudioProjectFileName);
                var mergingAudioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(mergingAudioProjectFileName);
                var mergingSoundBankBaseName = mergingSoundBank.Name.Replace($"_{mergingAudioProjectFileNameWithoutExtension}", string.Empty);
                var baseSoundBank = SoundBanks.FirstOrDefault(soundBank => soundBank.Name.Replace($"_{baseAudioProjectFileNameWithoutExtension}", string.Empty) == mergingSoundBankBaseName);

                if (baseSoundBank != null)
                {
                    foreach (var mergingDialogueEvent in mergingSoundBank.DialogueEvents)
                    {
                        var baseDialogueEvent = baseSoundBank.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == mergingDialogueEvent.Name);
                        if (baseDialogueEvent != null)
                        {
                            baseDialogueEvent.StatePaths.AddRange(baseDialogueEvent.StatePaths);
                            var sortedStatePaths = baseDialogueEvent.StatePaths
                                .OrderBy(statePath => statePath.Name, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                            baseDialogueEvent.StatePaths.Clear();
                            baseDialogueEvent.StatePaths.AddRange(sortedStatePaths);
                            baseSoundBank.DialogueEvents.Add(mergingDialogueEvent);
                        }
                        else
                            baseSoundBank.DialogueEvents.Add(mergingDialogueEvent);
                    }

                    foreach (var mergingActionEvent in mergingSoundBank.ActionEvents)
                        baseSoundBank.ActionEvents.Add(mergingActionEvent);

                    var sortedActionEvents = baseSoundBank.ActionEvents
                        .OrderBy(actionEvent => actionEvent.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    baseSoundBank.ActionEvents.Clear();
                    baseSoundBank.ActionEvents.AddRange(sortedActionEvents);
                }

                var baseSoundBankIds = GetSoundBankSoundIds(baseSoundBank.Name);

                foreach (var sound in mergingSoundBank.Sounds)
                    baseSoundBank.Sounds.TryAdd(sound);

                foreach (var randomSequenceContainer in mergingSoundBank.RandomSequenceContainers)
                    baseSoundBank.RandomSequenceContainers.TryAdd(randomSequenceContainer);
            }
        }

        public static AudioProjectFile Clean(AudioProjectFile audioProject)
        {
            return new AudioProjectFile
            {
                Language = audioProject.Language,
                SoundBanks = CleanSoundBanks(audioProject.SoundBanks),
                StateGroups = CleanStateGroups(audioProject.StateGroups),
                AudioFiles = audioProject.AudioFiles
            };
        }

        public List<SoundBank> GetEditedActionEventSoundBanks()
        {
            return SoundBanks
                .Where(soundBank => Wh3ActionEventInformation.Contains(soundBank.GameSoundBank) && soundBank.ActionEvents.Count > 0)
                .ToList();
        }

        public List<SoundBank> GetEditedDialogueEventSoundBanks()
        {
            return SoundBanks
                .Where(soundBank => Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank)
                    && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.StatePaths.Count > 0))
                .ToList();
        }

        public List<SoundBank> GetEditedSoundBanks()
        {
            var editedActionEventSoundBanks = GetEditedActionEventSoundBanks();
            var editedDialogueEventSoundBanks = GetEditedDialogueEventSoundBanks();
            var editedSoundBanks = editedActionEventSoundBanks
                .Union(editedDialogueEventSoundBanks)
                .ToList();
            return editedSoundBanks;
        }

        public List<StateGroup> GetEditedStateGroups()
        {
            return StateGroups
                .Where(state => state.States.Count > 0)
                .ToList();
        }

        public SoundBank GetSoundBank(string soundBankName)
        {
            return SoundBanks.FirstOrDefault(soundBank => soundBank.Name == soundBankName);
        }

        public ActionEvent GetActionEvent(string actionEventName)
        {
            return SoundBanks
                .Where(soundBank => Wh3ActionEventInformation.Contains(soundBank.GameSoundBank))
                .SelectMany(soundBank => soundBank.ActionEvents)
                .FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);
        }

        public DialogueEvent GetDialogueEvent(string dialogueEventName)
        {
            return SoundBanks
                .Where(soundBank => Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank))
                .SelectMany(soundBank => soundBank.DialogueEvents)
                .FirstOrDefault(dialogueEvent => dialogueEvent.Name == dialogueEventName);
        }

        public StateGroup GetStateGroup(string stateGroupName)
        {
            return StateGroups.FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);
        }

        public List<Sound> GetSounds()
        {
            return (SoundBanks ?? [])
                .SelectMany(soundBank => soundBank?.Sounds ?? [])
                .Where(sound => sound != null)
                .ToList();
        }

        public HashSet<uint> GetSoundBankSoundIds(string soundBankName)
        {
            var soundBank = SoundBanks.FirstOrDefault(soundBank => soundBank.Name == soundBankName);
            return soundBank.Sounds.Select(sound => sound.Id).ToHashSet();
        }

        public HashSet<uint> GetAudioFileIds() => AudioFiles.Select(audioFile => audioFile.Id).ToHashSet();

        public AudioFile GetAudioFile(string filePath) => AudioFiles.FirstOrDefault(audioFile => string.Equals(audioFile.WavPackFilePath, filePath, StringComparison.OrdinalIgnoreCase));

        public AudioFile GetAudioFile(uint sourceId) => AudioFiles.FirstOrDefault(audioFile => audioFile.Id == sourceId);

        public List<SoundBank> GetSoundBanksWithActionEvents()
        {
            return SoundBanks.Where(soundBank => soundBank.ActionEvents != null && soundBank.ActionEvents.Count != 0).ToList();
        }

        public List<SoundBank> GetSoundBanksWithDialogueEvents()
        {
            return SoundBanks.Where(soundBank => soundBank.DialogueEvents != null && soundBank.DialogueEvents.Count != 0).ToList();
        }

        public List<ActionEvent> GetActionEvents()
        {
            return SoundBanks?
                .Where(soundBank => soundBank?.ActionEvents != null)
                .SelectMany(soundBank => soundBank.ActionEvents)
                .ToList()
                ?? [];
        }

        public HashSet<uint> GetGeneratableItemIds() => GetGeneratableItems().Select(item => item.Id).ToHashSet();

        public List<AudioProjectItem> GetGeneratableItems()
        {
            var audioProjectItems = new List<AudioProjectItem>();

            if (SoundBanks != null)
            {
                foreach (var soundBank in SoundBanks)
                {
                    if (soundBank == null) 
                        continue;
                    audioProjectItems.Add(soundBank);

                    if (soundBank.ActionEvents != null)
                    {
                        foreach (var actionEvent in soundBank.ActionEvents)
                        {
                            if (actionEvent == null) 
                                continue;
                            audioProjectItems.Add(actionEvent);

                            if (actionEvent.Actions != null)
                            {
                                foreach (var action in actionEvent.Actions)
                                {
                                    if (action == null) 
                                        continue;

                                    audioProjectItems.Add(action);

                                    if (action.TargetHircTypeIsSound())
                                    {
                                        var sound = soundBank.GetSound(action.TargetHircId);
                                        audioProjectItems.Add(sound);
                                    }

                                    if (action.TargetHircTypeIsRandomSequenceContainer())
                                    {
                                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(action.TargetHircId);
                                        audioProjectItems.Add(randomSequenceContainer);

                                        var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                                        audioProjectItems.AddRange(sounds);
                                    }
                                }
                            }
                        }
                    }

                    if (soundBank.DialogueEvents != null)
                    {
                        foreach (var dialogueEvent in soundBank.DialogueEvents)
                        {
                            if (dialogueEvent == null) 
                                continue;

                            audioProjectItems.Add(dialogueEvent);

                            if (dialogueEvent.StatePaths != null)
                            {
                                foreach (var statePath in dialogueEvent.StatePaths)
                                {
                                    if (statePath == null) 
                                        continue;

                                    if (statePath.TargetHircTypeIsSound())
                                    {
                                        var sound = soundBank.GetSound(statePath.TargetHircId);
                                        audioProjectItems.Add(sound);
                                    }

                                    if (statePath.TargetHircTypeIsRandomSequenceContainer())
                                    {
                                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                                        audioProjectItems.Add(randomSequenceContainer);

                                        var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
                                        audioProjectItems.AddRange(sounds);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return audioProjectItems;
        }

        private static List<SoundBank> CreateSoundBanks(string audioProjectNameWithoutExtension, string audioProjectLanguage)
        {
            var soundBanks = new List<SoundBank>();

            foreach (var soundBankDefinition in Wh3SoundBankInformation.Information)
            {
                var gameSoundBank = soundBankDefinition.GameSoundBank;
                var soundBankInformationName = Wh3SoundBankInformation.GetName(gameSoundBank);
                var soundBankName = $"{soundBankInformationName}_{audioProjectNameWithoutExtension}";

                var language = audioProjectLanguage;
                var requiredLanguage = Wh3SoundBankInformation.GetRequiredLanguage(gameSoundBank);
                if (requiredLanguage != null)
                    language = Wh3LanguageInformation.GetLanguageAsString((Wh3Language)requiredLanguage);

                var soundBank = SoundBank.Create(soundBankName, gameSoundBank, language);

                if (Wh3ActionEventInformation.Contains(gameSoundBank))
                    soundBank.ActionEvents = [];

                if (Wh3DialogueEventInformation.Contains(gameSoundBank))
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = Wh3DialogueEventInformation.Information.Where(dialogueEvent => dialogueEvent.SoundBank == gameSoundBank);
                    foreach (var filteredDialogueEvent in filteredDialogueEvents)
                    {
                        var dialogueEvent = DialogueEvent.Create(filteredDialogueEvent.Name);
                        soundBank.DialogueEvents.Add(dialogueEvent);
                    }

                    var sortedDialogueEvents = soundBank.DialogueEvents
                        .OrderBy(dialogueEvent => dialogueEvent?.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    soundBank.DialogueEvents.Clear();
                    soundBank.DialogueEvents.AddRange(sortedDialogueEvents);
                }

                soundBank.Sounds = [];
                soundBank.RandomSequenceContainers = [];

                soundBanks.Add(soundBank);
            }

            var sortedSoundBanks = soundBanks
                .OrderBy(soundBank => soundBank?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            soundBanks.Clear();
            soundBanks.AddRange(sortedSoundBanks);
            return soundBanks;
        }

        private static List<StateGroup> CreateStateGroups()
        {
            var stateGroups = new List<StateGroup>();
            foreach (var moddableStateGroup in ModdableStateGroups)
            {
                var stateGroup = StateGroup.Create(moddableStateGroup, []);
                stateGroups.Add(stateGroup);
            }
            return stateGroups;
        }

        private static void MergeCleanIntoDirtySoundBanks(List<SoundBank> dirtySoundBanks, List<SoundBank> cleanSoundBanks)
        {
            if (dirtySoundBanks == null || cleanSoundBanks == null)
                return;

            foreach (var cleanSoundBank in cleanSoundBanks)
            {
                if (cleanSoundBank == null)
                    continue;

                var dirtySoundBank = dirtySoundBanks.FirstOrDefault(soundBank => soundBank.Name == cleanSoundBank.Name);
                if (dirtySoundBank != null)
                {
                    OverwriteDirtyWithCleanDialogueEvents(dirtySoundBank.DialogueEvents, cleanSoundBank.DialogueEvents);
                    AddCleanIntoDirtyActionEvents(dirtySoundBank.ActionEvents, cleanSoundBank.ActionEvents);
                }

                dirtySoundBank.Sounds = cleanSoundBank.Sounds;
                dirtySoundBank.RandomSequenceContainers = cleanSoundBank.RandomSequenceContainers;
            }
        }

        private static void OverwriteDirtyWithCleanDialogueEvents(List<DialogueEvent> dirtyDialogueEvents, List<DialogueEvent> cleanDialogueEvents)
        {
            if (dirtyDialogueEvents == null || cleanDialogueEvents == null)
                return;

            foreach (var cleanDialogueEvent in cleanDialogueEvents)
            {
                var dirtyDialogueEvent = dirtyDialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == cleanDialogueEvent.Name);
                if (dirtyDialogueEvent != null)
                {
                    dirtyDialogueEvents.Remove(dirtyDialogueEvent);

                    var sortedStatePaths = cleanDialogueEvent.StatePaths
                        .OrderBy(statePath => statePath.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    cleanDialogueEvent.StatePaths.Clear();
                    cleanDialogueEvent.StatePaths.AddRange(sortedStatePaths);
                    dirtyDialogueEvents.Add(cleanDialogueEvent);
                }
            }
        }

        private static void AddCleanIntoDirtyActionEvents(List<ActionEvent> dirtyActionEvents, List<ActionEvent> cleanActionEvents)
        {
            if (dirtyActionEvents == null || cleanActionEvents == null)
                return;

            foreach (var cleanActionEvent in cleanActionEvents)
                dirtyActionEvents.Add(cleanActionEvent);

            var sortedActionEvents = dirtyActionEvents
                .OrderBy(actionEvent => actionEvent.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            dirtyActionEvents.Clear();
            dirtyActionEvents.AddRange(sortedActionEvents);
        }

        private static void AddCleanToDirtyStateGroups(List<StateGroup> dirtyStateGroups, List<StateGroup> cleanStateGroups)
        {
            if (dirtyStateGroups == null || cleanStateGroups == null)
                return;

            foreach (var cleanStateGroup in cleanStateGroups)
            {
                var dirtyStateGroup = dirtyStateGroups.FirstOrDefault(stateGroup => stateGroup.Name == cleanStateGroup.Name);
                if (dirtyStateGroup != null)
                    dirtyStateGroup.States = cleanStateGroup.States;
            }
        }

        private static List<SoundBank> CleanSoundBanks(List<SoundBank> soundBanks)
        {
            var cleanedSoundBanks = soundBanks
                .Where(originalSoundBank => originalSoundBank != null)
                .Select(CleanSoundBank)
                .Where(cleanedSoundBank => cleanedSoundBank != null)
                .ToList();

            return cleanedSoundBanks.Count != 0 ? cleanedSoundBanks : [];
        }

        private static SoundBank CleanSoundBank(SoundBank originalSoundBank)
        {
            if (originalSoundBank == null)
                return null;

            var cleanedDialogueEvents = (originalSoundBank.DialogueEvents ?? [])
                .Where(dialogueEvent =>
                    dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            var cleanedActionEvents = (originalSoundBank.ActionEvents ?? [])
                .Where(actionEvent =>
                    actionEvent.Actions != null && actionEvent.Actions.Count != 0)
                .ToList();

            if (cleanedDialogueEvents.Count == 0 && cleanedActionEvents.Count == 0)
                return null;

            return new SoundBank
            {
                Id = originalSoundBank.Id,
                Name = originalSoundBank.Name,
                Language = originalSoundBank.Language,
                LanguageId = originalSoundBank.LanguageId,
                FileName = originalSoundBank.FileName,
                FilePath = originalSoundBank.FilePath,
                TestingId = originalSoundBank.TestingId,
                TestingFileName = originalSoundBank.TestingFileName,
                TestingFilePath = originalSoundBank.TestingFilePath,
                MergingId = originalSoundBank.MergingId,
                MergingFileName = originalSoundBank.MergingFileName,
                MergingFilePath = originalSoundBank.MergingFilePath,
                GameSoundBank = originalSoundBank.GameSoundBank,
                DialogueEvents = cleanedDialogueEvents,
                ActionEvents = cleanedActionEvents,
                Sounds = originalSoundBank.Sounds,
                RandomSequenceContainers = originalSoundBank.RandomSequenceContainers
            };
        }

        private static List<StateGroup> CleanStateGroups(List<StateGroup> stateGroups)
        {
            var filteredStateGroups = stateGroups
                .Where(stateGroup => stateGroup.States != null && stateGroup.States.Count != 0)
                .ToList();

            return filteredStateGroups.Count != 0 ? filteredStateGroups : [];
        }
    }
}
