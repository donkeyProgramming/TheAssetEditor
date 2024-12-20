using Shared.Core.ByteParsing;
using static Shared.GameFormats.WWise.Hirc.ICAkSwitchCntr;

namespace Shared.GameFormats.WWise.Hirc.V112
{
    public class CAkSwitchCntr_V112 : CAkSwitchCntr, ICAkSwitchCntr
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public AkGroupType EGroupType { get; set; }
        public uint UlGroupID { get; set; }   // Enum group name
        public uint UlDefaultSwitch { get; set; }    // Default value name
        public byte BIsContinuousValidation { get; set; }
        public Children Children { get; set; }
        public List<ICAkSwitchPackage> SwitchList { get; set; } = new List<ICAkSwitchPackage>();
        public List<AkSwitchNodeParams> Parameters { get; set; } = new List<AkSwitchNodeParams>();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            EGroupType = (AkGroupType)chunk.ReadByte();
            UlGroupID = chunk.ReadUInt32();
            UlDefaultSwitch = chunk.ReadUInt32();
            BIsContinuousValidation = chunk.ReadByte();
            Children = Children.Create(chunk);

            var switchListCount = chunk.ReadUInt32();
            for (var i = 0; i < switchListCount; i++)
                SwitchList.Add(CAkSwitchPackage.Create(chunk));

            var paramCount = chunk.ReadUInt32();
            for (var i = 0; i < paramCount; i++)
                Parameters.Add(AkSwitchNodeParams.Create(chunk));
        }

        public override uint GroupId => UlGroupID;
        public override uint DefaultSwitch => UlDefaultSwitch;
        public override uint ParentId => NodeBaseParams.DirectParentId;
        public override List<SwitchListItem> Items => SwitchList.Select(x => new SwitchListItem() { SwitchId = x.SwitchId, ChildNodeIds = x.NodeIdList }).ToList();
        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
    }

    public class Children
    {
        public List<uint> ChildIdList { get; set; } = new List<uint>(); // Probably the name of something, or at least a reference to something interesting

        public static Children Create(ByteChunk chunk)
        {
            var instance = new Children();
            var numChildren = chunk.ReadUInt32();
            for (var i = 0; i < numChildren; i++)
                instance.ChildIdList.Add(chunk.ReadUInt32());

            return instance;
        }
    }

    public class CAkSwitchPackage : ICAkSwitchPackage
    {
        public uint SwitchId { get; set; }  // ID/Name of the switch case
        public List<uint> NodeIdList { get; set; } = new List<uint>(); // Probably the name of something, or at least a reference to something interesting

        public static ICAkSwitchPackage Create(ByteChunk chunk)
        {
            var instance = new CAkSwitchPackage();
            instance.SwitchId = chunk.ReadUInt32();
            var numChildren = chunk.ReadUInt32();
            for (var i = 0; i < numChildren; i++)
                instance.NodeIdList.Add(chunk.ReadUInt32());

            return instance;
        }
    }

    public class AkSwitchNodeParams
    {
        public uint NodeId { get; set; }
        public byte BitVector0 { get; set; }
        public byte BitVector1 { get; set; }
        public float FadeOutTime { get; set; }
        public float FadeInTime { get; set; }

        public static AkSwitchNodeParams Create(ByteChunk chunk)
        {
            var instance = new AkSwitchNodeParams();
            instance.NodeId = chunk.ReadUInt32();
            instance.BitVector0 = chunk.ReadByte();
            instance.BitVector1 = chunk.ReadByte();
            instance.FadeOutTime = chunk.ReadSingle();
            instance.FadeInTime = chunk.ReadSingle();

            return instance;
        }
    }

    public class NodeBaseParams
    {
        public NodeInitialFxParams NodeInitialFxParams { get; set; }

        public byte BOverrideAttachmentParams { get; set; }
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public byte ByBitVector { get; set; }

        public NodeInitialParams NodeInitialParams { get; set; }
        public PositioningParams PositioningParams { get; set; }
        public AuxParams AuxParams { get; set; }
        public AdvSettingsParams AdvSettingsParams { get; set; }
        public StateChunk StateChunk { get; set; }
        public InitialRTPC InitialRTPC { get; set; }


        public byte EGroupType { get; set; } // "Switch"
        public uint UlGroupID { get; set; }     // Enum switch name
        public uint UlDefaultSwitch { get; set; }   // Enum switch defautl value
        public byte BIsContinuousValidation { get; set; } // "Switch"

        public static NodeBaseParams Create(ByteChunk chunk)
        {
            var node = new NodeBaseParams();
            node.NodeInitialFxParams = NodeInitialFxParams.Create(chunk);
            node.BOverrideAttachmentParams = chunk.ReadByte();
            node.OverrideBusId = chunk.ReadUInt32();
            node.DirectParentId = chunk.ReadUInt32();
            node.ByBitVector = chunk.ReadByte();
            node.NodeInitialParams = NodeInitialParams.Create(chunk);
            node.PositioningParams = PositioningParams.Create(chunk);
            node.AuxParams = AuxParams.Create(chunk);
            node.AdvSettingsParams = AdvSettingsParams.Create(chunk);
            node.StateChunk = StateChunk.Create(chunk);
            node.InitialRTPC = InitialRTPC.Create(chunk);

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

        public List<AkPropBundleInstance> _values = [];

        public static AkPropBundle Create(ByteChunk chunk)
        {
            var output = new AkPropBundle();
            var propsCount = chunk.ReadByte();

            for (byte i = 0; i < propsCount; i++)
                output._values.Add(new AkPropBundleInstance() { Type = (AkPropBundleType)chunk.ReadByte() });

            for (byte i = 0; i < propsCount; i++)
                output._values[i].Value = chunk.ReadSingle();

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
        public byte BIsOverrideParentFX { get; set; }

        public byte BitsFXBypass { get; set; }
        public List<FXChunk> FxList { get; set; } = [];

        public static NodeInitialFxParams Create(ByteChunk chunk)
        {
            var instance = new NodeInitialFxParams();
            instance.BIsOverrideParentFX = chunk.ReadByte();
            var uNumFx = chunk.ReadByte();

            if (uNumFx != 0)
            {
                instance.BitsFXBypass = chunk.ReadByte();
                for (var i = 0; i < uNumFx; i++)
                    instance.FxList.Add(FXChunk.Create(chunk));
            }

            return instance;
        }
    }

    public class FXChunk
    {
        public byte UFXIndex { get; set; }
        public uint FxID { get; set; }
        public byte BIsShareSet { get; set; }
        public byte BIsRendered { get; set; }

        public static FXChunk Create(ByteChunk chunk)
        {
            var instance = new FXChunk();
            instance.UFXIndex = chunk.ReadByte();
            instance.FxID = chunk.ReadUInt32();
            instance.BIsShareSet = chunk.ReadByte();
            instance.BIsRendered = chunk.ReadByte();
            return instance;
        }
    }


    public class PositioningParams
    {
        public byte UByVector { get; set; }
        public byte UBits3d { get; set; }
        public uint UAttenuationID { get; set; }

        public byte EPathMode { get; set; }
        public float TransitionTime { get; set; }
        public List<AkPathVertex> VertexList { get; set; } = [];
        public List<AkPathListItemOffset> PlayListItems { get; set; } = [];
        public List<Ak3DAutomationParams> Params { get; set; } = [];

        public static PositioningParams Create(ByteChunk chunk)
        {
            var instance = new PositioningParams();
            instance.UByVector = chunk.ReadByte();

            var bPositioningInfoOverrideParent = (instance.UByVector >> 0 & 1) == 1;
            var cbIs3DPositioningAvailable = (instance.UByVector >> 3 & 1) == 1;

            if (bPositioningInfoOverrideParent && cbIs3DPositioningAvailable)
            {
                instance.UBits3d = chunk.ReadByte();
                instance.UAttenuationID = chunk.ReadUInt32();

                if ((instance.UBits3d >> 0 & 1) == 0)
                {
                    instance.EPathMode = chunk.ReadByte();
                    instance.TransitionTime = chunk.ReadSingle();

                    var numVertexes = chunk.ReadUInt32();
                    for (var i = 0; i < numVertexes; i++)
                        instance.VertexList.Add(AkPathVertex.Create(chunk));

                    var numPlayListItems = chunk.ReadUInt32();
                    for (var i = 0; i < numPlayListItems; i++)
                        instance.PlayListItems.Add(AkPathListItemOffset.Create(chunk));

                    //var numParams = 4;
                    //if (instance.ePathMode == 0x05)   //StepRandomPickNewPath
                    //    numParams = 1;
                    for (var i = 0; i < numPlayListItems; i++)
                        instance.Params.Add(Ak3DAutomationParams.Create(chunk));
                }
            }

            return instance;
        }
    }

    public class AkPathVertex
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Duration { get; set; }

        public static AkPathVertex Create(ByteChunk chunk)
        {
            var instance = new AkPathVertex();
            instance.X = chunk.ReadSingle();
            instance.Y = chunk.ReadSingle();
            instance.Z = chunk.ReadSingle();
            instance.Duration = chunk.ReadInt32();

            return instance;
        }
    }

    public class AkPathListItemOffset
    {
        public uint ulVerticesOffset { get; set; }
        public uint iNumVertices { get; set; }

        public static AkPathListItemOffset Create(ByteChunk chunk)
        {
            var instance = new AkPathListItemOffset();
            instance.ulVerticesOffset = chunk.ReadUInt32();
            instance.iNumVertices = chunk.ReadUInt32();

            return instance;
        }
    }

    public class Ak3DAutomationParams
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static Ak3DAutomationParams Create(ByteChunk chunk)
        {
            var instance = new Ak3DAutomationParams();
            instance.X = chunk.ReadSingle();
            instance.Y = chunk.ReadSingle();
            instance.Z = chunk.ReadSingle();

            return instance;
        }
    }

    public class AuxParams
    {
        public byte ByBitVector { get; set; }
        public uint AuxID0 { get; set; }
        public uint AuxID1 { get; set; }
        public uint AuxID2 { get; set; }
        public uint AuxID3 { get; set; }

        public static AuxParams Create(ByteChunk chunk)
        {
            var instance = new AuxParams();
            instance.ByBitVector = chunk.ReadByte();

            if ((instance.ByBitVector >> 3 & 1) == 1)
            {
                instance.AuxID0 = chunk.ReadUInt32();
                instance.AuxID1 = chunk.ReadUInt32();
                instance.AuxID2 = chunk.ReadUInt32();
                instance.AuxID3 = chunk.ReadUInt32();
            }

            return instance;
        }
    }

    public class AdvSettingsParams
    {
        public byte ByBitVector { get; set; }
        public byte EVirtualQueueBehavior { get; set; }
        public ushort U16MaxNumInstance { get; set; }
        public byte EBelowThresholdBehavior { get; set; }
        public byte ByBitVector2 { get; set; }

        public static AdvSettingsParams Create(ByteChunk chunk)
        {
            return new AdvSettingsParams() { ByBitVector = chunk.ReadByte(), EVirtualQueueBehavior = chunk.ReadByte(), U16MaxNumInstance = chunk.ReadUShort(), EBelowThresholdBehavior = chunk.ReadByte(), ByBitVector2 = chunk.ReadByte() };
        }
    }

    public class StateChunk
    {
        public List<AkStateGroupChunk> StateChunks { get; set; } = new List<AkStateGroupChunk>();

        public static StateChunk Create(ByteChunk chunk)
        {
            var instance = new StateChunk();
            var value = chunk.ReadUInt32();
            for (var i = 0; i < value; i++)
                instance.StateChunks.Add(AkStateGroupChunk.Create(chunk));
            return instance;
        }
    }

    public class AkStateGroupChunk
    {
        public uint UlStateGroupID { get; set; }
        public byte EStateSyncType { get; set; }
        public List<AkState> States { get; set; } = new List<AkState>();

        public static AkStateGroupChunk Create(ByteChunk chunk)
        {
            var instance = new AkStateGroupChunk();
            instance.UlStateGroupID = chunk.ReadUInt32();
            instance.EStateSyncType = chunk.ReadByte();

            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.States.Add(AkState.Create(chunk));

            return instance;
        }
    }

    public class AkState
    {
        public uint UlStateID { get; set; }
        public uint UlStateInstanceID { get; set; }
        public static AkState Create(ByteChunk chunk)
        {
            var instance = new AkState();
            instance.UlStateID = chunk.ReadUInt32();
            instance.UlStateInstanceID = chunk.ReadUInt32();
            return instance;
        }
    }

    public class InitialRTPC
    {
        public List<RTPC> RTPCList { get; set; } = new List<RTPC>();

        public static InitialRTPC Create(ByteChunk chunk)
        {
            var instance = new InitialRTPC();
            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.RTPCList.Add(RTPC.Create(chunk));

            return instance;
        }
    }

    public class RTPC
    {
        public uint RTPCID { get; set; }
        public byte RtpcType { get; set; }
        public byte RtpcAccum { get; set; }
        public byte ParamID { get; set; }
        public uint RtpcCurveID { get; set; }
        public byte EScaling { get; set; }
        public List<AkRTPCGraphPoint> PRTPCMgr { get; set; } = new List<AkRTPCGraphPoint>();

        public static RTPC Create(ByteChunk chunk)
        {
            var instance = new RTPC();
            instance.RTPCID = chunk.ReadUInt32();
            instance.RtpcType = chunk.ReadByte();
            instance.RtpcAccum = chunk.ReadByte();
            instance.ParamID = chunk.ReadByte();
            instance.RtpcCurveID = chunk.ReadUInt32();
            instance.EScaling = chunk.ReadByte();

            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.PRTPCMgr.Add(AkRTPCGraphPoint.Create(chunk));

            return instance;
        }
    }

    public class AkRTPCGraphPoint
    {
        public float From { get; set; }
        public float To { get; set; }
        public uint Interp { get; set; }

        public static AkRTPCGraphPoint Create(ByteChunk chunk)
        {
            var instance = new AkRTPCGraphPoint();
            instance.From = chunk.ReadSingle();
            instance.To = chunk.ReadSingle();
            instance.Interp = chunk.ReadUInt32();
            return instance;
        }
    }
}
