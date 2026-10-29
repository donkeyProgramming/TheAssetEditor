using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using static Shared.GameFormats.Wwise.Hirc.V136.Shared.AkPropBundle_V136;
using Action = Editors.Audio.Shared.AudioProject.Models.Action;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    public class ActionHircGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
        {
            var audioProjectActionEvent = audioProjectItem as Action;

            var actionHirc = CreateAction(audioProjectActionEvent, soundBank.GameSoundBank);

            if (audioProjectActionEvent.ActionType == AkActionType.Play)
                actionHirc.PlayActionParams = CreatePlayActionParams(audioProjectActionEvent.BankId);
            else if (audioProjectActionEvent.ActionType == AkActionType.Pause_E_O)
                actionHirc.ActiveActionParams = CreatePauseActionParams();
            else if (audioProjectActionEvent.ActionType == AkActionType.Resume_E_O)
                actionHirc.ActiveActionParams = CreateResumeActionParams();
            else if (audioProjectActionEvent.ActionType == AkActionType.Stop_E_O)
                actionHirc.ActiveActionParams = CreateStopActionParams();
            else if (audioProjectActionEvent.ActionType == AkActionType.SetState)
                actionHirc.StateActionParams = CreateStateActionParams(audioProjectActionEvent.StateGroupId, audioProjectActionEvent.IdExt);

            actionHirc.UpdateSectionSize();

            return actionHirc;
        }

        private static CAkAction_V136 CreateAction(Action audioProjectActionEvent, Wh3SoundBank soundBankSubType)
        {
            var action = new CAkAction_V136
            {
                Id = audioProjectActionEvent.Id,
                HircType = audioProjectActionEvent.HircType,
                ActionType = audioProjectActionEvent.ActionType,
                IdExt = audioProjectActionEvent.IdExt
            };

            // Vanilla SetState actions carry no properties at all - every culture music action in
            // the shipped banks has both prop bundles empty - so they stay out of the global music
            // transition-time case below.
            if (soundBankSubType == Wh3SoundBank.GlobalMusic && audioProjectActionEvent.ActionType != AkActionType.SetState)
            {
                action.AkPropBundle0.PropsList.Add(new PropBundleInstance_V136
                {
                    Id = AkPropId_V136.TransitionTime,
                    Value = 1000
                });
            }

            return action;
        }

        private static CAkAction_V136.PlayActionParams_V136 CreatePlayActionParams(uint bankId)
        {
            return new CAkAction_V136.PlayActionParams_V136
            {
                BitVector = 4,
                BankId = bankId
            };
        }

        private static CAkAction_V136.StateActionParams_V136 CreateStateActionParams(uint stateGroupId, uint stateId)
        {
            return new CAkAction_V136.StateActionParams_V136
            {
                StateGroupId = stateGroupId,
                TargetStateId = stateId
            };
        }

        private static CAkAction_V136.ActiveActionParams_V136 CreatePauseActionParams()
        {
            return new CAkAction_V136.ActiveActionParams_V136
            {
                BitVector = 4,
                PauseActionSpecificParams = new CAkAction_V136.ActiveActionParams_V136.PauseActionSpecificParams_V136
                {
                    BitVector = 7
                },
                ExceptParams = new CAkAction_V136.ActiveActionParams_V136.ExceptParams_V136
                {
                    ExceptionListSize = 0
                }
            };
        }

        private static CAkAction_V136.ActiveActionParams_V136 CreateResumeActionParams()
        {
            return new CAkAction_V136.ActiveActionParams_V136
            {
                BitVector = 0,
                ResumeActionSpecificParams = new CAkAction_V136.ActiveActionParams_V136.ResumeActionSpecificParams_V136
                {
                    BitVector = 7
                },
                ExceptParams = new CAkAction_V136.ActiveActionParams_V136.ExceptParams_V136
                {
                    ExceptionListSize = 0
                }
            };
        }

        private static CAkAction_V136.ActiveActionParams_V136 CreateStopActionParams()
        {
            return new CAkAction_V136.ActiveActionParams_V136
            {
                BitVector = 4,
                StopActionSpecificParams = new CAkAction_V136.ActiveActionParams_V136.StopActionSpecificParams_V136
                {
                    BitVector = 6
                },
                ExceptParams = new CAkAction_V136.ActiveActionParams_V136.ExceptParams_V136
                {
                    ExceptionListSize = 0
                }
            };
        }
    }
}
