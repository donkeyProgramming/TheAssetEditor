using Audio.FileFormats.WWise.Hirc.V136;
using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System;
using System.Linq;
using CommunityToolkit.Diagnostics;

namespace Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActorMixerGenerator : IWWiseHircGenerator
    {
        public string GameName => CompilerConstants.Game_Warhammer3;
        public Type AudioProjectType => typeof(ActorMixer);

        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, AudioInputProject project, HircProjectItemRepository repository)
        {
            var typedProjectItem = projectItem as ActorMixer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWWise(typedProjectItem, repository);
        }

        public CAkActorMixer_v136 ConvertToWWise(ActorMixer actorMixer, HircProjectItemRepository repository)
        {
            var allActorChildren = actorMixer.ActorMixerChildren.ToList();
            var allSoundsChildren = actorMixer.Sounds.ToList();
            var allChildren = allActorChildren.Concat(allSoundsChildren);
            var allChildIds = allChildren
                .Select(x => repository.GetHircItemId(x))
                .OrderBy(x=>x)
                .ToList();

            var wwiseActorMixer = new CAkActorMixer_v136();
            wwiseActorMixer.Id = repository.GetHircItemId(actorMixer.Id);
            wwiseActorMixer.Type = HircType.ActorMixer;
            wwiseActorMixer.NodeBaseParams = NodeBaseParams.CreateDefault();

            wwiseActorMixer.Children = new Children()
            {
                ChildIdList = allChildIds
            };

            var mixer = repository.GetActionMixerParentForActorMixer(actorMixer.Id);
            if (mixer != null)
                wwiseActorMixer.NodeBaseParams.DirectParentID = repository.GetHircItemId(mixer.Id);

            // If there is a parent, tell the vector to overrwirte it
            wwiseActorMixer.NodeBaseParams.byBitVector = mixer != null ? (byte)0x01 : (byte)0x0;

            wwiseActorMixer.UpdateSize();
            return wwiseActorMixer;
        }
    }
}
