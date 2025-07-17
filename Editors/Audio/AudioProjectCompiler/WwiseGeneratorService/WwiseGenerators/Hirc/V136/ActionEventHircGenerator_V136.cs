using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class ActionEventHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectActionEvent = audioProjectItem as ActionEvent;
            var actionEventHirc = CreateActionEvent(audioProjectActionEvent);
            actionEventHirc.UpdateSectionSize();
            return actionEventHirc;
        }

        private static CAkEvent_V136 CreateActionEvent(ActionEvent audioProjectActionEvent)
        {
            return new CAkEvent_V136()
            {
                Id = audioProjectActionEvent.Id,
                HircType = audioProjectActionEvent.HircType,
                Actions = audioProjectActionEvent.Actions.Select(action => CreateAction(action.Id)).OrderBy(action => action.ActionId).ToList()
            };
        }

        private static CAkEvent_V136.Action_V136 CreateAction(uint actionId)
        {
            return new CAkEvent_V136.Action_V136() { ActionId = actionId };
        }
    }
}
