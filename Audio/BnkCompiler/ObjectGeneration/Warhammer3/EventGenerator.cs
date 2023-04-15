using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class EventGenerator
    {
        public CAkEvent_v136 ConvertToWWise(Event inputEvent, HircProjectItemRepository repository)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = repository.GetHircItemId(inputEvent.Id);
            wwiseEvent.Type = HircType.Event;
            wwiseEvent.Actions = new List<CAkEvent_v136.Action>()
            {
                new CAkEvent_v136.Action(){ ActionId = repository.GetHircItemId(inputEvent.Action)}
            };
            wwiseEvent.UpdateSize();
            return wwiseEvent;
        }

        public List<CAkEvent_v136> ConvertToWWise(IEnumerable<Event> inputEvent, HircProjectItemRepository repository)
        {
            return inputEvent.Select(x => ConvertToWWise(x, repository)).ToList();
        }
    }
}
