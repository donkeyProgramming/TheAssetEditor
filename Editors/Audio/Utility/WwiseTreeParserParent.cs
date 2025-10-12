using Editors.Audio.AudioExplorer;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Utility
{
    public class WwiseTreeParserParent : WwiseTreeParserBase
    {
        public WwiseTreeParserParent(IAudioRepository audioRepository) : base(audioRepository)
        {
            _hircProcessChildMap.Add(AkBkHircType.SwitchContainer, FindParentSwitchControl);
            _hircProcessChildMap.Add(AkBkHircType.LayerContainer, FindParentLayerContainer);
            _hircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, FindParentRandContainer);
            _hircProcessChildMap.Add(AkBkHircType.Sound, FindParentSound);
            _hircProcessChildMap.Add(AkBkHircType.ActorMixer, FindParentActorMixer);
            _hircProcessChildMap.Add(AkBkHircType.FxCustom, FindParentFxCustom);
            _hircProcessChildMap.Add(AkBkHircType.FxShareSet, FindParentFxShareSet);
        }

        private void FindParentLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(layerContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(layerNode);
            ProcessNext(layerContainer.GetDirectParentId(), layerNode);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeItem parent)
        {
            var wwiseObject = GetAsType<ICAkSwitchCntr>(item);
            var switchNode = new HircTreeItem() { DisplayName = $"Switch Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(wwiseObject.GetDirectParentId())}", Item = item };
            parent.Children.Add(switchNode);
            ProcessNext(wwiseObject.GetDirectParentId(), switchNode);
        }

        private void FindParentRandContainer(HircItem item, HircTreeItem parent)
        {
            var sqtContainer = GetAsType<ICAkRanSeqCntr>(item);
            var node = new HircTreeItem() { DisplayName = $"Rand Container {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(sqtContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(sqtContainer.GetDirectParentId(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeItem() { DisplayName = $"Actor Mixer {GetDisplayId(item.Id, item.BnkFilePath, false)} {GetParentInfo(actorMixer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetDirectParentId(), node);
        }

        private void FindParentFxShareSet(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"FxShareSet {GetDisplayId(item.Id, item.BnkFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentFxCustom(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"Fx custom {GetDisplayId(item.Id, item.BnkFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentSound(HircItem item, HircTreeItem parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeItem() { DisplayName = $"Sound {GetDisplayId(item.Id, item.BnkFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
            ProcessNext(sound.GetDirectParentId(), node);
        }

        protected override string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _audioRepository.GetNameFromId(id, out var found);
            if (found)
                return $"'{name}' with Id[{id}] in {fileName}";
            else
                return $"with Id[{id}] in {fileName}";
        }

        string GetParentInfo(uint id)
        {
            var name = _audioRepository.GetNameFromId(id, out var found);
            if (found)
                return $"has parent '{name}' with Id[{id}]";
            else
                return $"has parent with Id[{id}]";
        }
    }
}
