using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CommunityToolkit.Diagnostics;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class EventGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(Event);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, AudioInputProject project, HircProjectItemRepository repository)
        {
            var typedProjectItem = projectItem as Event;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, repository);
        }

        public CAkEvent_v136 ConvertToWWise(Event inputEvent, HircProjectItemRepository repository)
        {
            var wwiseEvent = new CAkEvent_v136();
            wwiseEvent.Id = repository.GetHircItemId(inputEvent.Id);
            wwiseEvent.Type = HircType.Event;
            wwiseEvent.Actions = inputEvent.Actions.Select(x => CreateActionFromInputEvent(x, repository)).ToList();

            wwiseEvent.UpdateSize();
            return wwiseEvent;
        }

        private static CAkEvent_v136.Action CreateActionFromInputEvent(string actionId, HircProjectItemRepository repository)
        {
            return new CAkEvent_v136.Action() { ActionId = repository.GetHircItemId(actionId) };
        }
    }
}
