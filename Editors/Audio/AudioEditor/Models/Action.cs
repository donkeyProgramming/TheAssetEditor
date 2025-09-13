using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class Action : AudioProjectItem
    {
        public Sound Sound { get; set; }
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public AkActionType ActionType { get; set; }
        public uint IdExt { get; set; }

        public Action()
        {
            HircType = AkBkHircType.Action;
        }

        public static Action Create(Sound sound, AkActionType actionType)
        {
            return new Action
            {
                Sound = sound,
                ActionType = actionType
            };
        }

        public static Action Create(RandomSequenceContainer randomSequenceContainer, AkActionType actionType)
        {
            return new Action
            {
                RandomSequenceContainer = randomSequenceContainer,
                ActionType = actionType
            };
        }

        public AudioSettings GetAudioSettings()
        {
            if (Sound != null)
                return Sound.AudioSettings;
            else
                return RandomSequenceContainer.AudioSettings;
        }
    }
}
