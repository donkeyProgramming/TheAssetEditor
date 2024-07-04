using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc.V136;


namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class RandomContainerGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(RandomContainer);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as RandomContainer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkRanSeqCntr_v136 ConvertToWWise(RandomContainer inputContainer, CompilerData project)
        {
            var wwiseRandomContainer = new CAkRanSeqCntr_v136();
            wwiseRandomContainer.Id = project.GetHircItemIdFromName(inputContainer.Name);
            wwiseRandomContainer.Type = HircType.SequenceContainer;
            wwiseRandomContainer.NodeBaseParams = NodeBaseParams.CreateDefaultRandomContainer();
            wwiseRandomContainer.byBitVector = 0x12;
            wwiseRandomContainer.fTransitionTime = 1000;

            var mixer = project.GetActorMixerForObject(inputContainer.Name);
            if (mixer != null)
                wwiseRandomContainer.NodeBaseParams.DirectParentId = project.GetHircItemIdFromName(inputContainer.DirectParentId);

            wwiseRandomContainer.sLoopCount = 1;
            wwiseRandomContainer.wAvoidRepeatCount = 2;

            var allChildIds = inputContainer.Children
            .Select(x => project.GetHircItemIdFromName(x))
            .OrderBy(x => x)
            .ToList();

            wwiseRandomContainer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            foreach (var childId in allChildIds)
            {
                var akPlaylistItem = new AkPlaylistItem
                {
                    PlayId = childId,
                    Weight = 50000
                };
                wwiseRandomContainer.AkPlaylist.Add(akPlaylistItem);
            }

            wwiseRandomContainer.UpdateSize();
            return wwiseRandomContainer;
        }
    }
}
