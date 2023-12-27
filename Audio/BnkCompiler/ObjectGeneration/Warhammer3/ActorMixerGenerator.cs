using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommunityToolkit.Diagnostics;
using System;
using System.Linq;

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
            var allSoundsChildren = actorMixer.Sounds.ToList();
            var allChildren = allActorChildren.Concat(allSoundsChildren);
            var allChildIds = allChildren
                .Select(x => project.GetHircItemIdFromName(x))
                .OrderBy(x => x)
                .ToList();

            var wwiseActorMixer = new CAkActorMixer_v136();
            wwiseActorMixer.Id = project.GetHircItemIdFromName(actorMixer.Name);
            wwiseActorMixer.Type = HircType.ActorMixer;

            var statePropNum_Priority = actorMixer.StatePropNum_Priority;
            var userAuxSendVolume0 = actorMixer.UserAuxSendVolume0;
            var initialDelay = actorMixer.InitialDelay;

            if (statePropNum_Priority != null || userAuxSendVolume0 != null || initialDelay != null)
                wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateCustomMixerParams(actorMixer);
            else
                wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateDefault();

            wwiseActorMixer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            if (string.IsNullOrEmpty(actorMixer.OverrideBusId))
            {
                var mixer = project.GetActionMixerParentForActorMixer(actorMixer.Name);
                if (mixer != null)
                    wwiseActorMixer.NodeBaseParams.OverrideBusId = project.GetHircItemIdFromName(mixer.Name);

                // If there is a parent, tell the vector to overrwirte it
                wwiseActorMixer.NodeBaseParams.byBitVector = mixer != null ? (byte)0x01 : (byte)0x0;
            }
            else
            {
                wwiseActorMixer.NodeBaseParams.OverrideBusId = uint.Parse(actorMixer.OverrideBusId);
            }

            wwiseActorMixer.UpdateSize();
            return wwiseActorMixer;
        }
    }
}
