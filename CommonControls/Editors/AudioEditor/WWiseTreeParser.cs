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

    public class WWiseSoundTreeParserErrorInterigator
    {

    }

    public class WWiseTreeParser
    {
        ILogger _logger = Logging.Create<WWiseTreeParser>();
       
        Dictionary<HircType, Action<HircItem, HircTreeItem>> _hircProcessMap = new Dictionary<HircType, Action<HircItem, HircTreeItem>>();
        private readonly ExtenededSoundDataBase _globalSoundDb;
        private readonly WWiseNameLookUpHelper _nameLookUpHelper;

        public WWiseTreeParser(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper)
        {
            _hircProcessMap.Add(HircType.Event, ProcessEvent);
            _hircProcessMap.Add(HircType.Action, ProcessAction);
            _hircProcessMap.Add(HircType.SwitchContainer, ProcessSwitchControl);
            _hircProcessMap.Add(HircType.LayerContainer, ProcessLayerContainer);
            _hircProcessMap.Add(HircType.SequenceContainer, ProcessSequenceContainer);
            _hircProcessMap.Add(HircType.Sound, ProcessSound);
            _globalSoundDb = globalSoundDb;
            _nameLookUpHelper = nameLookUpHelper;
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
            if (_hircProcessMap.TryGetValue(item.Type, out var func))
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

 

        //void ProcessSwitchControl(CAkSwitchCntr item, VisualEventOutputNode currentNode)
        //{
        //    var node = currentNode.AddChild($"CAkSwitchCntr EnumGroup:[{_nameHelper.GetName(item.GroupId)}] \tDefault:[{_nameHelper.GetName(item.DefaultSwitch)}] \tId:[{item.Id}] \tParentId:[{item.ParentId}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");
        //    foreach (var switchCase in item.Items)
        //    {
        //        var switchCaseNode = node.AddChild($"SwitchValue [{_nameHelper.GetName(switchCase.SwitchId)}]");
        //        foreach (var child in switchCase.ChildNodeIds)
        //        {
        //            var childRefs = _db.GetHircObject(child, _ownerFileName, _errorNode);
        //            ProcessChildrenOfNode(childRefs, switchCaseNode);
        //        }
        //    }
        //}


        void ProcessChildrenOfNode(HircItem item, HircTreeItem parent)
        { }


        Wanted GetAsType<Wanted>(HircItem instance) where Wanted : class
        {
            var wanted = instance as Wanted;
            if (wanted == null)
                throw new Exception();
            return wanted;
        }
    }
}
