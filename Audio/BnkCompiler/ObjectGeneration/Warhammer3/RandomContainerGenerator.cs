using Audio.BnkCompiler;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;

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
            wwiseRandomContainer.NodeBaseParams = NodeBaseParams.CreateDefault();

            var mixer = project.GetActionMixerForObject(inputContainer.Name);
            if (mixer != null)
                wwiseRandomContainer.NodeBaseParams.DirectParentID = project.GetHircItemIdFromName(mixer.Name);

            wwiseRandomContainer.sLoopCount = 1;
            wwiseRandomContainer.wAvoidRepeatCount = 1;

            var allChildIds = inputContainer.Children
            .Select(x => project.GetHircItemIdFromName(x))
            .OrderBy(x => x)
            .ToList();

            wwiseRandomContainer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            var akPlaylistItem = new AkPlaylistItem();
            foreach (var childId in allChildIds)
            {
                akPlaylistItem.PlayId = childId;
                akPlaylistItem.Weight = 50000;
                wwiseRandomContainer.AkPlaylist.Add(akPlaylistItem);
            }

            wwiseRandomContainer.UpdateSize();
            return wwiseRandomContainer;
        }
    }
}
