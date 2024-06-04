using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V122
{

    public class CAkSwitchCntr_v122 : HircItem
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public AkGroupType eGroupType { get; set; }
        public uint ulGroupID { get; set; }   // Enum group name
        public uint ulDefaultSwitch { get; set; }    // Default value name
        public byte bIsContinuousValidation { get; set; }
        public Children Children { get; set; }
        public List<CAkSwitchPackage> SwitchList { get; set; } = new List<CAkSwitchPackage>();
        public List<AkSwitchNodeParams> Parameters { get; set; } = new List<AkSwitchNodeParams>();
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;


        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            eGroupType = (AkGroupType)chunk.ReadByte();
            ulGroupID = chunk.ReadUInt32();
            ulDefaultSwitch = chunk.ReadUInt32();
            bIsContinuousValidation = chunk.ReadByte();
            Children = Children.Create(chunk);

            var switchListCount = chunk.ReadUInt32();
            for (var i = 0; i < switchListCount; i++)
                SwitchList.Add(CAkSwitchPackage.Create(chunk));

            var paramCount = chunk.ReadUInt32();
            for (var i = 0; i < paramCount; i++)
                Parameters.Add(AkSwitchNodeParams.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
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

    public class CAkSwitchPackage
    {
        public uint SwitchId { get; set; }  // ID/Name of the switch case
        public List<uint> NodeIdList { get; set; } = new List<uint>(); // Probably the name of something, or at least a reference to something interesting

        public static CAkSwitchPackage Create(ByteChunk chunk)
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


        public byte bOverrideAttachmentParams { get; set; }
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
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
            node.DirectParentId = chunk.ReadUInt32();
            node.byBitVector = chunk.ReadByte();
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

        public byte bitsFXBypass { get; set; }
        public List<FXChunk> FxList { get; set; } = new List<FXChunk>();

        public static NodeInitialFxParams Create(ByteChunk chunk)
        {
            var instance = new NodeInitialFxParams();
            instance.bIsOverrideParentFX = chunk.ReadByte();
            var uNumFx = chunk.ReadByte();

            if (uNumFx != 0)
            {
                instance.bitsFXBypass = chunk.ReadByte();
                for (var i = 0; i < uNumFx; i++)
                    instance.FxList.Add(FXChunk.Create(chunk));
            }

            return instance;
        }
    }

    public class FXChunk
    {
        public byte uFXIndex { get; set; }
        public uint fxID { get; set; }
        public byte bIsShareSet { get; set; }
        public byte bIsRendered { get; set; }

        public static FXChunk Create(ByteChunk chunk)
        {
            var instance = new FXChunk();
            instance.uFXIndex = chunk.ReadByte();
            instance.fxID = chunk.ReadUInt32();
            instance.bIsShareSet = chunk.ReadByte();
            instance.bIsRendered = chunk.ReadByte();
            return instance;
        }
    }


    public class PositioningParams
    {
        public byte uByVector { get; set; }
        public byte uBits3d { get; set; }
        public uint uAttenuationID { get; set; }

        public byte ePathMode { get; set; }
        public float TransitionTime { get; set; }
        public List<AkPathVertex> VertexList { get; set; } = new List<AkPathVertex>();
        public List<AkPathListItemOffset> PlayListItems { get; set; } = new List<AkPathListItemOffset>();
        public List<Ak3DAutomationParams> Params { get; set; } = new List<Ak3DAutomationParams>();

        public static PositioningParams Create(ByteChunk chunk)
        {
            var instance = new PositioningParams();
            instance.uByVector = chunk.ReadByte();

            var bPositioningInfoOverrideParent = (instance.uByVector >> 0 & 1) == 1;
            var cbIs3DPositioningAvailable = (instance.uByVector >> 3 & 1) == 1;

            if (bPositioningInfoOverrideParent && cbIs3DPositioningAvailable)
            {
                instance.uBits3d = chunk.ReadByte();
                instance.uAttenuationID = chunk.ReadUInt32();

                if ((instance.uBits3d >> 0 & 1) == 0)
                {
                    instance.ePathMode = chunk.ReadByte();
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
        public byte byBitVector { get; set; }
        public uint auxID0 { get; set; }
        public uint auxID1 { get; set; }
        public uint auxID2 { get; set; }
        public uint auxID3 { get; set; }

        public static AuxParams Create(ByteChunk chunk)
        {
            var instance = new AuxParams();
            instance.byBitVector = chunk.ReadByte();

            if ((instance.byBitVector >> 3 & 1) == 1)
            {
                instance.auxID0 = chunk.ReadUInt32();
                instance.auxID1 = chunk.ReadUInt32();
                instance.auxID2 = chunk.ReadUInt32();
                instance.auxID3 = chunk.ReadUInt32();
            }

            return instance;
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
        public uint ulStateGroupID { get; set; }
        public byte eStateSyncType { get; set; }
        public List<AkState> States { get; set; } = new List<AkState>();

        public static AkStateGroupChunk Create(ByteChunk chunk)
        {
            var instance = new AkStateGroupChunk();
            instance.ulStateGroupID = chunk.ReadUInt32();
            instance.eStateSyncType = chunk.ReadByte();

            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.States.Add(AkState.Create(chunk));

            return instance;
        }
    }

    public class AkState
    {
        public uint ulStateID { get; set; }
        public uint ulStateInstanceID { get; set; }
        public static AkState Create(ByteChunk chunk)
        {
            var instance = new AkState();
            instance.ulStateID = chunk.ReadUInt32();
            instance.ulStateInstanceID = chunk.ReadUInt32();
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
        public byte rtpcType { get; set; }
        public byte rtpcAccum { get; set; }
        public byte ParamID { get; set; }
        public uint rtpcCurveID { get; set; }
        public byte eScaling { get; set; }
        public List<AkRTPCGraphPoint> pRTPCMgr { get; set; } = new List<AkRTPCGraphPoint>();

        public static RTPC Create(ByteChunk chunk)
        {
            var instance = new RTPC();
            instance.RTPCID = chunk.ReadUInt32();
            instance.rtpcType = chunk.ReadByte();
            instance.rtpcAccum = chunk.ReadByte();
            instance.ParamID = chunk.ReadByte();
            instance.rtpcCurveID = chunk.ReadUInt32();
            instance.eScaling = chunk.ReadByte();

            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.pRTPCMgr.Add(AkRTPCGraphPoint.Create(chunk));

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
