using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    public class CAkEventGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
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
