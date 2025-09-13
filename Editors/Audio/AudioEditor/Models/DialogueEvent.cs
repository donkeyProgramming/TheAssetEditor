using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class DialogueEvent : AudioProjectItem
    {
        public List<StatePath> StatePaths { get; set; } = [];

        public DialogueEvent()
        {
            HircType = AkBkHircType.Dialogue_Event;
        }

        public static DialogueEvent Create(string name)
        {
            return new DialogueEvent
            {
                Name = name
            };
        }

        public StatePath GetStatePath(string statePathName)
        {
            return StatePaths.FirstOrDefault(statePath => statePath.Name.Equals(statePathName));
        }

        public void InsertAlphabetically(StatePath statePath) => InsertAlphabeticallyUnique(StatePaths, statePath);
    }
}
