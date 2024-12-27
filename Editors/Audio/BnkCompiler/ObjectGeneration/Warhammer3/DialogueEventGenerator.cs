using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc.Shared;
using Shared.GameFormats.WWise.Hirc.V136;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class DialogueEventGenerator : IWWiseHircGenerator
    {
        private readonly IAudioRepository _audioRepository;

        public DialogueEventGenerator(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public string GameName => CompilerConstants.GameWarhammer3;
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

            wwiseDialogueEvent.Id = inputDialogueEvent.Id;
            wwiseDialogueEvent.Type = HircType.Dialogue_Event;

            var extractedDialogueEvents = _audioRepository.DialogueEventsWithStateGroups;

            foreach (var stateGroup in extractedDialogueEvents[inputDialogueEvent.Name])
            {
                var argument = new ArgumentList.Argument
                {
                    UlGroupId = WwiseHash.Compute(stateGroup),
                    EGroupType = AkGroupType.State
                };
                wwiseDialogueEvent.CustomArgumentList.Add(argument);
            }

            wwiseDialogueEvent.UTreeDepth = (uint)wwiseDialogueEvent.CustomArgumentList.Count();
            wwiseDialogueEvent.UTreeDataSize = inputDialogueEvent.NodesCount * 12;
            wwiseDialogueEvent.UProbability = 100;

            wwiseDialogueEvent.UMode = (byte)AkMode.BestMatch;

            wwiseDialogueEvent.CustomAkDecisionTree = AkDecisionTree.ReflattenTree(inputDialogueEvent.RootNode, wwiseDialogueEvent.UTreeDepth);

            wwiseDialogueEvent.AkPropBundle0 = 0;
            wwiseDialogueEvent.AkPropBundle1 = 0;

            wwiseDialogueEvent.UpdateSize();
            return wwiseDialogueEvent;
        }
    }
}
