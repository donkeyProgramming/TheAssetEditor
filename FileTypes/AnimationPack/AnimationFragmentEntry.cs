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
        public float BlendInTime { get; set; } = 0;
        public float SelectionWeight { get; set; } = 0;
        public int Unknown0 { get; set; } = 0;
        public int Unknown1 { get; set; } = 0;
        public string Comment { get; set; } = string.Empty;
        public bool Ignore { get; set; } = false;

        public AnimationFragmentEntry(ByteChunk data)
        {
            _id = data.ReadInt32();
            _slot = data.ReadInt32();

            Slot = AnimationSlotTypeHelper.GetFromId(_slot);

            AnimationFile = data.ReadString();
            MetaDataFile = data.ReadString();
            SoundMetaDataFile = data.ReadString();
            Skeleton = data.ReadString();
            BlendInTime = data.ReadSingle();
            SelectionWeight = data.ReadSingle();
            Unknown0 = data.ReadInt32();
            Unknown1 = data.ReadInt32();
            Comment = data.ReadString();
            Ignore = data.ReadBool();
        }

        public AnimationFragmentEntry()
        { }

        public AnimationFragmentEntry Clone()
        {
            return new AnimationFragmentEntry()
            {
                Slot = Slot.Clone(),
                AnimationFile = AnimationFile,
                MetaDataFile = MetaDataFile,
                SoundMetaDataFile = SoundMetaDataFile,
                Skeleton = Skeleton,
                BlendInTime = BlendInTime,
                SelectionWeight = SelectionWeight,
                Unknown0 = Unknown0,
                Unknown1 = Unknown1,
                Comment = Comment,
                Ignore = Ignore
            };
        }


        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();

            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));

            memStream.Write(ByteParsers.String.WriteCaString(AnimationFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(MetaDataFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(SoundMetaDataFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(Skeleton.ToLower()));

            memStream.Write(ByteParsers.Single.EncodeValue(BlendInTime, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(SelectionWeight, out _));

            memStream.Write(ByteParsers.Int32.EncodeValue(Unknown0, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(Unknown1, out _));
            memStream.Write(ByteParsers.String.WriteCaString(Comment));
            memStream.Write(ByteParsers.Bool.EncodeValue(Ignore, out _));

            return memStream.ToArray();
        }

        public void SetUnknown0Flag(int index, bool value)
        {
            BitArray b = new BitArray(new int[] { Unknown0 });
            b[index] = value;
            int[] array = new int[1];
            b.CopyTo(array, 0);
            Unknown0 = array[0];
        }

        public void SetUnknown1Flag(int index, bool value)
        {
            BitArray b = new BitArray(new int[] { Unknown1 });
            b[index] = value;
            int[] array = new int[1];
            b.CopyTo(array, 0);
            Unknown1 = array[0];
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
