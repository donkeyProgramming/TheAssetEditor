using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class NodeBaseParams_V136
    {
        public NodeInitialFxParams_V136 NodeInitialFxParams { get; set; } = new NodeInitialFxParams_V136();
        public byte OverrideAttachmentParams { get; set; }
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public byte BitVector { get; set; }
        public NodeInitialParams_V136 NodeInitialParams { get; set; } = new NodeInitialParams_V136();
        public PositioningParams_V136 PositioningParams { get; set; } = new PositioningParams_V136();
        public AuxParams_V136 AuxParams { get; set; } = new AuxParams_V136();
        public AdvSettingsParams_V136 AdvSettingsParams { get; set; } = new AdvSettingsParams_V136();
        public StateChunk_V136 StateChunk { get; set; } = new StateChunk_V136();
        public InitialRtpc_V136 InitialRtpc { get; set; } = new InitialRtpc_V136();

        public void ReadData(ByteChunk chunk)
        {
            NodeInitialFxParams.ReadData(chunk);
            OverrideAttachmentParams = chunk.ReadByte();
            OverrideBusId = chunk.ReadUInt32();
            DirectParentId = chunk.ReadUInt32();
            BitVector = chunk.ReadByte();
            NodeInitialParams.ReadData(chunk);
            PositioningParams.ReadData(chunk);
            AuxParams.ReadData(chunk);
            AdvSettingsParams.ReadData(chunk);
            StateChunk.ReadData(chunk);
            InitialRtpc.ReadData(chunk);
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(NodeInitialFxParams.WriteData());
            memStream.Write(ByteParsers.Byte.EncodeValue(OverrideAttachmentParams, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(OverrideBusId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(DirectParentId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));
            memStream.Write(NodeInitialParams.WriteData());
            memStream.Write(PositioningParams.WriteData());
            memStream.Write(AuxParams.WriteData());
            memStream.Write(AdvSettingsParams.WriteData());
            memStream.Write(StateChunk.WriteData());
            memStream.Write(InitialRtpc.WriteData());
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var overrideAttachmentSize = ByteHelper.GetPropertyTypeSize(OverrideAttachmentParams);
            var overrideBusIdSize = ByteHelper.GetPropertyTypeSize(OverrideBusId);
            var directParentId = ByteHelper.GetPropertyTypeSize(DirectParentId);
            var bitVectorId = ByteHelper.GetPropertyTypeSize(BitVector);

            return NodeInitialFxParams.GetSize() + (overrideAttachmentSize + overrideBusIdSize + directParentId + bitVectorId)
                   + NodeInitialParams.GetSize() + PositioningParams.GetSize() + AuxParams.GetSize() + AdvSettingsParams.GetSize() + StateChunk.GetSize() + InitialRtpc.GetSize();
        }
    }
}
