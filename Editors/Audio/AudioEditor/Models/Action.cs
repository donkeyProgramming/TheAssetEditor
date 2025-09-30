using Editors.Audio.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class Action : AudioProjectItem
    {
        public Sound Sound { get; set; }
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
        public AkActionType ActionType { get; set; }
        public uint IdExt { get; set; }
        public uint BankId { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }

        public Action()
        {
            HircType = AkBkHircType.Action;
        }

        public static Action Create(uint id, string name, Sound sound, AkActionType actionType)
        {
            return new Action
            {
                Id = id,
                Name = name,
                Sound = sound,
                ActionType = actionType
            };
        }

        public static Action Create(uint id, string name, RandomSequenceContainer randomSequenceContainer, AkActionType actionType)
        {
            return new Action
            {
                Id = id,
                Name = name,
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
