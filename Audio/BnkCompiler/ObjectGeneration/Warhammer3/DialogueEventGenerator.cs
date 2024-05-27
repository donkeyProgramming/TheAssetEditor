using Audio.FileFormats.WWise.Hirc.V136;
using System;
using Audio.FileFormats.WWise;
using CommunityToolkit.Diagnostics;
using Audio.FileFormats.WWise.Hirc.Shared;
using System.Linq;
using System.Collections.Generic;
using Audio.Utility;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class DialogueEventGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(DialogueEvent);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as DialogueEvent;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkDialogueEvent_v136 ConvertToWWise(DialogueEvent inputDialogueEvent, CompilerData project)
        {
            var wwiseDialogueEvent = new CAkDialogueEvent_v136();
            wwiseDialogueEvent.CustomArgumentList = new List<ArgumentList.Argument>();

            wwiseDialogueEvent.Id = project.GetHircItemIdFromName(inputDialogueEvent.Name);
            wwiseDialogueEvent.Type = HircType.Dialogue_Event;

            var ExtractedDialogueEvents = DialogueEventData.ExtractedDialogueEvents;

            foreach (var stateGroup in ExtractedDialogueEvents[inputDialogueEvent.Name])
            {
                var argument = new ArgumentList.Argument
                {
                    ulGroupId = WWiseHash.Compute(stateGroup),
                    eGroupType = AkGroupType.State
                };
                wwiseDialogueEvent.CustomArgumentList.Add(argument);
            }

            wwiseDialogueEvent.uTreeDepth = (uint)wwiseDialogueEvent.CustomArgumentList.Count();
            wwiseDialogueEvent.uTreeDataSize = inputDialogueEvent.NodesCount * 12;
            wwiseDialogueEvent.uProbability = 100;

            wwiseDialogueEvent.uMode = (byte)AkMode.BestMatch;

            wwiseDialogueEvent.CustomAkDecisionTree = AkDecisionTree.ReflattenTree(inputDialogueEvent.RootNode, wwiseDialogueEvent.uTreeDepth);

            wwiseDialogueEvent.AkPropBundle0 = 0;
            wwiseDialogueEvent.AkPropBundle1 = 0;

            wwiseDialogueEvent.UpdateSize();
            return wwiseDialogueEvent;
        }
    }
}
