using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Storage;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc;

namespace Editors.Audio.Utility
{
    public class WWiseTreeParserParent : WWiseTreeParserBase
    {
        public WWiseTreeParserParent(IAudioRepository repository, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(repository, showId, showOwningBnkFile, filterByBnkName)
        {
            _hircProcessChildMap.Add(HircType.SwitchContainer, FindParentSwitchControl);
            _hircProcessChildMap.Add(HircType.LayerContainer, FindParentLayerContainer);
            _hircProcessChildMap.Add(HircType.SequenceContainer, FindParentRandContainer);
            _hircProcessChildMap.Add(HircType.Sound, FindParentSound);
            _hircProcessChildMap.Add(HircType.ActorMixer, FindParentActorMixer);
            _hircProcessChildMap.Add(HircType.FxCustom, FindParentFxCustom);
            _hircProcessChildMap.Add(HircType.FxShareSet, FindParentFxShareSet);
        }

        private void FindParentLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(layerContainer.GetDirectParentId())}", Item = item };
            parent.Children.Add(layerNode);
            ProcessNext(layerContainer.GetDirectParentId(), layerNode);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeItem parent)
        {
            var wwiseObject = GetAsType<ICAkSwitchCntr>(item);
            var switchNode = new HircTreeItem() { DisplayName = $"Switch Container {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(wwiseObject.GetDirectParentId())}", Item = item };
            parent.Children.Add(switchNode);
            ProcessNext(wwiseObject.GetDirectParentId(), switchNode);
        }

        private void FindParentRandContainer(HircItem item, HircTreeItem parent)
        {
            var sqtContainer = GetAsType<CAkRanSeqCnt>(item);
            var node = new HircTreeItem() { DisplayName = $"Rand Container {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(sqtContainer.GetParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(sqtContainer.GetParentId(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeItem() { DisplayName = $"Actor Mixer {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(actorMixer.GetDirectParentId())}", Item = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetDirectParentId(), node);
        }


        private void FindParentFxShareSet(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"FxShareSet {GetDisplayId(item.Id, item.OwnerFile, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentFxCustom(HircItem item, HircTreeItem parent)
        {
            var node = new HircTreeItem() { DisplayName = $"Fx custom {GetDisplayId(item.Id, item.OwnerFile, false)} cant have parents", Item = item };
            parent.Children.Add(node);
        }

        private void FindParentSound(HircItem item, HircTreeItem parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeItem() { DisplayName = $"Sound {GetDisplayId(item.Id, item.OwnerFile, false)} cant have parents", Item = item };
            parent.Children.Add(node);
            ProcessNext(sound.GetDirectParentId(), node);
        }

        protected override string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _repository.GetNameFromHash(id, out var found);
            if (found)
                return $"'{name}' with ID[{id}] in {fileName}";
            else
                return $"with ID[{id}] in {fileName}";
        }

        string GetParentInfo(uint id)
        {
            var name = _repository.GetNameFromHash(id, out var found);
            if (found)
                return $"has parent '{name}' with ID[{id}]";
            else
                return $"has parent with ID[{id}]";
        }
    }
}
