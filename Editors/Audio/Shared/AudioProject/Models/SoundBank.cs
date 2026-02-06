using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class SoundBank : AudioProjectItem
    {
        public string Language { get; set; }
        [JsonIgnore] public uint LanguageId { get; set; }
        [JsonIgnore] public string FileName { get; set; }
        [JsonIgnore] public string FilePath { get; set; }
        [JsonIgnore] public uint TestingId { get; set; }
        [JsonIgnore] public string TestingFileName { get; set; }
        [JsonIgnore] public string TestingFilePath { get; set; }
        [JsonIgnore] public uint MergingId { get; set; }
        [JsonIgnore] public string MergingFileName { get; set; }
        [JsonIgnore] public string MergingFilePath { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; } = [];
        public List<ActionEvent> ActionEvents { get; set; } = [];
        public List<Sound> Sounds { get; set; } = [];
        public List<RandomSequenceContainer> RandomSequenceContainers { get; set; } = [];

        public SoundBank(string name, Wh3SoundBank gameSoundBank, string language)
        {
            Id = WwiseHash.Compute(name);
            Name = name;
            GameSoundBank = gameSoundBank;
            Language = language;
            LanguageId = WwiseHash.Compute(language);
        }

        public SoundBank Clean()
        {
            var cleanedDialogueEvents = DialogueEvents
                .Where(dialogueEvent =>
                    dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            var cleanedActionEvents = ActionEvents
                .Where(actionEvent => actionEvent.Actions.Count != 0)
                .ToList();

            if (cleanedDialogueEvents.Count == 0 && cleanedActionEvents.Count == 0)
                return null;

            return new SoundBank(Name, GameSoundBank, Language)
            {
                FileName = FileName,
                FilePath = FilePath,
                TestingId = TestingId,
                TestingFileName = TestingFileName,
                TestingFilePath = TestingFilePath,
                MergingId = MergingId,
                MergingFileName = MergingFileName,
                MergingFilePath = MergingFilePath,
                DialogueEvents = cleanedDialogueEvents,
                ActionEvents = cleanedActionEvents,
                Sounds = Sounds.ToList(),
                RandomSequenceContainers = RandomSequenceContainers.ToList()
            };
        }

        public ActionEvent GetActionEvent(string actionEventName) => ActionEvents.FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);

        public List<Wh3ActionEventType> GetUsedActionEventTypes()
        {
            var usedActionEventGroups = ActionEvents
                .Select(actionEventGroup => actionEventGroup.ActionEventType)
                .Distinct();

            var allowedActionEventGroups = Wh3ActionEventInformation
                .GetSoundBankActionEventTypes(GameSoundBank);

            return usedActionEventGroups
                .Where(allowedActionEventGroups.Contains)
                .OrderBy(allowedActionEventGroups.IndexOf)
                .ToList();
        }

        public List<DialogueEvent> GetEditedDialogueEvents()
        {
            return DialogueEvents
                .Where(dialogueEvent => dialogueEvent.StatePaths.Count != 0)
                .ToList();
        }

        public List<ActionEvent> GetPlayActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetPlayActions().Count != 0)
                .ToList();
        }

        public Sound GetSound(uint id) => Sounds.FirstOrDefault(sound => sound.Id == id);

        public List<Sound> GetSounds(List<uint> soundReferences)
        {
            var sounds = new List<Sound>();
            foreach (var soundReference in soundReferences)
                sounds.Add(GetSound(soundReference));
            return sounds;
        }

        public RandomSequenceContainer GetRandomSequenceContainer(uint id)
        {
            return RandomSequenceContainers.FirstOrDefault(randomSequenceContainer => randomSequenceContainer.Id == id);
        }
    }

    public static class SoundBankListExtensions
    {
        public static void TryAdd(this List<SoundBank> existingSoundBanks, SoundBank soundBank)
        {
            ArgumentNullException.ThrowIfNull(existingSoundBanks);
            ArgumentNullException.ThrowIfNull(soundBank);

            if (existingSoundBanks.Any(existingSoundBank => existingSoundBank.Id == soundBank.Id))
                throw new ArgumentException($"Cannot add SoundBank with Id {soundBank.Id} as it already exists.");

            var index = existingSoundBanks.BinarySearch(soundBank, AudioProjectItem.IdComparer);
            if (index < 0)
                index = ~index;

            existingSoundBanks.Insert(index, soundBank);
        }
    }
}
