using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class DialogueEventGenerator : IWwiseHircGenerator
    {
        private readonly IAudioRepository _audioRepository;

        public DialogueEventGenerator(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(DialogueEvent);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as DialogueEvent;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public CAkDialogueEvent_v136 ConvertToWwise(DialogueEvent inputDialogueEvent, CompilerData project)
        {
            var wwiseDialogueEvent = new CAkDialogueEvent_v136();
            wwiseDialogueEvent.Arguments = new List<AkGameSync_V136>();
            wwiseDialogueEvent.Id = inputDialogueEvent.Id;
            wwiseDialogueEvent.HircType = AkBkHircType.Dialogue_Event;

            var extractedDialogueEvents = _audioRepository.DialogueEventsWithStateGroups;
            foreach (var stateGroup in extractedDialogueEvents[inputDialogueEvent.Name])
            {
                var argument = new AkGameSync_V136
                {
                    GroupId = WwiseHash.Compute(stateGroup),
                    GroupType = AkGroupType.State
                };
                wwiseDialogueEvent.Arguments.Add(argument);
            }

            wwiseDialogueEvent.TreeDepth = (uint)wwiseDialogueEvent.Arguments.Count();
            wwiseDialogueEvent.TreeDataSize = inputDialogueEvent.NodesCount * 12;
            wwiseDialogueEvent.Probability = 100;
            wwiseDialogueEvent.Mode = (byte)AkMode.BestMatch;
            wwiseDialogueEvent.AkDecisionTree.Nodes = AkDecisionTree_V136.TraverseAndFlatten(inputDialogueEvent.RootNode);
            wwiseDialogueEvent.AkPropBundle0 = new AkPropBundle_V136()
            {
                PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>()
            };
            wwiseDialogueEvent.AkPropBundle1 = new AkPropBundleMinMax_V136()
            {
                Values = new List<AkPropBundleMinMax_V136.AkPropBundleInstance_V136>()
            };
            wwiseDialogueEvent.UpdateSectionSize();
            return wwiseDialogueEvent;
        }
    }
}
