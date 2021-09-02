using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
{

    public class CAkSwitchCntr : HricItem
    {

        public class Action
        {
            public uint ActionId { get; set; }
        }


        public List<Action> Actions { get; set; } = new List<Action>();

        public static CAkSwitchCntr Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var switchCntr = new CAkSwitchCntr();
            switchCntr.LoadCommon(chunk);

            //var actionCount = chunk.ReadUInt32();
            ///for (int i = 0; i < actionCount; i++)
            //    akEvent.Actions.Add(new Action() { ActionId = chunk.ReadUInt32() });

            switchCntr.SkipToEnd(chunk, objectStartIndex + 5);
            return switchCntr;

        }
    }

    public class NodeBaseParams
    {
        public NodeInitialFxParams NodeInitialFxParams { get; set; }


        public byte bOverrideAttachmentParams { get; set; }
        public uint OverrideBusId { get; set; }
        public uint DirectParentID { get; set; }
        public byte byBitVector { get; set; }

        //public NodeInitialParams NodeInitialParams { get; set; }

        public static NodeBaseParams Create(ByteChunk chunk)
        {
            var node = new NodeBaseParams();
            node.NodeInitialFxParams = NodeInitialFxParams.Create(chunk);
            node.bOverrideAttachmentParams = chunk.ReadByte();
            node.OverrideBusId = chunk.ReadUInt32();
            node.DirectParentID = chunk.ReadUInt32();
            node.byBitVector = chunk.ReadByte();
            //node.NodeInitialParams = NodeInitialParams.Create(chunk);

            return node;
        }
    }

    public class NodeInitialFxParams
    {
        public byte bIsOverrideParentFX { get; set; }
        public byte uNumFx { get; set; }
        public static NodeInitialFxParams Create(ByteChunk chunk)
        {
            return new NodeInitialFxParams() { bIsOverrideParentFX = chunk.ReadByte(), uNumFx = chunk.ReadByte() };
        }
    }

    //public class NodeInitialParams
    //{
    //    AkPropBundleList AkPropBundle0;
    //    AkPropBundleList AkPropBundle1;
    //
    //    public static NodeInitialParams Create(ByteChunk chunk)
    //    {
    //    }
    //}
    //
    //public class AkPropBundleList
    //{
    //    public class AkPropBundle
    //    { }
    //
    //    public static AkPropBundleList Create(ByteChunk chunk)
    //    {
    //    }
    //}
}
