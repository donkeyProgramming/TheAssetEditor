using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class EventGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(Event);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Event;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkEvent_v136 ConvertToWWise(Event inputEvent, CompilerData project)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = project.GetHircItemIdFromName(inputEvent.Name);
            wwiseEvent.Type = HircType.Event;
            wwiseEvent.Actions = inputEvent.Actions.Select(x => CreateActionFromInputEvent(x, project)).ToList();

            wwiseEvent.UpdateSize();
            return wwiseEvent;
        }

        private static CAkEvent_v136.Action CreateActionFromInputEvent(string actionName, CompilerData project)
        {
            return new CAkEvent_v136.Action() { ActionId = project.GetHircItemIdFromName(actionName) };
        }
    }
}
