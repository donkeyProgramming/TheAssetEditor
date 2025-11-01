using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public partial class SoundBank : AudioProjectItem
    {
        public string Language { get; set; }
        public uint LanguageId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public uint TestingId { get; set; }
        public string TestingFileName { get; set; }
        public string TestingFilePath { get; set; }
        public uint MergingId { get; set; }
        public string MergingFileName { get; set; }
        public string MergingFilePath { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }
        public List<ActionEvent> ActionEvents { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; }
        public List<Sound> Sounds { get; set; }
        public List<RandomSequenceContainer> RandomSequenceContainers { get; set; }

        public static SoundBank Create(string name, Wh3SoundBank gameSoundBank, string language)
        {
            return new SoundBank
            {
                Id = WwiseHash.Compute(name),
                Name = name,
                GameSoundBank = gameSoundBank,
                Language = language,
                LanguageId = WwiseHash.Compute(language)
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
                .Where(actionEventGroup => allowedActionEventGroups.Contains(actionEventGroup))
                .OrderBy(actionEventGroup => allowedActionEventGroups.IndexOf(actionEventGroup))
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
                .ToList()
                ?? [];
        }

        public Sound GetSound(uint id) => Sounds.FirstOrDefault(sound => sound.Id == id);

        public List<Sound> GetSounds(List<uint> soundReferences)
        {
            var sounds = new List<Sound>();
            foreach (var soundReference in soundReferences)
                sounds.Add(GetSound(soundReference));
            return sounds;
        }

        public RandomSequenceContainer GetRandomSequenceContainer(uint id) => RandomSequenceContainers.FirstOrDefault(randomSequenceContainer => randomSequenceContainer.Id == id);
    }
}
