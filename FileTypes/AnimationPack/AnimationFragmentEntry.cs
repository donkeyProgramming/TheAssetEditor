using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace FileTypes.AnimationPack
{

    public class AnimationFragmentEntry
    {
        int _id { get; set; }
        int _slot { get; set; }

        public AnimationSlotType Slot { get; set; }
        public string AnimationFile { get; set; } = string.Empty;
        public string MetaDataFile { get; set; } = string.Empty;
        public string SoundMetaDataFile { get; set; } = string.Empty;
        public string Skeleton { get; set; } = string.Empty;
        public float Blend { get; set; } = 0;
        public float Weight { get; set; } = 0;
        public int Unknown0 { get; set; } = 0;
        public int Unknown1 { get; set; } = 0;
        public string Unknown3 { get; set; } = string.Empty;
        public bool Unknown4 { get; set; } = false;

        public AnimationFragmentEntry(ByteChunk data)
        {
            _id = data.ReadInt32();
            _slot = data.ReadInt32();

            Slot = AnimationSlotTypeHelper.GetFromId(_slot);

            AnimationFile = data.ReadString();
            MetaDataFile = data.ReadString();
            SoundMetaDataFile = data.ReadString();
            Skeleton = data.ReadString();
            Blend = data.ReadSingle();
            Weight = data.ReadSingle();
            Unknown0 = data.ReadInt32();
            Unknown1 = data.ReadInt32();
            Unknown3 = data.ReadString();
            Unknown4 = data.ReadBool();
        }

        public AnimationFragmentEntry Clone()
        {
            return new AnimationFragmentEntry()
            {
                Slot = Slot.Clone(),
                AnimationFile = AnimationFile,
                MetaDataFile = MetaDataFile,
                SoundMetaDataFile = SoundMetaDataFile,
                Skeleton = Skeleton,
                Blend = Blend,
                Weight = Weight,
                Unknown0 = Unknown0,
                Unknown1 = Unknown1,
                Unknown3 = Unknown3,
                Unknown4 = Unknown4
            };
        }

        public AnimationFragmentEntry()
        {
        }

        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();

            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));

            memStream.Write(ByteParsers.String.WriteCaString(AnimationFile));
            memStream.Write(ByteParsers.String.WriteCaString(MetaDataFile));
            memStream.Write(ByteParsers.String.WriteCaString(SoundMetaDataFile));
            memStream.Write(ByteParsers.String.WriteCaString(Skeleton));

            memStream.Write(ByteParsers.Single.EncodeValue(Blend, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(Weight, out _));

            memStream.Write(ByteParsers.Int32.EncodeValue(Unknown0, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(Unknown1, out _));
            memStream.Write(ByteParsers.String.WriteCaString(Unknown3));
            memStream.Write(ByteParsers.Bool.EncodeValue(Unknown4, out _));

            return memStream.ToArray();
        }

        public void SetWeaponFlag(int weaponIndex, bool value)
        {
            BitArray b = new BitArray(new int[] { Unknown0 });
            b[weaponIndex] = value;
            int[] array = new int[1];
            b.CopyTo(array, 0);
            Unknown0 = array[0];
        }

        int getIntFromBitArray(BitArray bitArray)
        {
            int value = 0;

            for (int i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i])
                    value += Convert.ToInt16(Math.Pow(2, i));
            }

            return value;
        }


    }
}
