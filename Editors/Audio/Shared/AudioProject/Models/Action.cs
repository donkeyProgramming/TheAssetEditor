using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class Action : AudioProjectItem
    {
        public uint TargetHircId { get; set; }
        public AkBkHircType TargetHircType { get; set; }
        public AkActionType ActionType { get; set; }
        public uint IdExt { get; set; }
        public uint BankId { get; set; }

        public Action()
        {
            HircType = AkBkHircType.Action;
        }

        public static Action Create(uint id, AkBkHircType targetHircType, AkActionType actionType, uint idExt, uint bankId)
        {
            return new Action
            {
                Id = id,
                TargetHircId = idExt,
                TargetHircType = targetHircType,
                ActionType = actionType,
                IdExt = idExt,
                BankId = bankId
            };
        }

        public bool TargetHircTypeIsSound()
        {
            if (TargetHircType == AkBkHircType.Sound)
                return true;
            else
                return false;
        }

        public bool TargetHircTypeIsRandomSequenceContainer()
        {
            if (TargetHircType == AkBkHircType.RandomSequenceContainer)
                return true;
            else
                return false;
        }
    }
}
