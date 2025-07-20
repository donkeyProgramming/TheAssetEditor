using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class ActionEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.Event;
        public List<Action> Actions { get; set; }
        // Actions technically should contain the Sound / RandomSequenceContainer rather than the ActionEvent but making multiple Actions for an ActionEvent isn't currently supported by the tool so not needed.
        public Sound Sound { get; set; }
        public RandomSequenceContainer RandomSequenceContainer { get; set; }

        public static ActionEvent Create(string name, Sound sound)
        {
            return new ActionEvent
            {
                Name = name,
                Sound = sound
            };
        }

        public static ActionEvent Create(string name, RandomSequenceContainer randomSequenceContainer)
        {
            return new ActionEvent
            {
                Name = name,
                RandomSequenceContainer = randomSequenceContainer
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
