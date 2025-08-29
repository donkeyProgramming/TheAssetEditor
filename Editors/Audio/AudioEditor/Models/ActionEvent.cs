using System.Collections.Generic;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class ActionEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.Event;
        // TODO: Refactor Actions to include the random sequence contains and sound
        // Actions is a List because in Wwise an Action Event can have multiple actions but making multiple Actions
        // for an ActionEvent isn't supported by the tool as it's unlikely to be used. If it were supported Actions
        // should be refactored to contain the Sound / RandomSequenceContainer rather than the ActionEvent as each
        // Action would then target its own audio.
        public List<Action> Actions { get; set; }
        public Sound Sound { get; set; }
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public Wh3ActionEventType ActionEventType { get; set; }

        public static ActionEvent Create(string name, Sound sound, Wh3ActionEventType actionEventType)
        {
            return new ActionEvent
            {
                Name = name,
                Sound = sound,
                ActionEventType = actionEventType,
            };
        }

        public static ActionEvent Create(string name, RandomSequenceContainer randomSequenceContainer, Wh3ActionEventType actionEventGroup)
        {
            return new ActionEvent
            {
                Name = name,
                RandomSequenceContainer = randomSequenceContainer,
                ActionEventType = actionEventGroup,
            };
        }

        public AudioSettings GetAudioSettings()
        {
            if (RandomSequenceContainer != null)
                return RandomSequenceContainer.AudioSettings;
            else
                return Sound.AudioSettings;
        }
    }
}
