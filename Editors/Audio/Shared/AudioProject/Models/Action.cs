using System.Text.Json.Serialization;
using Shared.GameFormats.Wwise;
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

        /// <summary>Only meaningful for SetState actions, where it names the State Group the
        /// target State belongs to. Left out of the JSON otherwise.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint StateGroupId { get; set; }

        /// <summary>The names the SetState ids were hashed from, kept so the editor can show what
        /// the action does without having to reverse a Wwise hash.</summary>
        public string StateGroupName { get; set; }
        public string StateName { get; set; }

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

        /// <summary>
        /// A SetState action, which is how vanilla drives music: the culture music events hold one
        /// of these and nothing else - no Play action, no Sound, no bank reference. The state id
        /// goes in IdExt as well as in the state params, which is what every vanilla music action
        /// does.
        /// </summary>
        public static Action CreateSetState(uint id, string stateGroupName, string stateName)
        {
            return new Action(id, AkBkHircType.State, AkActionType.SetState, WwiseHash.Compute(stateName), bankId: 0)
            {
                StateGroupId = WwiseHash.Compute(stateGroupName),
                StateGroupName = stateGroupName,
                StateName = stateName
            };
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
