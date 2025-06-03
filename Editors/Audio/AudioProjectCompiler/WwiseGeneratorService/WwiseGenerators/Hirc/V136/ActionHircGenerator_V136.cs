using Editors.Audio.AudioEditor;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using static Shared.GameFormats.Wwise.Hirc.V136.Shared.AkPropBundle_V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class ActionHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectActionEvent = audioProjectItem as Action;

            var actionHirc = CreateAction(audioProjectActionEvent, soundBank.SoundBankSubtype);

            if (audioProjectActionEvent.ActionType == AkActionType.Play)
                actionHirc.PlayActionParams = CreatePlayActionParams(soundBank);
            else if (audioProjectActionEvent.ActionType == AkActionType.Stop_E_O)
                actionHirc.ActiveActionParams = CreateStopActionParams(soundBank);

            actionHirc.UpdateSectionSize();

            return actionHirc;
        }

        private static CAkAction_V136 CreateAction(Action audioProjectActionEvent, Wh3SoundBankSubtype soundBankSubType)
        {
            var action = new CAkAction_V136
            {
                Id = audioProjectActionEvent.Id,
                HircType = audioProjectActionEvent.HircType,
                ActionType = audioProjectActionEvent.ActionType,
                IdExt = audioProjectActionEvent.IdExt
            };

            if (soundBankSubType == Wh3SoundBankSubtype.FrontendMusic)
            {
                action.AkPropBundle0.PropsList.Add(new PropBundleInstance_V136
                {
                    Id = AkPropId_V136.TransitionTime,
                    Value = 1000
                });
            }

            return action;
        }

        private static CAkAction_V136.PlayActionParams_V136 CreatePlayActionParams(SoundBank soundBank)
        {
            return new CAkAction_V136.PlayActionParams_V136
            {
                BitVector = 4,
                BankId = soundBank.Id
            };
        }

        private static CAkAction_V136.ActiveActionParams_V136 CreateStopActionParams(SoundBank soundBank)
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
