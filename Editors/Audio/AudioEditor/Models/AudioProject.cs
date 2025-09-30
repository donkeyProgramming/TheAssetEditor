using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.GameInformation.Warhammer3.Wh3StateGroupInformation;

namespace Editors.Audio.AudioEditor.Models
{
    public abstract class AudioProjectItem : IComparable, IComparable<AudioProjectItem>, IEquatable<AudioProjectItem>
    {
        [JsonPropertyOrder(-4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Name { get; set; }

        [JsonPropertyOrder(-3)] 
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
        public Guid Guid { get; set; }

        [JsonPropertyOrder(-2)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
        public uint Id { get; set; }

        [JsonPropertyOrder(-1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
        public AkBkHircType HircType { get; set; }

        public int CompareTo(object obj) => CompareTo(obj as AudioProjectItem);
        public int CompareTo(AudioProjectItem other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);
        public bool Equals(AudioProjectItem other) => string.Equals(Name, other?.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as AudioProjectItem);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

        public static bool InsertAlphabeticallyUnique<T>(List<T> list, T item) where T : IComparable<T>
        {
            var index = list.BinarySearch(item);

            // Prevents duplicates being added
            if (index >= 0)
                return false;

            list.Insert(~index, item);
            return true;
        }
    }

    public class AudioProject
    {
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; }
        public List<StateGroup> StateGroups { get; set; }

        public static AudioProject Create(GameTypeEnum currentGame, string language, string nameWithoutExtension)
        {
            if (currentGame == GameTypeEnum.Warhammer3)
                return new AudioProject
                {
                    Language = language,
                    SoundBanks = CreateSoundBanks(nameWithoutExtension, language),
                    StateGroups = CreateStateGroups()
                };

            return null;
        }

        public static AudioProject Create(AudioProject cleanAudioProject, GameTypeEnum currentGame, string nameWithoutExtension)
        {
            var dirtyAudioProject = Create(currentGame, cleanAudioProject.Language, nameWithoutExtension);
            MergeSoundBanks(dirtyAudioProject.SoundBanks, cleanAudioProject.SoundBanks);
            MergeStateGroups(dirtyAudioProject.StateGroups, cleanAudioProject.StateGroups);
            return dirtyAudioProject;
        }

        public static AudioProject Clean(AudioProject audioProject)
        {
            return new AudioProject
            {
                Language = audioProject.Language,
                SoundBanks = CleanSoundBanks(audioProject.SoundBanks),
                StateGroups = CleanStateGroups(audioProject.StateGroups)
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
            return SoundBanks
                .SelectMany(soundBank => soundBank.GetSounds())
                .Where(sound => sound != null)
                .ToList();
        }

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

                                    if (action.Sound != null)
                                        audioProjectItems.Add(action.Sound);

                                    if (action.RandomSequenceContainer != null)
                                    {
                                        audioProjectItems.Add(action.RandomSequenceContainer);
                                        if (action.RandomSequenceContainer.Sounds != null)
                                            audioProjectItems.AddRange(action.RandomSequenceContainer.Sounds);
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

                                    if (statePath.Sound != null)
                                        audioProjectItems.Add(statePath.Sound);

                                    if (statePath.RandomSequenceContainer != null)
                                    {
                                        audioProjectItems.Add(statePath.RandomSequenceContainer);
                                        if (statePath.RandomSequenceContainer.Sounds != null)
                                            audioProjectItems.AddRange(statePath.RandomSequenceContainer.Sounds);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return audioProjectItems;
        }

        public HashSet<uint> GetGeneratableItemIds() => GetGeneratableItems().Select(item => item.Id).ToHashSet();

        public HashSet<uint> GetSourceIds() => GetSounds().Select(item => item.SourceId).ToHashSet();

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
                    language = Wh3LanguageInformation.GetGameLanguageAsString((Wh3GameLanguage)requiredLanguage);
                var soundBank = SoundBank.Create(soundBankName, gameSoundBank, language);

                if (Wh3ActionEventInformation.Contains(gameSoundBank))
                    soundBank.ActionEvents = [];

                if (Wh3DialogueEventInformation.Contains(gameSoundBank))
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = Wh3DialogueEventInformation.Information
                        .Where(dialogueEvent => dialogueEvent.SoundBank == gameSoundBank);
                    foreach (var filteredDialogueEvent in filteredDialogueEvents)
                    {
                        var dialogueEvent = DialogueEvent.Create(filteredDialogueEvent.Name);
                        soundBank.DialogueEvents.Add(dialogueEvent);
                    }
                }

                soundBanks.Add(soundBank);
            }

            soundBanks = soundBanks.OrderBy(soundBank => soundBank.Name).ToList();
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

        private static void MergeSoundBanks(List<SoundBank> dirtySoundBanks, List<SoundBank> cleanSoundBanks)
        {
            if (dirtySoundBanks == null || cleanSoundBanks == null)
                return;

            foreach (var cleanSoundBank in cleanSoundBanks)
            {
                if (cleanSoundBank == null)
                    continue;

                var dirtySoundBank = dirtySoundBanks.FirstOrDefault(soundBank => soundBank.Name == cleanSoundBank.Name);
                if (dirtySoundBank != null)
                    MergeSoundBank(dirtySoundBank, cleanSoundBank);
            }
        }

        private static void MergeSoundBank(SoundBank dirtySoundBank, SoundBank cleanSoundBank)
        {
            dirtySoundBank.Id = cleanSoundBank.Id;
            dirtySoundBank.GameSoundBank = cleanSoundBank.GameSoundBank;
            dirtySoundBank.Language = cleanSoundBank.Language;
            dirtySoundBank.LanguageId = cleanSoundBank.LanguageId;

            MergeDialogueEvents(dirtySoundBank.DialogueEvents, cleanSoundBank.DialogueEvents);
            MergeActionEvents(dirtySoundBank.ActionEvents, cleanSoundBank.ActionEvents);
        }

        private static void MergeDialogueEvents(List<DialogueEvent> dirtyDialogueEvents, List<DialogueEvent> cleanDialogueEvents)
        {
            if (dirtyDialogueEvents == null || cleanDialogueEvents == null)
                return;

            foreach (var cleanDialogueEvent in cleanDialogueEvents)
            {
                var dirtyDialogueEvent = dirtyDialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == cleanDialogueEvent.Name);
                if (dirtyDialogueEvent != null)
                {
                    dirtyDialogueEvents.Remove(dirtyDialogueEvent);
                    dirtyDialogueEvents.Add(cleanDialogueEvent);
                }
            }
        }

        private static void MergeActionEvents(List<ActionEvent> dirtyActionEvents, List<ActionEvent> cleanActionEvents)
        {
            if (dirtyActionEvents == null || cleanActionEvents == null)
                return;

            foreach (var cleanActionEvent in cleanActionEvents)
                dirtyActionEvents.Add(cleanActionEvent);
        }

        private static void MergeStateGroups(List<StateGroup> dirtyStateGroups, List<StateGroup> cleanStateGroups)
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
            if (soundBanks == null)
                return null;

            var processedBanks = soundBanks
                .Where(soundBank => soundBank != null)
                .Select(ProcessSoundBank)
                .Where(soundBank =>
                    soundBank.DialogueEvents != null && soundBank.DialogueEvents.Count != 0 ||
                    soundBank.ActionEvents != null && soundBank.ActionEvents.Count != 0)
                .ToList();

            return processedBanks.Count != 0 ? processedBanks : null;
        }

        private static SoundBank ProcessSoundBank(SoundBank soundBank)
        {
            soundBank.DialogueEvents = (soundBank.DialogueEvents ?? [])
                .Where(dialogueEvent => dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            soundBank.ActionEvents = (soundBank.ActionEvents ?? [])
                .Where(actionEvent => actionEvent.Actions != null && actionEvent.Actions.Count != 0)
                .ToList();

            return soundBank;
        }


        private static List<StateGroup> CleanStateGroups(List<StateGroup> stateGroups)
        {
            if (stateGroups == null)
                return null;

            var filteredStateGroups = stateGroups
                .Where(stateGroup => stateGroup.States != null && stateGroup.States.Count != 0)
                .ToList();

            return filteredStateGroups.Count != 0 ? filteredStateGroups : null;
        }
    }
}
