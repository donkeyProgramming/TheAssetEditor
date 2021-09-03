using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
{

    public class CAkSwitchCntr : HricItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }

        public static CAkSwitchCntr Create(ByteChunk chunk)
        {
            // Start
            var objectStartIndex = chunk.Index;

            var switchCntr = new CAkSwitchCntr();
            switchCntr.LoadCommon(chunk);
            switchCntr.NodeBaseParams = NodeBaseParams.Create(chunk);

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

        public NodeInitialParams NodeInitialParams { get; set; }
        public PositioningParams PositioningParams { get; set; }
        public AuxParams AuxParams { get; set; }
        public AdvSettingsParams AdvSettingsParams { get; set; }
        public StateChunk StateChunk { get; set; }
        public InitialRTPC InitialRTPC { get; set; }


        public byte eGroupType { get; set; } // "Switch"
        public uint ulGroupID { get; set; }     // Enum switch name
        public uint ulDefaultSwitch { get; set; }   // Enum switch defautl value
        public byte bIsContinuousValidation { get; set; } // "Switch"

        public static NodeBaseParams Create(ByteChunk chunk)
        {
            var node = new NodeBaseParams();
            node.NodeInitialFxParams = NodeInitialFxParams.Create(chunk);
            node.bOverrideAttachmentParams = chunk.ReadByte();
            node.OverrideBusId = chunk.ReadUInt32();
            node.DirectParentID = chunk.ReadUInt32();
            node.byBitVector = chunk.ReadByte();
            node.NodeInitialParams = NodeInitialParams.Create(chunk);
            node.PositioningParams = PositioningParams.Create(chunk);
            node.AuxParams = AuxParams.Create(chunk);
            node.AdvSettingsParams = AdvSettingsParams.Create(chunk);
            node.StateChunk = StateChunk.Create(chunk);
            node.InitialRTPC = InitialRTPC.Create(chunk);

            node.eGroupType = chunk.ReadByte();
            node.ulGroupID = chunk.ReadUInt32();
            node.ulDefaultSwitch = chunk.ReadUInt32();
            node.bIsContinuousValidation = chunk.ReadByte();

            return node;
        }
    }

    public class NodeInitialParams
    {
        public AkPropBundle AkPropBundle0 { get; set; }
        public AkPropBundleMinMax AkPropBundle1 { get; set; }

        public static NodeInitialParams Create(ByteChunk chunk)
        {
            return new NodeInitialParams() 
            {
                AkPropBundle0 = AkPropBundle.Create(chunk),
                AkPropBundle1 = AkPropBundleMinMax.Create(chunk),
            };
        }
    }

    public class AkPropBundle
    {
        public class AkPropBundleInstance
        { 
            public AkPropBundleType Type { get; set; }
            public float Value { get; set; }
        }

        public List<AkPropBundleInstance> Values = new List<AkPropBundleInstance>();

        public static AkPropBundle Create(ByteChunk chunk)
        {
            var output = new AkPropBundle();
            var propsCount = chunk.ReadByte();

            for (byte i = 0; i < propsCount; i++)
                output.Values.Add(new AkPropBundleInstance() { Type = (AkPropBundleType)chunk.ReadByte() });

            for (byte i = 0; i < propsCount; i++)
                output.Values[i].Value = chunk.ReadSingle();

            return output;
        }
    }

    public class AkPropBundleMinMax
    {
        public class AkPropBundleInstance
        {
            public AkPropBundleType Type { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }
        }

        public List<AkPropBundleInstance> Values = new List<AkPropBundleInstance>();

        public static AkPropBundleMinMax Create(ByteChunk chunk)
        {
            var output = new AkPropBundleMinMax();
            var propsCount = chunk.ReadByte();

            for (byte i = 0; i < propsCount; i++)
                output.Values.Add(new AkPropBundleInstance() { Type = (AkPropBundleType)chunk.ReadByte() });

            for (byte i = 0; i < propsCount; i++)
            {
                output.Values[i].Min = chunk.ReadSingle();
                output.Values[i].Max = chunk.ReadSingle();
            }

            return output;
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


    public class PositioningParams
    {
        public byte uByVector { get; set; }

        public static PositioningParams Create(ByteChunk chunk)
        {
            return new PositioningParams() { uByVector = chunk.ReadByte() };
        }
    }

    public class AuxParams
    {
        public byte byBitVector { get; set; }

        public static AuxParams Create(ByteChunk chunk)
        {
            return new AuxParams() { byBitVector = chunk.ReadByte() };
        }
    }


    public class AdvSettingsParams
    {
        public byte byBitVector { get; set; }
        public byte eVirtualQueueBehavior { get; set; }
        public ushort u16MaxNumInstance { get; set; }
        public byte eBelowThresholdBehavior { get; set; }
        public byte byBitVector2 { get; set; }



        public static AdvSettingsParams Create(ByteChunk chunk)
        {
            return new AdvSettingsParams() { byBitVector = chunk.ReadByte(), eVirtualQueueBehavior = chunk.ReadByte(), u16MaxNumInstance = chunk.ReadUShort(), eBelowThresholdBehavior = chunk.ReadByte(), byBitVector2 = chunk.ReadByte() };
        }
    }


    public class StateChunk
    {
        public uint ulNumStateGroups { get; set; }

        public static StateChunk Create(ByteChunk chunk)
        {
            var value = chunk.ReadUInt32();
            if (value != 0)
                throw new Exception();
            return new StateChunk() { ulNumStateGroups = value };
        }
    }


    public class InitialRTPC
    {
        public ushort ulNumRTPC { get; set; }

        public static InitialRTPC Create(ByteChunk chunk)
        {
            var value = chunk.ReadUShort();
            if (value != 0)
                throw new Exception();
            return new InitialRTPC() { ulNumRTPC = value };
        }
    }

}
