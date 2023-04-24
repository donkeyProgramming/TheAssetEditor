using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using System;
using System.Linq;
using CommunityToolkit.Diagnostics;

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
                .OrderBy(x=>x)
                .ToList();

            var wwiseActorMixer = new CAkActorMixer_v136();
            wwiseActorMixer.Id = project.GetHircItemIdFromName(actorMixer.Name);
            wwiseActorMixer.Type = HircType.ActorMixer;
            wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateDefault();

            wwiseActorMixer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            if (string.IsNullOrEmpty(actorMixer.RootParentId))
            {
                var mixer = project.GetActionMixerParentForActorMixer(actorMixer.Name);
                if (mixer != null)
                    wwiseActorMixer.NodeBaseParams.DirectParentID = project.GetHircItemIdFromName(mixer.Name);

                // If there is a parent, tell the vector to overrwirte it
                wwiseActorMixer.NodeBaseParams.byBitVector = mixer != null ? (byte)0x01 : (byte)0x0;
            }
            else
            {
                wwiseActorMixer.NodeBaseParams.DirectParentID = uint.Parse(actorMixer.RootParentId);
            }
          
            wwiseActorMixer.UpdateSize();
            return wwiseActorMixer;
        }
    }
}
