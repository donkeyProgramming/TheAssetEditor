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
            HircProcessChildMap.Add(AkBkHircType.SwitchContainer, FindParentSwitchControl);
            HircProcessChildMap.Add(AkBkHircType.LayerContainer, FindParentLayerContainer);
            HircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, FindParentRandomSequenceContainer);
            HircProcessChildMap.Add(AkBkHircType.Sound, FindParentSound);
            HircProcessChildMap.Add(AkBkHircType.ActorMixer, FindParentActorMixer);
            HircProcessChildMap.Add(AkBkHircType.FxCustom, FindParentFxCustom);
            HircProcessChildMap.Add(AkBkHircType.FxShareSet, FindParentFxShareSet);
        }

        private void FindParentLayerContainer(HircItem item, HircTreeNode parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Layer Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(layerContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(layerContainer.GetDirectParentId(), node);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeNode parent)
        {
            var switchContainer = GetAsType<ICAkSwitchCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Switch Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(switchContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(switchContainer.GetDirectParentId(), node);
        }

        private void FindParentRandomSequenceContainer(HircItem item, HircTreeNode parent)
        {
            var randomSequenceContainer = GetAsType<ICAkRanSeqCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Random Sequence Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(randomSequenceContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(randomSequenceContainer.GetDirectParentId(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeNode parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeNode() { DisplayName = $"Actor Mixer {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(actorMixer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetDirectParentId(), node);
        }

        private void FindParentFxShareSet(HircItem item, HircTreeNode parent)
        {
            var node = new HircTreeNode() { DisplayName = $"Fx Share Set {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentFxCustom(HircItem item, HircTreeNode parent)
        {
            var node = new HircTreeNode() { DisplayName = $"Fx Custom {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentSound(HircItem item, HircTreeNode parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeNode() { DisplayName = $"Sound {GetDisplayId(item.Id, item.BnkFilePath, false)} can't have parents", Item = item };
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
