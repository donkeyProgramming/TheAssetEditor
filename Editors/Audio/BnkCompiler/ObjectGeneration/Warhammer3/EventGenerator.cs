using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc.V136;
using Editors.Audio.BnkCompiler;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.BnkCompiler.ObjectGeneration;
using Shared.GameFormats.Wwise.Hirc;

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

        public static CAkEvent_v136 ConvertToWwise(Event inputEvent, CompilerData project)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = inputEvent.Id;
            wwiseEvent.HircType = HircType.Event;
            wwiseEvent.Actions = inputEvent.Actions.Select(x => CreateActionFromInputEvent(x, project)).ToList();

            wwiseEvent.UpdateSectionSize();
            return wwiseEvent;
        }

        private static CAkEvent_v136.Action CreateActionFromInputEvent(uint actionId, CompilerData project)
        {
            return new CAkEvent_v136.Action() { ActionId = actionId };
        }
    }
}
