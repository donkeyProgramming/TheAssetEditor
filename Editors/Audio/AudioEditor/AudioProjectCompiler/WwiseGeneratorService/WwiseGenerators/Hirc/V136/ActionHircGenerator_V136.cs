using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class ActionHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectActionEvent = audioProjectItem as Action;

            var actionHirc = CreateAction(audioProjectActionEvent);

            if (audioProjectActionEvent.ActionType == AkActionType.Play)
                actionHirc.PlayActionParams = CreatePlayActionParams(soundBank);

            actionHirc.UpdateSectionSize();

            return actionHirc;
        }

        private static CAkAction_V136 CreateAction(Action audioProjectActionEvent)
        {
            return new CAkAction_V136
            {
                ID = audioProjectActionEvent.ID,
                HircType = audioProjectActionEvent.HircType,
                ActionType = audioProjectActionEvent.ActionType,
                IdExt = audioProjectActionEvent.IDExt
            };
        }

        private static CAkAction_V136.PlayActionParams_V136 CreatePlayActionParams(SoundBank soundBank)
        {
            return new CAkAction_V136.PlayActionParams_V136
            {
                BitVector = 0x04,
                BankId = WwiseHash.Compute(soundBank.Name)
            };
        }
    }
}
