using System.Collections.Generic;
using System.Linq;
using Editors.Audio.GameInformation.Warhammer3;

namespace Editors.Audio.AudioEditor.Models
{
    public partial class SoundBank : AudioProjectItem
    {
        public Wh3SoundBank GameSoundBank { get; set; }
        public List<ActionEvent> ActionEvents { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; }
        public string Language { get; set; }
        public string SoundBankFileName { get; set; }
        public string SoundBankFilePath { get; set; }

        public static SoundBank Create(string name, Wh3SoundBank gameSoundBank)
        {
            return new SoundBank
            {
                Name = name,
                GameSoundBank = gameSoundBank
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

        public void InsertAlphabetically(ActionEvent actionEvent) => InsertAlphabeticallyUnique(ActionEvents, actionEvent);
    }
}
