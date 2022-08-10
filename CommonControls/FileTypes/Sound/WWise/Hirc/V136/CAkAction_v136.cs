using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    public class CAkAction_v136 : HircItem, ICAkAction 
    {
        public ActionType ActionType { get; set; }
        public uint idExt { get; set; }
        public byte idExt_4 { get; set; }

        public AkPropBundle AkPropBundle0 { get; set; } = new AkPropBundle();
        public AkPropBundle AkPropBundle1 { get; set; } = new AkPropBundle();
        public AkPlayActionParams AkPlayActionParams { get; set; } = new AkPlayActionParams();

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            ActionType = (ActionType)chunk.ReadUShort();
            idExt = chunk.ReadUInt32();
            idExt_4 = chunk.ReadByte();

            if (ActionType == ActionType.Play)
            {
                AkPropBundle0 = AkPropBundle.Create(chunk);
                AkPropBundle1 = AkPropBundle.Create(chunk);
                AkPlayActionParams = AkPlayActionParams.Create(chunk);
            }
        }

        public override byte[] GetAsByteArray()
        {
            if (ActionType != ActionType.Play)
                throw new Exception("Unsuported action type");

            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.UShort.EncodeValue((ushort)ActionType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(idExt, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(idExt_4, out _));
            memStream.Write(AkPropBundle0.GetAsBytes());
            memStream.Write(AkPropBundle1.GetAsBytes());
            memStream.Write(AkPlayActionParams.GetAsBytes());

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkAction_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void ComputeSize()
        {
            Size = (uint)(HircHeaderSize + 4 + 1 + AkPropBundle0.ComputeSize() + AkPropBundle1.ComputeSize() + AkPlayActionParams.ComputeSize());
        }


        public ActionType GetActionType() => ActionType;
        public uint GetChildId() => idExt;
    }

    public class AkPlayActionParams
    {
        public byte byBitVector { get; set; }
        public uint bankId { get; set; }

        public static AkPlayActionParams Create(ByteChunk chunk)
        {
            return new AkPlayActionParams()
            {
                byBitVector = chunk.ReadByte(),
                bankId = chunk.ReadUInt32(),
            };
        }

        internal uint ComputeSize()
        {
            return 5;
        }

        public byte[] GetAsBytes()
        {
            var allbytes = new List<byte>();
            allbytes.AddRange(ByteParsers.Byte.EncodeValue(byBitVector, out _));
            allbytes.AddRange(ByteParsers.UInt32.EncodeValue(bankId, out _));
            return allbytes.ToArray();
        }
    }
}