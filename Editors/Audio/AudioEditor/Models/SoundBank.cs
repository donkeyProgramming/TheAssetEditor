using System.Collections.Generic;
using System.Linq;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Models
{
    public partial class SoundBank : AudioProjectItem
    {
        public Wh3SoundBankType SoundBankType { get; set; }
        public Wh3SoundBankSubtype SoundBankSubtype { get; set; }
        public List<ActionEvent> ActionEvents { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; }
        public string Language { get; set; }
        public string SoundBankFileName { get; set; }
        public string SoundBankFilePath { get; set; }

        public static SoundBank Create(string name, Wh3SoundBankType type)
        {
            return new SoundBank
            {
                Name = name,
                SoundBankType = type
            };
        }

        public ActionEvent GetActionEvent(string actionEventName)
        {
            return ActionEvents.FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);
        }

        public List<DialogueEvent> GetEditedDialogueEvents()
        {
            return DialogueEvents.Where(dialogueEvent => dialogueEvent.StatePaths.Count != 0).ToList();
        }

        public void InsertAlphabetically(ActionEvent actionEvent) => InsertAlphabeticallyUnique(ActionEvents, actionEvent);
    }
}
