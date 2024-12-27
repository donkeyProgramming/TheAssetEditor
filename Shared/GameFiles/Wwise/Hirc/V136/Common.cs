using Shared.Core.ByteParsing;
using static Shared.GameFormats.Wwise.Hirc.ICAkSwitchCntr;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class Children
    {
        public List<uint> ChildIdList { get; set; } = [];

        public static Children Create(ByteChunk chunk)
        {
            var instance = new Children();
            var numChildren = chunk.ReadUInt32();
            for (var i = 0; i < numChildren; i++)
                instance.ChildIdList.Add(chunk.ReadUInt32());
            return instance;
        }

        public uint GetSize()
        {
            return (uint)(ChildIdList.Count * 4 + 4);
        }

        internal ReadOnlySpan<byte> GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)ChildIdList.Count, out _));

            foreach (var child in ChildIdList)
                memStream.Write(ByteParsers.UInt32.EncodeValue(child, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }
    }

    public class CAkSwitchPackage : ICAkSwitchPackage
    {
        public uint SwitchId { get; set; }
        public List<uint> NodeIdList { get; set; } = [];

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
        public InitialRtpc InitialRtpc { get; set; }
        public byte EGroupType { get; set; }
        public uint UlGroupId { get; set; }
        public uint UlDefaultSwitch { get; set; }
        public byte BIsContinuousValidation { get; set; }

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
            node.InitialRtpc = InitialRtpc.Create(chunk);
            return node;
        }

        public static NodeBaseParams CreateDefault()
        {
            var instance = new NodeBaseParams();
            instance.NodeInitialFxParams = new NodeInitialFxParams()
            {
                BIsOverrideParentFX = 0,
                FxList = new List<FXChunk>(),
                BitsFXBypass = 0,
            };
            instance.BOverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;
            instance.DirectParentId = 0;
            instance.ByBitVector = 0;
            instance.NodeInitialParams = new NodeInitialParams()
            {
                AkPropBundle0 = new AkPropBundle()
                {
                    Values = new List<AkPropBundle.AkPropBundleInstance>()
                    {
                        //new(){Type = AkPropBundleType.StatePropNum_Priority, Value = 100},
                        //new(){Type = AkPropBundleType.UserAuxSendVolume0, Value = -96},
                        //new(){Type = AkPropBundleType.InitialDelay, Value = 0.5199999809265137f},
                    }
                },
                AkPropBundle1 = new AkPropBundleMinMax()
                {
                    Values = new List<AkPropBundleMinMax.AkPropBundleInstance>()
                }
            };
            instance.PositioningParams = new PositioningParams()
            {
                UBitsPositioning = 0x03,
                UBits3d = 0x08
            };
            instance.AuxParams = new AuxParams()
            {
                ByBitVector = 0,
                ReflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams()
            {
                ByBitVector = 0x00,
                EVirtualQueueBehavior = 0x01,
                U16MaxNumInstance = 0,
                EBelowThresholdBehavior = 0,
                ByBitVector2 = 0
            };
            instance.StateChunk = new StateChunk();
            instance.InitialRtpc = new InitialRtpc();
            return instance;
        }

        public static NodeBaseParams CreateDefaultRandomContainer()
        {
            var instance = new NodeBaseParams();
            instance.NodeInitialFxParams = new NodeInitialFxParams()
            {
                BIsOverrideParentFX = 0,
                FxList = new List<FXChunk>(),
                BitsFXBypass = 0,
            };
            instance.BOverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;
            instance.DirectParentId = 0;
            instance.ByBitVector = 0x02;
            instance.NodeInitialParams = new NodeInitialParams()
            {
                AkPropBundle0 = new AkPropBundle()
                {
                    Values = new List<AkPropBundle.AkPropBundleInstance>()
                    {
                    }
                },
                AkPropBundle1 = new AkPropBundleMinMax()
                {
                    Values = new List<AkPropBundleMinMax.AkPropBundleInstance>()
                }
            };

            instance.PositioningParams = new PositioningParams()
            {
                UBitsPositioning = 0x00,
            };
            instance.AuxParams = new AuxParams()
            {
                ByBitVector = 0,
                ReflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams()
            {
                ByBitVector = 0x00,
                EVirtualQueueBehavior = 0x01,
                U16MaxNumInstance = 0,
                EBelowThresholdBehavior = 0x02,
                ByBitVector2 = 0
            };
            instance.StateChunk = new StateChunk();
            instance.InitialRtpc = new InitialRtpc();
            return instance;
        }

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(NodeInitialFxParams.GetAsByteArray());

            memStream.Write(ByteParsers.Byte.EncodeValue(BOverrideAttachmentParams, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(OverrideBusId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(DirectParentId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(ByBitVector, out _));

            memStream.Write(NodeInitialParams.GetAsByteArray());
            memStream.Write(PositioningParams.GetAsByteArray());
            memStream.Write(AuxParams.GetAsByteArray());
            memStream.Write(AdvSettingsParams.GetAsByteArray());
            memStream.Write(StateChunk.GetAsByteArray());
            memStream.Write(InitialRtpc.GetAsByteArray());

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }

        internal uint GetSize()
        {
            return NodeInitialFxParams.GetSize() + 10 + NodeInitialParams.GetSize() + PositioningParams.GetSize() + AuxParams.GetSize() + AdvSettingsParams.GetSize() + StateChunk.GetSize() + InitialRtpc.GetSize();
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

        public uint GetSize() => AkPropBundle0.GetSize() + AkPropBundle1.GetSize();

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AkPropBundle0.GetAsBytes());
            memStream.Write(AkPropBundle1.GetAsBytes());
            return memStream.ToArray();
        }
    }

    public class AkPropBundle
    {
        public class AkPropBundleInstance
        {
            public AkPropBundleType Type { get; set; }
            public uint Value { get; set; }
        }

        public List<AkPropBundleInstance> Values { get; set; } = [];

        public static AkPropBundle Create(ByteChunk chunk)
        {
            var output = new AkPropBundle();
            var propsCount = chunk.ReadByte();

            for (byte i = 0; i < propsCount; i++)
                output.Values.Add(new AkPropBundleInstance() { Type = (AkPropBundleType)chunk.ReadByte() });

            for (byte i = 0; i < propsCount; i++)
                output.Values[i].Value = chunk.ReadUInt32();

            return output;
        }

        public uint GetSize()
        {
            return (uint)Values.Count * 5 + 1;
        }

        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)Values.Count, out _));
            foreach (var v in Values)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)v.Type, out _));

            foreach (var v in Values)
                memStream.Write(ByteParsers.UInt32.EncodeValue(v.Value, out _));

            return memStream.ToArray();
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

        public List<AkPropBundleInstance> Values { get; set; } = [];

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

        public uint GetSize() => 1 + (uint)(Values.Count * 9);

        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)Values.Count, out _));
            foreach (var value in Values)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)value.Type, out _));

            foreach (var value in Values)
            {
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Min, out _));
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Max, out _));
            }

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
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

        public static uint GetSize() => 2;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(BIsOverrideParentFX, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BitsFXBypass, out _));
            return memStream.ToArray();
        }
    }

    public class FXChunk
    {
        public byte UFxIndex { get; set; }
        public uint FxId { get; set; }
        public byte BIsShareSet { get; set; }
        public byte BIsRendered { get; set; }

        public static FXChunk Create(ByteChunk chunk)
        {
            var instance = new FXChunk();
            instance.UFxIndex = chunk.ReadByte();
            instance.FxId = chunk.ReadUInt32();
            instance.BIsShareSet = chunk.ReadByte();
            instance.BIsRendered = chunk.ReadByte();
            return instance;
        }
    }

    public class PositioningParams
    {
        public byte UBitsPositioning { get; set; }
        public byte UBits3d { get; set; }
        public byte EPathMode { get; set; }
        public float TransitionTime { get; set; }
        public List<AkPathVertex> VertexList { get; set; } = [];
        public List<AkPathListItemOffset> PlayListItems { get; set; } = [];
        public List<Ak3DAutomationParams> Params { get; set; } = [];

        public static PositioningParams Create(ByteChunk chunk)
        {
            var instance = new PositioningParams();
            instance.UBitsPositioning = chunk.ReadByte();

            var has_positioning = (instance.UBitsPositioning >> 0 & 1) == 1;
            var has_3d = (instance.UBitsPositioning >> 1 & 1) == 1;

            if (has_positioning && has_3d)
            {
                instance.UBits3d = chunk.ReadByte();

                var e3DPositionType = instance.UBitsPositioning >> 5 & 3;
                var has_automation = e3DPositionType != 0;

                if (has_automation)
                {
                    instance.EPathMode = chunk.ReadByte();
                    instance.TransitionTime = chunk.ReadSingle();

                    var numVertexes = chunk.ReadUInt32();
                    for (var i = 0; i < numVertexes; i++)
                        instance.VertexList.Add(AkPathVertex.Create(chunk));

                    var numPlayListItems = chunk.ReadUInt32();
                    for (var i = 0; i < numPlayListItems; i++)
                        instance.PlayListItems.Add(AkPathListItemOffset.Create(chunk));

                    for (var i = 0; i < numPlayListItems; i++)
                        instance.Params.Add(Ak3DAutomationParams.Create(chunk));
                }
            }

            return instance;
        }

        public uint GetSize()
        {
            if (UBitsPositioning == 0x03 && UBits3d == 0x08)
                return 2;
            else if (UBitsPositioning == 0x00)
                return 1;
            else
                throw new NotImplementedException();
        }

        public byte[] GetAsByteArray()
        {
            if (UBitsPositioning == 0x03 && UBits3d == 0x08)
                return [0x03, 0x08];
            else if (UBitsPositioning == 0x00)
                return [0x00];
            else
                throw new NotImplementedException();
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
        public uint UlVerticesOffset { get; set; }
        public uint INumVertices { get; set; }

        public static AkPathListItemOffset Create(ByteChunk chunk)
        {
            var instance = new AkPathListItemOffset();
            instance.UlVerticesOffset = chunk.ReadUInt32();
            instance.INumVertices = chunk.ReadUInt32();
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
        public uint AuxBus0 { get; set; }
        public uint AuxBus1 { get; set; }
        public uint AuxBus2 { get; set; }
        public uint AuxBus3 { get; set; }
        public uint ReflectionsAuxBus { get; set; }

        public static AuxParams Create(ByteChunk chunk)
        {
            var instance = new AuxParams();
            instance.ByBitVector = chunk.ReadByte();

            if ((instance.ByBitVector >> 3 & 1) == 1)
            {
                instance.AuxBus0 = chunk.ReadUInt32();
                instance.AuxBus1 = chunk.ReadUInt32();
                instance.AuxBus2 = chunk.ReadUInt32();
                instance.AuxBus3 = chunk.ReadUInt32();
            }

            instance.ReflectionsAuxBus = chunk.ReadUInt32();

            return instance;
        }

        public static uint GetSize() => 5;

        public byte[] GetAsByteArray()
        {
            if (ByBitVector != 0)
                throw new NotImplementedException();

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(ByBitVector, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(ReflectionsAuxBus, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
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
            var node = new AdvSettingsParams();
            node.ByBitVector = chunk.ReadByte();
            node.EVirtualQueueBehavior = chunk.ReadByte();
            node.U16MaxNumInstance = chunk.ReadUShort();
            node.EBelowThresholdBehavior = chunk.ReadByte();
            node.ByBitVector2 = chunk.ReadByte();
            return node;
        }

        public static uint GetSize() => 6;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(ByBitVector, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(EVirtualQueueBehavior, out _));
            memStream.Write(ByteParsers.UShort.EncodeValue((byte)U16MaxNumInstance, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(EBelowThresholdBehavior, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(ByBitVector2, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }
    }

    public class StateChunk
    {
        public List<AkStateGroupChunk> StateChunks { get; set; } = [];
        public List<AkStatePropertyInfo> StateProps { get; set; } = [];

        public static StateChunk Create(ByteChunk chunk)
        {
            var instance = new StateChunk();
            var numStateProps = chunk.ReadByte();
            for (var i = 0; i < numStateProps; i++)
                instance.StateProps.Add(AkStatePropertyInfo.Create(chunk));

            var numStateGroups = chunk.ReadByte();
            for (var i = 0; i < numStateGroups; i++)
                instance.StateChunks.Add(AkStateGroupChunk.Create(chunk));
            return instance;
        }

        public static uint GetSize() => 2;

        public byte[] GetAsByteArray()
        {
            if (StateChunks.Count != 0 || StateProps.Count != 0)
                throw new NotImplementedException();

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateChunks.Count, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateProps.Count, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }
    }

    public class AkStateGroupChunk
    {
        public uint UlStateGroupId { get; set; }
        public byte EStateSyncType { get; set; }
        public List<AkState> States { get; set; } = [];

        public static AkStateGroupChunk Create(ByteChunk chunk)
        {
            var instance = new AkStateGroupChunk();
            instance.UlStateGroupId = chunk.ReadUInt32();
            instance.EStateSyncType = chunk.ReadByte();

            var count = chunk.ReadByte();
            for (var i = 0; i < count; i++)
                instance.States.Add(AkState.Create(chunk));

            return instance;
        }
    }

    public class AkState
    {
        public uint UlStateId { get; set; }
        public uint UlStateInstanceId { get; set; }
        public static AkState Create(ByteChunk chunk)
        {
            var instance = new AkState();
            instance.UlStateId = chunk.ReadUInt32();
            instance.UlStateInstanceId = chunk.ReadUInt32();
            return instance;
        }
    }

    public class InitialRtpc
    {
        public List<Rtpc> RtpcList { get; set; } = [];

        public static InitialRtpc Create(ByteChunk chunk)
        {
            var instance = new InitialRtpc();
            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.RtpcList.Add(Rtpc.Create(chunk));

            return instance;
        }

        public static uint GetSize() => 2;

        public byte[] GetAsByteArray()
        {
            if (RtpcList.Count != 0)
                throw new NotImplementedException();

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)RtpcList.Count, out _));
            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }
    }

    public class Rtpc
    {
        public uint RtpcId { get; set; }
        public byte RtpcType { get; set; }
        public byte RtpcAccum { get; set; }
        public byte ParamId { get; set; }
        public uint RtpcCurveId { get; set; }
        public byte EScaling { get; set; }
        public List<AkRtpcGraphPoint> PRtpcMgr { get; set; } = [];

        public static Rtpc Create(ByteChunk chunk)
        {
            var instance = new Rtpc();
            instance.RtpcId = chunk.ReadUInt32();
            instance.RtpcType = chunk.ReadByte();
            instance.RtpcAccum = chunk.ReadByte();
            instance.ParamId = chunk.ReadByte();
            instance.RtpcCurveId = chunk.ReadUInt32();
            instance.EScaling = chunk.ReadByte();

            var count = chunk.ReadUShort();
            for (var i = 0; i < count; i++)
                instance.PRtpcMgr.Add(AkRtpcGraphPoint.Create(chunk));

            return instance;
        }
    }

    public class AkRtpcGraphPoint
    {
        public float From { get; set; }
        public float To { get; set; }
        public uint Interp { get; set; }

        public static AkRtpcGraphPoint Create(ByteChunk chunk)
        {
            var instance = new AkRtpcGraphPoint();
            instance.From = chunk.ReadSingle();
            instance.To = chunk.ReadSingle();
            instance.Interp = chunk.ReadUInt32();
            return instance;
        }
    }

    public class AkStatePropertyInfo
    {
        public byte PropertyId { get; set; }
        public byte AccumType { get; set; }
        public byte InDb { get; set; }

        public static AkStatePropertyInfo Create(ByteChunk chunk)
        {
            var instance = new AkStatePropertyInfo();
            instance.PropertyId = chunk.ReadByte();
            instance.AccumType = chunk.ReadByte();
            instance.InDb = chunk.ReadByte();
            return instance;
        }
    }
}
