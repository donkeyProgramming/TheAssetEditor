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

        public Action(uint id, AkBkHircType targetHircType, AkActionType actionType, uint idExt, uint bankId)
        {
            Id = id;
            HircType = AkBkHircType.Action;
            TargetHircId = idExt;
            TargetHircType = targetHircType;
            ActionType = actionType;
            IdExt = idExt;
            BankId = bankId;
        }

        public static Action CreatePlay(uint id, AkBkHircType targetHircType, uint idExt, uint bankId)
        {
            return new Action(id, targetHircType, AkActionType.Play, idExt, bankId);
        }

        public static Action CreatePauseFromSource(uint id, Action source) => CreateFromSource(id, source, AkActionType.Pause_E_O);

        public static Action CreateResumeFromSource(uint id, Action source) => CreateFromSource(id, source, AkActionType.Resume_E_O);

        public static Action CreateStopFromSource(uint id, Action source) => CreateFromSource(id, source, AkActionType.Stop_E_O);

        private static Action CreateFromSource(uint id, Action source, AkActionType actionType)
        {
            return new Action(id, source.TargetHircType, actionType, source.IdExt, source.BankId);
        }

        public bool TargetHircTypeIsSound() => TargetHircType == AkBkHircType.Sound;

        public bool TargetHircTypeIsRandomSequenceContainer() => TargetHircType == AkBkHircType.RandomSequenceContainer;
    }
}
