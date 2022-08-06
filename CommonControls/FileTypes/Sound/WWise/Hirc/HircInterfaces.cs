using CommonControls.FileTypes.Sound.WWise;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc
{
    public interface ICAkEvent
    {
        public List<uint> GetActionIds();
    }

    public interface ICAkSound 
    {
        public uint GetParentId();
        public uint GetSourceId();
    }

    public interface ICAkAction
    {
        public ActionType GetActionType();
        public uint GetChildId();
    }

    // Conert to interfaces 

    public abstract class CAkRanSeqCnt : HircItem
    {
        public abstract uint GetParentId();
        public abstract List<uint> GetChildren();
    }


    public abstract class CADialogEvent : HircItem
    {
        public abstract List<ChildNode> GetChildren();

        public abstract class ChildNode
        {
            public abstract uint GetKey();

            public abstract List<ChildNode> GetChildren();
            public abstract List<SoundNode> GetSoundNodes();
        }

        public abstract class SoundNode
        {
            public abstract uint GetKey();
            public abstract uint GetAudioNodeId();
        }
    }

    public abstract class CAkSwitchCntr : HircItem
    {
        public abstract uint GroupId { get; }
        public abstract uint DefaultSwitch { get; }
        public abstract uint ParentId { get; }
        public abstract List<SwitchListItem> Items { get; } 

        public class SwitchListItem
        {
            public uint SwitchId { get; set; }
            public List<uint> ChildNodeIds { get; set; }
        }
    }

    public abstract class CAkLayerCntr : HircItem
    {
        public abstract uint ParentId { get; }
        public abstract List<Layer> Layers { get; }

        public class Layer
        {
            public uint LayerId { get; set; }
            public uint RtpcID { get; set; }
            public List<uint> AssociatedChildDataListIds { get; set; }
        }
    }

    public abstract class CAkDialogueEvent : HircItem
    { 
        public abstract List<Node> Nodes { get; }
    

        public abstract class Node
        {
            abstract public uint Key { get; }
            abstract public List<Node> ChildNodes { get; }
            public abstract List<SoundNode> SoundNodes { get; }
        }

        public abstract class SoundNode
        {
            abstract public uint Key { get; }
            abstract public uint AudioNodeId { get; }
        }
    }
}