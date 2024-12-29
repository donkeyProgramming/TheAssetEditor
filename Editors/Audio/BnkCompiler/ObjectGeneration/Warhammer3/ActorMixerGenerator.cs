using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc.V136;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActorMixerGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(ActorMixer);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as ActorMixer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkActorMixer_v136 ConvertToWwise(ActorMixer actorMixer, CompilerData project)
        {
            var wwiseActorMixer = new CAkActorMixer_v136();
            wwiseActorMixer.Id = actorMixer.Id;
            wwiseActorMixer.Type = HircType.ActorMixer;
            wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateDefault();
            wwiseActorMixer.NodeBaseParams.DirectParentId = actorMixer.DirectParentId;

            var allChildIds = actorMixer.Children.ToList();

            wwiseActorMixer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            wwiseActorMixer.UpdateSize();

            return wwiseActorMixer;
        }
    }
}
