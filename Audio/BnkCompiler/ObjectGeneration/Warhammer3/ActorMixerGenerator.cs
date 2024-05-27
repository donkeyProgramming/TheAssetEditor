using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActorMixerGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(ActorMixer);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as ActorMixer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkActorMixer_v136 ConvertToWWise(ActorMixer actorMixer, CompilerData project)
        {
            var allActorChildren = actorMixer.ActorMixerChildren.ToList();
            var allSoundsChildren = actorMixer.Children.ToList();
            var allChildren = allActorChildren.Concat(allSoundsChildren);
            var allChildIds = allChildren
                .Select(x => project.GetHircItemIdFromName(x))
                .OrderBy(x => x)
                .ToList();

            var wwiseActorMixer = new CAkActorMixer_v136();
            wwiseActorMixer.Id = project.GetHircItemIdFromName(actorMixer.Name);
            wwiseActorMixer.Type = HircType.ActorMixer;
            wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateDefault();
            wwiseActorMixer.NodeBaseParams.DirectParentId = actorMixer.DirectParentId;

            wwiseActorMixer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            wwiseActorMixer.UpdateSize();
            return wwiseActorMixer;
        }
    }
}
