using Audio.FileFormats.WWise.Hirc.Shared;
using Shared.Core.ByteParsing;
using System;
using static Audio.FileFormats.WWise.Hirc.Shared.AkDecisionTree;
using static Audio.FileFormats.WWise.Hirc.Shared.ArgumentList;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkDialogueEvent_v136 : HircItem, ICADialogEvent
    {
        public byte uProbability { get; set; }
        public uint uTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint uTreeDataSize { get; set; }
        public byte uMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }
        public List<Argument> CustomArgumentList { get; set; }
        public List<BinaryNode> CustomAkDecisionTree { get; set; }
        public byte AkPropBundle0 { get; set; }
        public byte AkPropBundle1 { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            uProbability = chunk.ReadByte();
            uTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, uTreeDepth);
            uTreeDataSize = chunk.ReadUInt32();
            uMode = chunk.ReadByte();

            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);

            AkPropBundle0 = chunk.ReadByte();
            AkPropBundle1 = chunk.ReadByte();
        }

        public override void UpdateSize()
        {
            Size = HircHeaderSize + 1 + 4 + ((uint)CustomArgumentList.Count * 5) + 4 + 1 + uTreeDataSize + 1 + 1;
        }
        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(uProbability, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(uTreeDepth, out _));
            memStream.Write(ArgumentList.GetCustomArgumentsAsBytes(CustomArgumentList));
            memStream.Write(ByteParsers.UInt32.EncodeValue(uTreeDataSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(uMode, out _));

            memStream.Write(AkDecisionTree.GetAsBytes(CustomAkDecisionTree));

            memStream.Write(ByteParsers.Byte.EncodeValue(AkPropBundle0, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(AkPropBundle1, out _));

            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
