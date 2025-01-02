using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class NodeBaseParams_V112
    {
        public NodeInitialFxParams_V112 NodeInitialFxParams { get; set; } = new NodeInitialFxParams_V112();
        public byte OverrideAttachmentParams { get; set; }
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public byte BitVector { get; set; }
        public NodeInitialParams_V112 NodeInitialParams { get; set; } = new NodeInitialParams_V112();
        public PositioningParams_V112 PositioningParams { get; set; } = new PositioningParams_V112();
        public AuxParams_V112 AuxParams { get; set; } = new AuxParams_V112();
        public AdvSettingsParams_V112 AdvSettingsParams { get; set; } = new AdvSettingsParams_V112();
        public StateChunk_V112 StateChunk { get; set; } = new StateChunk_V112();
        public InitialRtpc_V112 InitialRtpc { get; set; } = new InitialRtpc_V112();

        public void Create(ByteChunk chunk)
        {
            NodeInitialFxParams.Create(chunk);
            OverrideAttachmentParams = chunk.ReadByte();
            OverrideBusId = chunk.ReadUInt32();
            DirectParentId = chunk.ReadUInt32();
            BitVector = chunk.ReadByte();
            NodeInitialParams.Create(chunk);
            PositioningParams.Create(chunk);
            AuxParams.Create(chunk);
            AdvSettingsParams.Create(chunk);
            StateChunk.Create(chunk);
            InitialRtpc.Create(chunk);
        }

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(NodeInitialFxParams.GetAsByteArray());
            memStream.Write(ByteParsers.Byte.EncodeValue(OverrideAttachmentParams, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(OverrideBusId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(DirectParentId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));
            memStream.Write(NodeInitialParams.GetAsByteArray());
            memStream.Write(PositioningParams.GetAsByteArray());
            memStream.Write(AuxParams.GetAsByteArray());
            memStream.Write(AdvSettingsParams.GetAsByteArray());
            memStream.Write(StateChunk.GetAsByteArray());
            memStream.Write(InitialRtpc.GetAsByteArray());
            return memStream.ToArray();
        }

        internal uint GetSize()
        {
            var overrideAttachmentSize = ByteHelper.GetPropertyTypeSize(OverrideAttachmentParams);
            var overrideBusIdSize = ByteHelper.GetPropertyTypeSize(OverrideBusId);
            var directParentId = ByteHelper.GetPropertyTypeSize(DirectParentId);
            var bitVectorId = ByteHelper.GetPropertyTypeSize(BitVector);

            return NodeInitialFxParams.GetSize() + (overrideAttachmentSize + overrideBusIdSize + directParentId + bitVectorId)
                   + NodeInitialParams.GetSize() + PositioningParams.GetSize() + AuxParams.GetSize() + AdvSettingsParams.GetSize() + InitialRtpc.GetSize();
        }
    }
}
