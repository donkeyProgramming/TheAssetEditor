using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public class HircTreeParentParser : HircTreeBaseParser
    {
        public HircTreeParentParser(IAudioRepository audioRepository) : base(audioRepository)
        {
            HircProcessChildMap.Add(AkBkHircType.SwitchContainer, FindParentSwitchContainer);
            HircProcessChildMap.Add(AkBkHircType.LayerContainer, FindParentBlendContainer);
            HircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, FindParentRandomSequenceContainer);
            HircProcessChildMap.Add(AkBkHircType.Sound, FindParentSound);
            HircProcessChildMap.Add(AkBkHircType.ActorMixer, FindParentActorMixer);
            HircProcessChildMap.Add(AkBkHircType.FxCustom, FindParentFxCustom);
            HircProcessChildMap.Add(AkBkHircType.FxShareSet, FindParentFxShareSet);
        }

        private void FindParentBlendContainer(HircItem item, HircTreeNode parent)
        {
            var blendContainer = GetAsType<ICAkLayerCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Layer Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(blendContainer.GetDirectParentId())}", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(blendContainer.GetDirectParentId(), node);
        }

        private void FindParentSwitchContainer(HircItem item, HircTreeNode parent)
        {
            var switchContainer = GetAsType<ICAkSwitchCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Switch Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(switchContainer.GetDirectParentId())}", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(switchContainer.GetDirectParentId(), node);
        }

        private void FindParentRandomSequenceContainer(HircItem item, HircTreeNode parent)
        {
            var randomSequenceContainer = GetAsType<ICAkRanSeqCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Random Sequence Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(randomSequenceContainer.GetDirectParentId())}", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(randomSequenceContainer.GetDirectParentId(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeNode parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeNode() { DisplayName = $"Actor Mixer {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(actorMixer.GetDirectParentId())}", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetDirectParentId(), node);
        }

        private void FindParentFxShareSet(HircItem item, HircTreeNode parent)
        {
            var node = new HircTreeNode() { DisplayName = $"Fx Share Set {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Hirc = item };
            parent.Children.Add(node);
        }

        private void FindParentFxCustom(HircItem item, HircTreeNode parent)
        {
            var node = new HircTreeNode() { DisplayName = $"Fx Custom {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Hirc = item };
            parent.Children.Add(node);
        }

        private void FindParentSound(HircItem item, HircTreeNode parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeNode() { DisplayName = $"Sound {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(sound.GetDirectParentId(), node);
        }

        protected override string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = AudioRepository.GetNameFromId(id, out var found);
            if (found)
                return $"'{name}' with Id[{id}] in {fileName}";
            else
                return $"with Id[{id}] in {fileName}";
        }

        string GetParentInfo(uint id)
        {
            var name = AudioRepository.GetNameFromId(id, out var found);
            if (found)
                return $"has parent '{name}' with Id[{id}]";
            else
                return $"has parent with Id[{id}]";
        }
    }
}
