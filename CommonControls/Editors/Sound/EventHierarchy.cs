using FileTypes.Sound.WWise;
using System.Collections.Generic;
using System.Linq;
using FileTypes.Sound.WWise.Hirc.V122;
using System.Text;
using FileTypes.Sound.WWise.Hirc;

namespace CommonControls.Editors.Sound
{

    public class EventHierarchy
    {
        string _ownerFileName;
        ExtenededSoundDataBase _db;
        string _name;
        NameLookupHelper _nameHelper;

        VisualEventOutputNode _outputNode;
        VisualEventOutputNode _errorNode;

        public bool ProcesedCorrectly { get; set; } = true;

        public EventHierarchy(HircItem startEvent, ExtenededSoundDataBase db, NameLookupHelper nameHelper, VisualEventOutputNode rootOutput, VisualEventOutputNode errorNode, string ownerFileName)
        {
            _ownerFileName = ownerFileName;
            _nameHelper = nameHelper;
            _db = db;
            _name = _nameHelper.GetName(startEvent.Id);
            _errorNode = errorNode;

            ProcessGenericChild(startEvent, rootOutput);
        }

        void ProcessGenericChild(HircItem item, VisualEventOutputNode currentNode)
        {
            if (item is CAkAction action)
                ProcessChild(action, currentNode);
            else if (item is CAkEvent caEvent)
                ProcessChild(caEvent, currentNode);
            else if (item is CAkSound sound)
                ProcessChild(sound, currentNode);
            else if (item is CAkSwitchCntr switchContainer)
                ProcessChild(switchContainer, currentNode);
            else if (item is CAkRanSeqCnt randomContainer)
                ProcessChild(randomContainer, currentNode);
            else if (item is CAkLayerCntr layeredControl)
                ProcessChild(layeredControl, currentNode);
            else if (item is CAkDialogueEvent dialogEvent)
                ProcessChild(dialogEvent, currentNode);
            else
                ProcessUnknownChild(item, currentNode);
        }

        void ProcessChild(CAkEvent item, VisualEventOutputNode currentNode)
        {
            var name = _nameHelper.GetName(item.Id);
            var eventNode = currentNode.AddChild($"-> Event:{name}[{item.Id}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");
            if (_outputNode == null)
                _outputNode = eventNode;

            var actionIdsForEvent = item.GetActionIds();
            foreach (var id in actionIdsForEvent)
            {
                var children = _db.GetHircObject(id, item.OwnerFile, _errorNode);
                ProcessChildrenOfNode(children, eventNode);
            }
        }


        void ProcessChild(CAkDialogueEvent item, VisualEventOutputNode currentNode)
        {
            var name = _nameHelper.GetName(item.Id);
            var node = currentNode.AddChild($"-> DialogEvent:{name} \tId:[{item.Id}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");

            // arguments id and name

            foreach (var child in item.AkDecisionTree.Root.Children)
                ProcessAkDecisionTreeNode(child, node);
        }

        void ProcessChild(CAkAction item, VisualEventOutputNode currentNode)
        {
            var node = currentNode.AddChild($"CAkAction ActionType:[{item.GetActionType()}] \tId:[{item.Id}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");

            var actionRefs = _db.GetHircObject(item.GetSoundId(), _ownerFileName, _errorNode);
            ProcessChildrenOfNode(actionRefs, node);
        }

        void ProcessChild(CAkSound item, VisualEventOutputNode currentNode)
        {
            currentNode.AddChild($"CAkSound {item.GetSourceId()}.wem \tId:[{item.Id}] \tParentId:[{item.GetParentId()}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");
            _db.ReferensedSounds.Add(item.GetSourceId());
        }

        void ProcessChild(CAkSwitchCntr item, VisualEventOutputNode currentNode)
        {
            var node = currentNode.AddChild($"CAkSwitchCntr EnumGroup:[{_nameHelper.GetName(item.ulGroupID)}] \tDefault:[{_nameHelper.GetName(item.ulDefaultSwitch)}] \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]" );
            foreach (var switchCase in item.SwitchList)
            {
                var switchCaseNode = node.AddChild($"SwitchValue [{_nameHelper.GetName(switchCase.SwitchId)}]");
                foreach (var child in switchCase.NodeIdList)
                {
                    var childRefs = _db.GetHircObject(child, _ownerFileName, _errorNode);
                    ProcessChildrenOfNode(childRefs, switchCaseNode);
                }
            }
        }
        void ProcessChild(CAkLayerCntr item, VisualEventOutputNode currentNode)
        {
            var node = currentNode.AddChild($"CAkLayerCntr \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]");
            foreach (var layer in item.LayerList)
            {
                var switchCaseNode = node.AddChild($"LayerChildItem Id:[{layer.ulLayerID}] \trtpcID:[{_nameHelper.GetName(layer.rtpcID)}]");
                foreach (var child in layer.CAssociatedChildDataList)
                {
                    var childRefs = _db.GetHircObject(child.ulAssociatedChildID, _ownerFileName, _errorNode);
                    ProcessChildrenOfNode(childRefs, switchCaseNode);
                }
            }
        }

        void ProcessChild(CAkRanSeqCnt item, VisualEventOutputNode currentNode)
        {
            var node = currentNode.AddChild($"CAkRanSeqCnt \tId:[{item.Id}] \tParentId:[{item.GetParentId()}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]" );
            var children = item.GetChildren();
            foreach (var child in children)
            {
                var playListRefs = _db.GetHircObject(child, _ownerFileName, _errorNode);
                ProcessChildrenOfNode(playListRefs, node);
            }
        }



        void ProcessAkDecisionTreeNode(AkDecisionTree.Node node, VisualEventOutputNode currentOutputNode)
        {
            var name = _nameHelper.GetName(node.key);
            var outputNode = currentOutputNode.AddChild($"DialogNode {name} Id:[{node.key}]");

            foreach (var childNode in node.Children)
                ProcessAkDecisionTreeNode(childNode, outputNode);

            foreach (var childNode in node.SoundNodes)
            {
                var childNodeName = _nameHelper.GetName(childNode.key);
                var soundChildNode = outputNode.AddChild($"Sound_Node {childNodeName}  Id:[{childNode.key}] AudioNodeId:[{childNode.audioNodeId}]");
                    
                var nextItems = _db.GetHircObject(childNode.audioNodeId, _ownerFileName, _errorNode);
                ProcessChildrenOfNode(nextItems, soundChildNode);
            }
        }

        void ProcessUnknownChild(HircItem item, VisualEventOutputNode currentNode)
        {
            var errorStr = $"Reference to unknown HricItem Type:[{item.Type}] \tId:[{item.Id}] ownerFile:[{item.OwnerFile}|{item.IndexInFile}]";
            currentNode.AddChild(errorStr);
            _errorNode.AddChild(errorStr);
            _db.UnknownObjectTypes.Add(item.Type.ToString());

            ProcesedCorrectly = false;
        }

        void ProcessChildrenOfNode(List<HircItem> children, VisualEventOutputNode currentOutputNode)
        {
            if (children.Count > 1)
                currentOutputNode.Data += " DuplicateChildRef " + children.Count + "{" +  string.Join(", ", children.Select(x=>x.OwnerFile + " " + x.Id)) + "}";

            foreach (var child in children)
                ProcessGenericChild(child, currentOutputNode);
        }
    }


}


