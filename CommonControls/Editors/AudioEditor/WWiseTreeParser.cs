using CommonControls.Common;
using CommonControls.Editors.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
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
            _hircProcessMap.Add(HircType.Action, ProcessAction);
            _hircProcessMap.Add(HircType.Action, ProcessSwitchControl);
            _globalSoundDb = globalSoundDb;
            _nameLookUpHelper = nameLookUpHelper;
        }

        public HircTreeItem BuildEventHierarchy(HircItem item)
        {
            HircTreeItem root = new HircTreeItem();
            ProcessHircObject(item, root);
            return root;
        }

        void ProcessHircObject(HircItem item, HircTreeItem parent)
        {
            if (_hircProcessMap.TryGetValue(item.Type, out var func))
                func(item, parent);

            // Handle unkown
        }

        void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);

            var soundId = actionHirc.GetSoundId();
            ProcessChildOfNode(soundId, actionTreeNode);

            //
            //
            //ar node = currentNode.AddChild($"CAkAction ActionType:[{item.GetActionType()}] \tId:[{item.Id}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");
            ///var actionRefs = _db.GetHircObject(item.GetSoundId(), _ownerFileName, _errorNode);
            //rocessChildrenOfNode(actionRefs, node);
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

        void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        { }

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
