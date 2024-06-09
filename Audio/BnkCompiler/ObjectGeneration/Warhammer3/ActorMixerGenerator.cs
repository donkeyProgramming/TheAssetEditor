using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc.V136;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActorMixerGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(ActorMixer);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as ActorMixer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, project);
        }

        public CAkActorMixer_v136 ConvertToWWise(ActorMixer actorMixer, CompilerData project)
        {
            var allChildIds = actorMixer.Children.ToList();

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
