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
    public class WWiseTreeParserBase
    {
        Dictionary<HircType, Action<HircItem, HircTreeItem>> _processMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        protected readonly ExtenededSoundDataBase _globalSoundDb;
        protected readonly WWiseNameLookUpHelper _nameLookUpHelper;

        protected readonly bool _showId;
        protected readonly bool _showOwningBnkFile;
        protected readonly bool _filterByBnkName;



        public HircTreeItem BuildEventHierarchy(HircItem item)
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
            if (_processMap.TryGetValue(item.Type, out var func))
            {
                func(item, parent);
            }
            else
            {
                var unkownNode = new HircTreeItem() { DisplayName = $"Unkown node type {item.Type} for Id {item.Id} in {item.OwnerFile}", Item = item };
                parent.Children.Add(unkownNode);
            }
        }

        string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
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

        string GetDisplayId(HircItem item, bool hidenNameIfMissing) => GetDisplayId(item.Id, item.OwnerFile, hidenNameIfMissing);

        Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            var wanted = instance as Wanted;
            if (wanted == null)
                throw new Exception();
            return wanted;
        }
    }

    public class WWiseTreeParser
    {
        ILogger _logger = Logging.Create<WWiseTreeParser>();
       
        Dictionary<HircType, Action<HircItem, HircTreeItem>> _hircProcessChildMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        Dictionary<HircType, Action<HircItem, HircTreeItem>> _hircFindParentMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        private readonly ExtenededSoundDataBase _globalSoundDb;
        private readonly WWiseNameLookUpHelper _nameLookUpHelper;


        private readonly bool _showId;
        private readonly bool _showOwningBnkFile;
        private readonly bool _filterByBnkName;

        public WWiseTreeParser(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper, bool showId, bool showOwningBnkFile, bool filterByBnkName)
        {
            _hircProcessChildMap.Add(HircType.Event, ProcessEvent);
            _hircProcessChildMap.Add(HircType.Action, ProcessAction);
            _hircProcessChildMap.Add(HircType.SwitchContainer, ProcessSwitchControl);
            _hircProcessChildMap.Add(HircType.LayerContainer, ProcessLayerContainer);
            _hircProcessChildMap.Add(HircType.SequenceContainer, ProcessSequenceContainer);
            _hircProcessChildMap.Add(HircType.Sound, ProcessSound);
            _hircProcessChildMap.Add(HircType.ActorMixer, ProcessActorMixer);
            // Audio stuff



            //_hircFindParentMap.Add(HircType.Event, ProcessEvent);
            //_hircFindParentMap.Add(HircType.Action, ProcessAction);
            _hircFindParentMap.Add(HircType.SwitchContainer, FindParentSwitchControl);
            _hircFindParentMap.Add(HircType.LayerContainer, FindParentLayerContainer);
            _hircFindParentMap.Add(HircType.SequenceContainer, FindParentRandContainer);
            _hircFindParentMap.Add(HircType.Sound, FindParentSound);
            _hircFindParentMap.Add(HircType.ActorMixer, FindParentActorMixer);

            //_hircFindParentMap.Add(HircType.Audio_Bus, FindParentActorMixer);
            //_hircFindParentMap.Add(HircType.AuxiliaryBus, FindParentActorMixer);
            //_hircFindParentMap.Add(HircType.FxCustom, FindParentActorMixer);
            //_hircFindParentMap.Add(HircType.FxShareSet, FindParentActorMixer);
            //_hircFindParentMap.Add(HircType.State, FindParentActorMixer);


            _globalSoundDb = globalSoundDb;
            _nameLookUpHelper = nameLookUpHelper;
            _showId = showId;
            _showOwningBnkFile = showOwningBnkFile;
            _filterByBnkName = filterByBnkName;
        }



        public HircTreeItem BuildEventHierarchy(HircItem item)
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

        void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Event {_nameLookUpHelper.GetName(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessChildrenOfNode(actions, actionTreeNode);
        }


        void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);

            var soundId = actionHirc.GetChildId();
            ProcessChildOfNode(soundId, actionTreeNode);
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

            ProcessChildrenOfNode(actorMixer.Children.ChildIdList, actorMixerNode);
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

                ProcessChildrenOfNode(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkLayerCntr_v136>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container", Item = item };
            parent.Children.Add(layerNode);

            foreach (var layer in layerContainer.LayerList)
            {
                var rtcp = _nameLookUpHelper.GetName(layer.rtpcID);
                var layerContainerNode = new HircTreeItem() { DisplayName = $"Layer - rptc:{rtcp}", Item = item };
                layerNode.Children.Add(layerContainerNode);
                foreach (var child in layer.CAssociatedChildDataList)
                    ProcessChildOfNode(child.ulAssociatedChildID, layerNode);
            }
        }

        private void ProcessSequenceContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkRanSeqCnt>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Rand Container", Item = item };
            parent.Children.Add(layerNode);

            ProcessChildrenOfNode(layerContainer.GetChildren(), layerNode);
        }


        void ProcessChildOfNode(uint hircId, HircTreeItem parent)
        {
            var instances = _globalSoundDb.GetHircObject(hircId);
            var hircItem = instances.FirstOrDefault();
            if (hircItem == null)
                parent.Children.Add(new HircTreeItem() { DisplayName = $"Error: Unable to find ID {hircId}" });
            else
                ProcessHircObject(hircItem, parent);
        }


        void ProcessChildrenOfNode(List<uint> ids, HircTreeItem parent)
        {
            foreach (var id in ids)
                ProcessChildOfNode(id, parent);
        }


        internal HircTreeItem FindAllParents(HircItem item)
        {
            HircTreeItem root = new HircTreeItem();
            FindAllParents(item, root);
            var actualRoot = root.Children.FirstOrDefault();
            actualRoot.Parent = null;
            root.Children = null;
            return actualRoot;
        }

        void FindAllParents(HircItem item, HircTreeItem parent)
        {
            if (_hircFindParentMap.TryGetValue(item.Type, out var func))
            {
                func(item, parent);
            }
            else
            {
                var unkownNode = new HircTreeItem() { DisplayName = $"Unkown node type {item.Type} for Id {item.Id} in {item.OwnerFile}", Item = item };
                parent.Children.Add(unkownNode);
            }
        }

        void ProcessParentOfNode(uint hircId, HircTreeItem parent)
        {
            var instances = _globalSoundDb.GetHircObject(hircId);
            var hircItem = instances.FirstOrDefault();
            if (hircItem == null)
                parent.Children.Add(new HircTreeItem() { DisplayName = $"Error: Unable to find ID {hircId}" });
            else
                ProcessHircObject(hircItem, parent);
        }

        private void FindParentLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkLayerCntr_v136>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container {GetDisplayId(item, true)}", Item = item };
            parent.Children.Add(layerNode);
            ProcessParentOfNode(layerContainer.NodeBaseParams.DirectParentID, layerNode);
        }

        private void FindParentSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchContainer = GetAsType<CAkSwitchCntr>(item);
            var switchNode = new HircTreeItem() { DisplayName = $"Switch Container {GetDisplayId(item, true)}", Item = item };
            parent.Children.Add(switchNode);
            ProcessParentOfNode(switchContainer.ParentId, switchNode);
        }

        private void FindParentRandContainer(HircItem item, HircTreeItem parent)
        {
            var sqtContainer = GetAsType<CAkRanSeqCnt>(item);
            var node = new HircTreeItem() { DisplayName = $"Rand Container {GetDisplayId(item, true)}", Item = item };
            parent.Children.Add(node);
            ProcessParentOfNode(sqtContainer.GetParentId(), node);
        }

        private void FindParentActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<CAkActorMixer_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Actor Mixer {GetDisplayId(item, true)}", Item = item };
            parent.Children.Add(node);
            ProcessParentOfNode(actorMixer.NodeBaseParams.DirectParentID, node);
        }

        private void FindParentSound(HircItem item, HircTreeItem parent)
        {
            var sound = GetAsType<ICAkSound>(item);
            var node = new HircTreeItem() { DisplayName = $"Sound {GetDisplayId(item, true)}", Item = item };
            parent.Children.Add(node);
            ProcessParentOfNode(sound.GetParentId(), node);
        }


        string GetDisplayId(uint id, string fileName, bool hidenNameIfMissing)
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

        string GetDisplayId(HircItem item, bool hidenNameIfMissing) => GetDisplayId(item.Id, item.OwnerFile, hidenNameIfMissing);

        Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            var wanted = instance as Wanted;
            if (wanted == null)
                throw new Exception();
            return wanted;
        }
    }
}
