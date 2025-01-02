using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Shared.GameFormats.Wwise.Hirc.V136;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class EventGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(Event);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Event;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkEvent_V136 ConvertToWwise(Event inputEvent, CompilerData project)
        {
            var wwiseEvent = new CAkEvent_V136();
            wwiseEvent.Id = inputEvent.Id;
            wwiseEvent.HircType = AkBkHircType.Event;
            wwiseEvent.Actions = inputEvent.Actions.Select(x => CreateActionFromInputEvent(x, project)).ToList();

            wwiseEvent.UpdateSectionSize();
            return wwiseEvent;
        }

        private static CAkEvent_V136.Action_V136 CreateActionFromInputEvent(uint actionId, CompilerData project)
        {
            return new CAkEvent_V136.Action_V136() { ActionId = actionId };
        }
    }
}
