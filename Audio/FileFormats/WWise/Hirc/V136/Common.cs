using Audio.BnkCompiler;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class Children
    {
        public List<uint> ChildIdList { get; set; } = new List<uint>(); // Probably the name of something, or at least a reference to something interesting

        public static Children Create(ByteChunk chunk)
        {
            var instance = new Children();
            var numChildren = chunk.ReadUInt32();
            for (int i = 0; i < numChildren; i++)
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



    public class CAkSwitchPackage : ICAkSwitchCntr.ICAkSwitchPackage
    {
        public uint SwitchId { get; set; }  // ID/Name of the switch case
        public List<uint> NodeIdList { get; set; } = new List<uint>(); // Probably the name of something, or at least a reference to something interesting

        public static CAkSwitchPackage Create(ByteChunk chunk)
        {
            var instance = new CAkSwitchPackage();
            instance.SwitchId = chunk.ReadUInt32();
            var numChildren = chunk.ReadUInt32();
            for (int i = 0; i < numChildren; i++)
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

            return node;
        }

        public static NodeBaseParams CreateDefault()
        {
            NodeBaseParams instance = new NodeBaseParams();
            instance.NodeInitialFxParams = new NodeInitialFxParams()
            {
                bIsOverrideParentFX = 0,
                FxList = new List<FXChunk>(),
                bitsFXBypass = 0,
            };
            instance.bOverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;    // "Master Audio Bus"
            instance.DirectParentID = 0;
            instance.byBitVector = 0;
            instance.NodeInitialParams = new NodeInitialParams()
            {
                AkPropBundle0 = new AkPropBundle()
                {
                    Values = new List<AkPropBundle.AkPropBundleInstance>()
                    {
                        new AkPropBundle.AkPropBundleInstance(){Type = AkPropBundleType.StatePropNum_Priority, Value = 100},
                        new AkPropBundle.AkPropBundleInstance(){Type = AkPropBundleType.UserAuxSendVolume0, Value = -96},
                        new AkPropBundle.AkPropBundleInstance(){Type = AkPropBundleType.InitialDelay, Value = 0.5199999809265137f},
                    }
                },
                AkPropBundle1 = new AkPropBundleMinMax()
                {
                    Values = new List<AkPropBundleMinMax.AkPropBundleInstance>()
                }
            };

            instance.PositioningParams = new PositioningParams()
            {
                uBitsPositioning = 0x03,
                uBits3d = 0x08
            };
            instance.AuxParams = new AuxParams()
            {
                byBitVector = 0,
                reflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams()
            {
                byBitVector = 0x00,
                eVirtualQueueBehavior = 0x01,   // [FromElapsedTime]
                u16MaxNumInstance = 0,
                eBelowThresholdBehavior = 0,
                byBitVector2 = 0
            };
            instance.StateChunk = new StateChunk();
            instance.InitialRTPC = new InitialRTPC();
            return instance;
        }

        public static NodeBaseParams CreateCustomMixerParams(ActorMixer initialParams)
        {
            var statePropNum_Priority = initialParams.StatePropNum_Priority;
            var userAuxSendVolume0 = initialParams.UserAuxSendVolume0;
            var initialDelay = initialParams.InitialDelay;
            
            // add the other params

            NodeBaseParams instance = new NodeBaseParams();
            instance.NodeInitialFxParams = new NodeInitialFxParams()
            {
                bIsOverrideParentFX = 0,
                FxList = new List<FXChunk>(),
                bitsFXBypass = 0,
            };
            instance.bOverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;    // "Master Audio Bus"
            instance.DirectParentID = 0;
            instance.byBitVector = 0;
            instance.NodeInitialParams = new NodeInitialParams()
            {
                AkPropBundle0 = new AkPropBundle()
                {
                    Values = new List<AkPropBundle.AkPropBundleInstance>()

                },
                AkPropBundle1 = new AkPropBundleMinMax()
                {
                    Values = new List<AkPropBundleMinMax.AkPropBundleInstance>()
                }
            };

            // add them in reverse order
            if (initialDelay != null)
            {
                var addStateInitialDelay = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.InitialDelay, Value = float.Parse(initialParams.InitialDelay) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(addStateInitialDelay);
            }

            if (userAuxSendVolume0 != null)
            {
                var adduserAuxSendVolume0 = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.UserAuxSendVolume0, Value = float.Parse(initialParams.UserAuxSendVolume0) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(adduserAuxSendVolume0);
            }

            if (statePropNum_Priority != null)
            {
                var addstatePropNum_Priority = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.StatePropNum_Priority, Value = float.Parse(initialParams.StatePropNum_Priority) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(addstatePropNum_Priority);
            }

            instance.PositioningParams = new PositioningParams()
            {
                uBitsPositioning = 0x03,
                uBits3d = 0x08
            };
            instance.AuxParams = new AuxParams()
            {
                byBitVector = 0,
                reflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams()
            {
                byBitVector = 0x00,
                eVirtualQueueBehavior = 0x01,   // [FromElapsedTime]
                u16MaxNumInstance = 0,
                eBelowThresholdBehavior = 0,
                byBitVector2 = 0
            };
            instance.StateChunk = new StateChunk();
            instance.InitialRTPC = new InitialRTPC();
            return instance;
        }

        public static NodeBaseParams CreateCustomSoundParams(GameSound initialParams)
        {
            var statePropNum_Priority = initialParams.StatePropNum_Priority;
            var userAuxSendVolume0 = initialParams.UserAuxSendVolume0;
            var initialDelay = initialParams.InitialDelay;

            // add the other params

            NodeBaseParams instance = new NodeBaseParams();
            instance.NodeInitialFxParams = new NodeInitialFxParams()
            {
                bIsOverrideParentFX = 0,
                FxList = new List<FXChunk>(),
                bitsFXBypass = 0,
            };
            instance.bOverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;    // "Master Audio Bus"
            instance.DirectParentID = 0;
            instance.byBitVector = 0;
            instance.NodeInitialParams = new NodeInitialParams()
            {
                AkPropBundle0 = new AkPropBundle()
                {
                    Values = new List<AkPropBundle.AkPropBundleInstance>()

                },
                AkPropBundle1 = new AkPropBundleMinMax()
                {
                    Values = new List<AkPropBundleMinMax.AkPropBundleInstance>()
                }
            };

            // add them in reverse order
            if (initialDelay != null)
            {
                var addStateInitialDelay = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.InitialDelay, Value = float.Parse(initialParams.InitialDelay) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(addStateInitialDelay);
            }

            if (userAuxSendVolume0 != null)
            {
                var adduserAuxSendVolume0 = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.UserAuxSendVolume0, Value = float.Parse(initialParams.UserAuxSendVolume0) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(adduserAuxSendVolume0);
            }

            if (statePropNum_Priority != null)
            {
                var addstatePropNum_Priority = new AkPropBundle.AkPropBundleInstance() { Type = AkPropBundleType.StatePropNum_Priority, Value = float.Parse(initialParams.StatePropNum_Priority) };
                instance.NodeInitialParams.AkPropBundle0.Values.Add(addstatePropNum_Priority);
            }

            instance.PositioningParams = new PositioningParams()
            {
                uBitsPositioning = 0x03,
                uBits3d = 0x08
            };
            instance.AuxParams = new AuxParams()
            {
                byBitVector = 0,
                reflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams()
            {
                byBitVector = 0x00,
                eVirtualQueueBehavior = 0x01,   // [FromElapsedTime]
                u16MaxNumInstance = 0,
                eBelowThresholdBehavior = 0,
                byBitVector2 = 0
            };
            instance.StateChunk = new StateChunk();
            instance.InitialRTPC = new InitialRTPC();
            return instance;
        }

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(NodeInitialFxParams.GetAsByteArray());

            memStream.Write(ByteParsers.Byte.EncodeValue(bOverrideAttachmentParams, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(OverrideBusId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(DirectParentID, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(byBitVector, out _));

            memStream.Write(NodeInitialParams.GetAsByteArray());
            memStream.Write(PositioningParams.GetAsByteArray());
            memStream.Write(AuxParams.GetAsByteArray());
            memStream.Write(AdvSettingsParams.GetAsByteArray());
            memStream.Write(StateChunk.GetAsByteArray());
            memStream.Write(InitialRTPC.GetAsByteArray());


            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }

        internal uint GetSize()
        {
            return NodeInitialFxParams.GetSize() + 10 + NodeInitialParams.GetSize() + PositioningParams.GetSize() + AuxParams.GetSize() + AdvSettingsParams.GetSize() + StateChunk.GetSize() + InitialRTPC.GetSize();
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
            public float Value { get; set; }
        }

        public List<AkPropBundleInstance> Values { get; set; } = new List<AkPropBundleInstance>();

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
                memStream.Write(ByteParsers.Single.EncodeValue(v.Value, out _));

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

        public List<AkPropBundleInstance> Values { get; set; } = new List<AkPropBundleInstance>();

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
                for (int i = 0; i < uNumFx; i++)
                    instance.FxList.Add(FXChunk.Create(chunk));
            }

            return instance;
        }

        public uint GetSize() => 2;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(bIsOverrideParentFX, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(bitsFXBypass, out _));
            return memStream.ToArray();
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
        public byte uBitsPositioning { get; set; }
        public byte uBits3d { get; set; }
        public byte ePathMode { get; set; }
        public float TransitionTime { get; set; }



        public List<AkPathVertex> VertexList { get; set; } = new List<AkPathVertex>();
        public List<AkPathListItemOffset> PlayListItems { get; set; } = new List<AkPathListItemOffset>();
        public List<Ak3DAutomationParams> Params { get; set; } = new List<Ak3DAutomationParams>();

        public static PositioningParams Create(ByteChunk chunk)
        {
            var instance = new PositioningParams();
            instance.uBitsPositioning = chunk.ReadByte();

            var has_positioning = (instance.uBitsPositioning >> 0 & 1) == 1;
            var has_3d = (instance.uBitsPositioning >> 1 & 1) == 1;

            if (has_positioning && has_3d)
            {
                instance.uBits3d = chunk.ReadByte();

                var e3DPositionType = instance.uBitsPositioning >> 5 & 3;
                var has_automation = e3DPositionType != 0;

                if (has_automation)
                {
                    instance.ePathMode = chunk.ReadByte();
                    instance.TransitionTime = chunk.ReadSingle();

                    var numVertexes = chunk.ReadUInt32();
                    for (int i = 0; i < numVertexes; i++)
                        instance.VertexList.Add(AkPathVertex.Create(chunk));

                    var numPlayListItems = chunk.ReadUInt32();
                    for (int i = 0; i < numPlayListItems; i++)
                        instance.PlayListItems.Add(AkPathListItemOffset.Create(chunk));

                    for (int i = 0; i < numPlayListItems; i++)
                        instance.Params.Add(Ak3DAutomationParams.Create(chunk));
                }
            }

            return instance;
        }

        public uint GetSize()
        {
            if (uBitsPositioning != 0x03 && uBits3d != 0x08)
                throw new NotImplementedException();
            return 2;
        }

        public byte[] GetAsByteArray()
        {
            if (uBitsPositioning != 0x03 && uBits3d != 0x08)
                throw new NotImplementedException();

            return new byte[] { 0x03, 0x08 };
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
        public uint reflectionsAuxBus { get; set; }

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

            instance.reflectionsAuxBus = chunk.ReadUInt32();

            return instance;
        }


        public uint GetSize() => 5;

        public byte[] GetAsByteArray()
        {
            if (byBitVector != 0)
                throw new NotImplementedException();

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(byBitVector, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(reflectionsAuxBus, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
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
            var node = new AdvSettingsParams();

            node.byBitVector = chunk.ReadByte();
            node.eVirtualQueueBehavior = chunk.ReadByte();
            node.u16MaxNumInstance = chunk.ReadUShort();
            node.eBelowThresholdBehavior = chunk.ReadByte();
            node.byBitVector2 = chunk.ReadByte();

            return node;
        }

        public uint GetSize() => 6;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(byBitVector, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(eVirtualQueueBehavior, out _));
            memStream.Write(ByteParsers.UShort.EncodeValue((byte)u16MaxNumInstance, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(eBelowThresholdBehavior, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(byBitVector2, out _));

            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }
    }


    public class StateChunk
    {
        public List<AkStateGroupChunk> StateChunks { get; set; } = new List<AkStateGroupChunk>();
        public List<AkStatePropertyInfo> StateProps { get; set; } = new List<AkStatePropertyInfo>();

        public static StateChunk Create(ByteChunk chunk)
        {
            var instance = new StateChunk();
            var numStateProps = chunk.ReadByte();
            for (int i = 0; i < numStateProps; i++)
                instance.StateProps.Add(AkStatePropertyInfo.Create(chunk));

            var numStateGroups = chunk.ReadByte();
            for (int i = 0; i < numStateGroups; i++)
                instance.StateChunks.Add(AkStateGroupChunk.Create(chunk));
            return instance;
        }

        public uint GetSize() => 2;

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
        public uint ulStateGroupID { get; set; }
        public byte eStateSyncType { get; set; }
        public List<AkState> States { get; set; } = new List<AkState>();

        public static AkStateGroupChunk Create(ByteChunk chunk)
        {
            var instance = new AkStateGroupChunk();
            instance.ulStateGroupID = chunk.ReadUInt32();
            instance.eStateSyncType = chunk.ReadByte();

            var count = chunk.ReadByte();
            for (int i = 0; i < count; i++)
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
            for (int i = 0; i < count; i++)
                instance.RTPCList.Add(RTPC.Create(chunk));

            return instance;
        }


        public uint GetSize() => 2;

        public byte[] GetAsByteArray()
        {
            if (RTPCList.Count != 0)
                throw new NotImplementedException();

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)RTPCList.Count, out _));
            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
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
            for (int i = 0; i < count; i++)
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

    public class AkStatePropertyInfo
    {
        public byte PropertyId { get; set; }
        public byte accumType { get; set; }
        public byte inDb { get; set; }

        public static AkStatePropertyInfo Create(ByteChunk chunk)
        {
            var instance = new AkStatePropertyInfo();
            instance.PropertyId = chunk.ReadByte();
            instance.accumType = chunk.ReadByte();
            instance.inDb = chunk.ReadByte();
            return instance;
        }
    }
}
