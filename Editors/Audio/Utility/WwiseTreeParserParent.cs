using Editors.Audio.AudioExplorer;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Utility
{
    public class WwiseTreeParserParent : WwiseTreeParserBase
    {
        public WwiseTreeParserParent(IAudioRepository repository, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(repository, showId, showOwningBnkFile, filterByBnkName)
        {
            _hircProcessChildMap.Add(AkBkHircType.SwitchContainer, FindParentSwitchControl);
            _hircProcessChildMap.Add(AkBkHircType.LayerContainer, FindParentLayerContainer);
            _hircProcessChildMap.Add(AkBkHircType.SequenceContainer, FindParentRandContainer);
            _hircProcessChildMap.Add(AkBkHircType.Sound, FindParentSound);
            _hircProcessChildMap.Add(AkBkHircType.ActorMixer, FindParentActorMixer);
            _hircProcessChildMap.Add(AkBkHircType.FxCustom, FindParentFxCustom);
            _hircProcessChildMap.Add(AkBkHircType.FxShareSet, FindParentFxShareSet);
        }

        private void FindParentLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container {GetDisplayId(item.ID, item.OwnerFilePath, false)} {GetParentInfo(layerContainer.GetDirectParentID())}", Item = item };
            parent.Children.Add(layerNode);
            ProcessNext(layerContainer.GetDirectParentID(), layerNode);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeItem parent)
        {
            var wwiseObject = GetAsType<ICAkSwitchCntr>(item);
            var switchNode = new HircTreeItem() { DisplayName = $"Switch Container {GetDisplayId(item.ID, item.OwnerFilePath, false)} {GetParentInfo(wwiseObject.GetDirectParentID())}", Item = item };
            parent.Children.Add(switchNode);
            ProcessNext(wwiseObject.GetDirectParentID(), switchNode);
        }

        private void FindParentRandContainer(HircItem item, HircTreeItem parent)
        {
            var sqtContainer = GetAsType<ICAkRanSeqCnt>(item);
            var node = new HircTreeItem() { DisplayName = $"Rand Container {GetDisplayId(item.ID, item.OwnerFilePath, false)} {GetParentInfo(sqtContainer.GetParentID())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(sqtContainer.GetParentID(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeItem() { DisplayName = $"Actor Mixer {GetDisplayId(item.ID, item.OwnerFilePath, false)} {GetParentInfo(actorMixer.GetDirectParentID())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetDirectParentID(), node);
        }

        private void FindParentFxShareSet(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"FxShareSet {GetDisplayId(item.ID, item.OwnerFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentFxCustom(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"Fx custom {GetDisplayId(item.ID, item.OwnerFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentSound(HircItem item, HircTreeItem parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeItem() { DisplayName = $"Sound {GetDisplayId(item.ID, item.OwnerFilePath, false)} cant have parents", Item = item };
            parent.Children.Add(node);
            ProcessNext(sound.GetDirectParentID(), node);
        }

        protected override string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _repository.GetNameFromID(id, out var found);
            if (found)
                return $"'{name}' with ID[{id}] in {fileName}";
            else
                return $"with ID[{id}] in {fileName}";
        }

        string GetParentInfo(uint id)
        {
            var name = _repository.GetNameFromID(id, out var found);
            if (found)
                return $"has parent '{name}' with ID[{id}]";
            else
                return $"has parent with ID[{id}]";
        }
    }
}
