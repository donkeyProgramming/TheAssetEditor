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
    // TODO: Reevaluate the use of AudioProjectItem, AudioProjectHircItem etc.
    public abstract class AudioProjectItem : IComparable, IComparable<AudioProjectItem>, IEquatable<AudioProjectItem>
    {
        [JsonPropertyOrder(-2)] public string Name { get; set; }
        [JsonPropertyOrder(-1)] public uint Id { get; set; }

        public int CompareTo(object obj) => CompareTo(obj as AudioProjectItem);
        public int CompareTo(AudioProjectItem other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);
        public bool Equals(AudioProjectItem other) => string.Equals(Name, other?.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as AudioProjectItem);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

        public static bool InsertAlphabeticallyUnique<T>(List<T> list, T item) where T : IComparable<T>
        {
            var index = list.BinarySearch(item);
            if (index >= 0) 
                return false; // Don't add duplicates

            list.Insert(~index, item);
            return true;
        }
    }

    public abstract class AudioProjectHircItem : AudioProjectItem
    {
        public abstract AkBkHircType HircType { get; set; }
    }

    public class AudioProject
    {
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; }
        public List<StateGroup> StateGroups { get; set; }

        public static AudioProject Create(GameTypeEnum currentGame, string language)
        {
            if (currentGame == GameTypeEnum.Warhammer3)
            {
                return new AudioProject
                {
                    Language = language,
                    SoundBanks = CreateSoundBanks(),
                    StateGroups = CreateStateGroups()
                };
            }

            return null;
        }

        private static List<SoundBank> CreateSoundBanks()
        {
            var soundBanks = new List<SoundBank>();

            foreach (var soundBankDefinition in Wh3SoundBankInformation.Information)
            {
                var gameSoundBank = soundBankDefinition.GameSoundBank;
                var soundBankName = Wh3SoundBankInformation.GetName(gameSoundBank);
                var soundBank = SoundBank.Create(soundBankName, gameSoundBank);

                if (Wh3ActionEventInformation.Contains(gameSoundBank))
                    soundBank.ActionEvents = [];
                
                if (Wh3DialogueEventInformation.Contains(gameSoundBank))
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = Wh3DialogueEventInformation.Information
                        .Where(dialogueEvent => dialogueEvent.SoundBank == Wh3SoundBankInformation.GetSoundBank(soundBankName));
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

        public static AudioProject Create(AudioProject cleanAudioProject, GameTypeEnum currentGame)
        {
            var dirtyAudioProject = Create(currentGame, cleanAudioProject.Language);
            MergeSoundBanks(dirtyAudioProject.SoundBanks, cleanAudioProject.SoundBanks);
            MergeStateGroups(dirtyAudioProject.StateGroups, cleanAudioProject.StateGroups);
            return dirtyAudioProject;
        }

        private static void MergeSoundBanks(List<SoundBank> dirtySoundBanks, List<SoundBank> cleanSoundBanks)
        {
            if (cleanSoundBanks == null)
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
            MergeDialogueEvents(dirtySoundBank.DialogueEvents, cleanSoundBank.DialogueEvents);
            MergeActionEvents(dirtySoundBank.ActionEvents, cleanSoundBank.ActionEvents);
        }

        private static void MergeDialogueEvents(List<DialogueEvent> dirtyDialogueEvents, List<DialogueEvent> cleanDialogueEvents)
        {
            if (cleanDialogueEvents == null) 
                return;

            foreach (var cleanDialogueEvent in cleanDialogueEvents)
            {
                var dirtyDialogueEvent = dirtyDialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == cleanDialogueEvent.Name);
                if (dirtyDialogueEvent != null)
                    dirtyDialogueEvent.StatePaths = cleanDialogueEvent.StatePaths;
            }
        }

        private static void MergeActionEvents(List<ActionEvent> dirtyActionEvents, List<ActionEvent> cleanActionEvents)
        {
            if (cleanActionEvents == null) 
                return;

            foreach (var cleanActionEvent in cleanActionEvents)
                dirtyActionEvents.Add(cleanActionEvent);
        }

        private static void MergeStateGroups(List<StateGroup> dirtyStateGroups, List<StateGroup> cleanStateGroups)
        {
            if (cleanStateGroups == null) 
                return;

            foreach (var cleanStateGroup in cleanStateGroups)
            {
                var dirtyStateGroup = dirtyStateGroups.FirstOrDefault(stateGroup => stateGroup.Name == cleanStateGroup.Name);
                if (dirtyStateGroup != null)
                    dirtyStateGroup.States = cleanStateGroup.States;
            }
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
            var dialogueEvents = (soundBank.DialogueEvents ?? [])
                .Where(dialogueEvent => dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            var actionEvents = (soundBank.ActionEvents ?? [])
                .Where(actionEvent => actionEvent.Actions != null && actionEvent.Actions.Count != 0)
                .ToList();

            return new SoundBank
            {
                Name = soundBank.Name,
                GameSoundBank = soundBank.GameSoundBank,
                DialogueEvents = dialogueEvents.Count != 0 ? dialogueEvents : null,
                ActionEvents = actionEvents.Count != 0 ? actionEvents : null
            };
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
    }
}
