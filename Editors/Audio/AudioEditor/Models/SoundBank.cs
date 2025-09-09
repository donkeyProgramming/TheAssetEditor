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
        public uint LanguageId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public uint DialogueEventsSplitTestingId { get; set; }
        public string DialogueEventsSplitTestingFileName { get; set; }
        public string DialogueEventsSplitTestingFilePath { get; set; }
        public uint DialogueEventsSplitMergingId { get; set; }
        public string DialogueEventsSplitMergingFileName { get; set; }
        public string DialogueEventsSplitMergingFilePath { get; set; }

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

        public List<ActionEvent> GetPlayActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetPlayActions().Count != 0 && actionEvent.GetStopActions().Count == 0)
                .ToList();
        }

        public List<ActionEvent> GetStopActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetStopActions().Count != 0 && actionEvent.GetPlayActions().Count == 0)
                .ToList();
        }
    }
}
