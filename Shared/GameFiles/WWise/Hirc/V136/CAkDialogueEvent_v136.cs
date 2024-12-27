using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise.Hirc.Shared;
using static Shared.GameFormats.WWise.Hirc.Shared.AkDecisionTree;
using static Shared.GameFormats.WWise.Hirc.Shared.ArgumentList;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    public class CAkDialogueEvent_v136 : HircItem, ICADialogEvent
    {
        public byte UProbability { get; set; }
        public uint UTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint UTreeDataSize { get; set; }
        public byte UMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }
        public List<Argument> CustomArgumentList { get; set; }
        public List<BinaryNode> CustomAkDecisionTree { get; set; }
        public byte AkPropBundle0 { get; set; }
        public byte AkPropBundle1 { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            UProbability = chunk.ReadByte();
            UTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, UTreeDepth);
            UTreeDataSize = chunk.ReadUInt32();
            UMode = chunk.ReadByte();

            AkDecisionTree = new AkDecisionTree(chunk, UTreeDepth, UTreeDataSize);

            AkPropBundle0 = chunk.ReadByte();
            AkPropBundle1 = chunk.ReadByte();
        }

        public override void UpdateSize()
        {
            Size = HircHeaderSize + 1 + 4 + (uint)CustomArgumentList.Count * 5 + 4 + 1 + UTreeDataSize + 1 + 1;
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(UProbability, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(UTreeDepth, out _));
            memStream.Write(GetCustomArgumentsAsBytes(CustomArgumentList));
            memStream.Write(ByteParsers.UInt32.EncodeValue(UTreeDataSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(UMode, out _));

            memStream.Write(GetAsBytes(CustomAkDecisionTree));

            memStream.Write(ByteParsers.Byte.EncodeValue(AkPropBundle0, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(AkPropBundle1, out _));

            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
