using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class ActionEventHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectActionEvent = audioProjectItem as ActionEvent;

            var actionEventHirc = new CAkEvent_V136()
            {
                ID = audioProjectActionEvent.ID,
                HircType = audioProjectActionEvent.HircType,
                Actions = audioProjectActionEvent.Actions.Select(action => CreateAction(action.ID)).OrderBy(action => action.ActionID).ToList()
            };
            
            actionEventHirc.UpdateSectionSize();

            return actionEventHirc;
        }

        private static CAkEvent_V136.Action_V136 CreateAction(uint actionId)
        {
            return new CAkEvent_V136.Action_V136() { ActionID = actionId };
        }
    }
}
