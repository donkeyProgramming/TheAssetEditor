using CommonControls.Common;
using CommonControls.Editors.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.FileTypes.Sound.WWise.Hirc.V136;
using MoreLinq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.Editors.AudioEditor
{

    public class WWiseTreeParserChildren : WWiseTreeParserBase
    {
        public WWiseTreeParserChildren(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(globalSoundDb, nameLookUpHelper, showId, showOwningBnkFile, filterByBnkName)
        {
            _hircProcessChildMap.Add(HircType.Event, ProcessEvent);
            _hircProcessChildMap.Add(HircType.Action, ProcessAction);
            _hircProcessChildMap.Add(HircType.SwitchContainer, ProcessSwitchControl);
            _hircProcessChildMap.Add(HircType.LayerContainer, ProcessLayerContainer);
            _hircProcessChildMap.Add(HircType.SequenceContainer, ProcessSequenceContainer);
            _hircProcessChildMap.Add(HircType.Sound, ProcessSound);
            _hircProcessChildMap.Add(HircType.ActorMixer, ProcessActorMixer);
        }

        void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Event {_nameLookUpHelper.GetName(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessNext(actions, actionTreeNode);
        }

        void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);

            var childId = actionHirc.GetChildId();
            ProcessNext(childId, actionTreeNode);
        }

        private void ProcessSound(HircItem item, HircTreeItem parent)
        {
            var soundHirc = GetAsType<ICAkSound>(item);
            var soundTreeNode = new HircTreeItem() { DisplayName = $"Sound {soundHirc.GetSourceId()}.wav", Item = item };
            parent.Children.Add(soundTreeNode);
        }

        private void ProcessActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<CAkActorMixer_v136>(item);
            var actorMixerNode = new HircTreeItem() { DisplayName = $"ActorMixer {_nameLookUpHelper.GetName(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.Children.ChildIdList, actorMixerNode);
        }

        void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchControl = GetAsType<CAkSwitchCntr_v136>(item);
            var switchType = _nameLookUpHelper.GetName(switchControl.ulGroupID);
            var defaultValue = _nameLookUpHelper.GetName(switchControl.ulDefaultSwitch);
            var switchControlNode = new HircTreeItem() { DisplayName = $"Switch {switchType} DefaultValue: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = _nameLookUpHelper.GetName(switchCase.SwitchId);
                var switchValueNode = new HircTreeItem() { DisplayName = $"SwitchValue: {switchValue}", Item = item };
                switchControlNode.Children.Add(switchValueNode);

                ProcessNext(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkLayerCntr_v136>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container", Item = item };
            parent.Children.Add(layerNode);

            foreach (var layer in layerContainer.Children.ChildIdList)
                ProcessNext(layer, layerNode);           
        }

        private void ProcessSequenceContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkRanSeqCnt>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Rand Container", Item = item };
            parent.Children.Add(layerNode);

            ProcessNext(layerContainer.GetChildren(), layerNode);
        }
    }

    public class WWiseTreeParserParent : WWiseTreeParserBase
    {
        public WWiseTreeParserParent(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(globalSoundDb, nameLookUpHelper, showId, showOwningBnkFile, filterByBnkName)
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
            var layerContainer = GetAsType<CAkLayerCntr_v136>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(layerContainer.NodeBaseParams.DirectParentID)}", Item = item };
            parent.Children.Add(layerNode);
            ProcessNext(layerContainer.NodeBaseParams.DirectParentID, layerNode);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeItem parent)
        {
            var wwiseObject = GetAsType<CAkSwitchCntr_v136>(item);
            var switchNode = new HircTreeItem() { DisplayName = $"Switch Container {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(wwiseObject.NodeBaseParams.DirectParentID)}", Item = item };
            parent.Children.Add(switchNode);
            ProcessNext(wwiseObject.NodeBaseParams.DirectParentID, switchNode);
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
            var actorMixer = GetAsType<CAkActorMixer_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Actor Mixer {GetDisplayId(item.Id, item.OwnerFile, false)} {GetParentInfo(actorMixer.NodeBaseParams.DirectParentID)}", Item = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.NodeBaseParams.DirectParentID, node);
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
            ProcessNext(sound.GetParentId(), node);
        }

        protected override string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _nameLookUpHelper.GetName(id, out var found);
            if (found)
                return $"'{name}' with ID[{id}] in {fileName}";
            else
                return $"with ID[{id}] in {fileName}";
        }

        string GetParentInfo(uint id)
        {
            var name = _nameLookUpHelper.GetName(id, out var found);
            if (found)
                return $"has parent '{name}' with ID[{id}]";
            else
                return $"has parent with ID[{id}]";
        }
    }

    public abstract class WWiseTreeParserBase
    {
        protected ILogger _logger = Logging.Create<WWiseTreeParserBase>();
        
        protected Dictionary<HircType, Action<HircItem, HircTreeItem>> _hircProcessChildMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        protected readonly ExtenededSoundDataBase _globalSoundDb;
        protected readonly WWiseNameLookUpHelper _nameLookUpHelper;
                
        protected readonly bool _showId;
        protected readonly bool _showOwningBnkFile;
        protected readonly bool _filterByBnkName;

        public WWiseTreeParserBase(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper, bool showId, bool showOwningBnkFile, bool filterByBnkName)
        {
            _globalSoundDb = globalSoundDb;
            _nameLookUpHelper = nameLookUpHelper;
            _showId = showId;
            _showOwningBnkFile = showOwningBnkFile;
            _filterByBnkName = filterByBnkName;
        }

        public HircTreeItem BuildHierarchy(HircItem item)
        {
            HircTreeItem root = new HircTreeItem();
            ProcessHircObject(item, root);
            var actualRoot = root.Children.FirstOrDefault();
            actualRoot.Parent = null;
            root.Children = null;
            return actualRoot;
        }

        void ProcessHircObject(HircItem item, HircTreeItem parent)
        {
            if (_hircProcessChildMap.TryGetValue(item.Type, out var func))
            {
                func(item, parent);
            }
            else
            {
                var unkownNode = new HircTreeItem() { DisplayName = $"Unkown node type {item.Type} for Id {item.Id} in {item.OwnerFile}", Item = item };
                parent.Children.Add(unkownNode);
            }
        }


        protected void ProcessNext(uint hircId, HircTreeItem parent)
        {
            var instances = _globalSoundDb.GetHircObject(hircId);
            var hircItem = instances.FirstOrDefault();
            if (hircItem == null)
                parent.Children.Add(new HircTreeItem() { DisplayName = $"Error: Unable to find ID {hircId}" });
            else
                ProcessHircObject(hircItem, parent);
        }


        protected void ProcessNext(List<uint> ids, HircTreeItem parent)
        {
            foreach (var id in ids)
                ProcessNext(id, parent);
        }


        protected virtual string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
        {
            var name = _nameLookUpHelper.GetName(id, out var found);
            if (hidenNameIfMissing)
                name = "";
            if (found == true && _showId)
                name += " " + id;
            if (_showOwningBnkFile && string.IsNullOrWhiteSpace(fileName) == false)
                name += " " + fileName;
            return name;
        }

        protected string GetDisplayId(HircItem item, bool hidenNameIfMissing) => GetDisplayId(item.Id, item.OwnerFile, hidenNameIfMissing);

        protected Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            var wanted = instance as Wanted;
            if (wanted == null)
                throw new Exception();
            return wanted;
        }
    }
}
