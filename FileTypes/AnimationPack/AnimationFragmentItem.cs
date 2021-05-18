using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace Filetypes.AnimationPack
{
    [Serializable]
    public class AnimationFragmentItem
    {
        int _id { get; set; }
        int _slot { get; set; }

        public AnimationSlotType Slot { get; set; }
        public string AnimationFile { get; set; } = string.Empty;
        public string MetaDataFile { get; set; } = string.Empty;
        public string SoundMetaDataFile { get; set; } = string.Empty;
        public string Skeleton { get; set; } = string.Empty;
        public float Blend { get; set; } = 0;
        public float Wight { get; set; } = 0;
        public int Unknown0 { get; set; } = 0;
        public int Unknown1 { get; set; } = 0;
        public string Unknown3 { get; set; } = string.Empty;
        public bool Unknown4 { get; set; } = false;

        public AnimationFragmentItem(ByteChunk data)
        {
            _id = data.ReadInt32();
            _slot = data.ReadInt32();

            Slot = AnimationSlotTypeHelper.GetFromId(_slot);

            AnimationFile = data.ReadString();
            MetaDataFile = data.ReadString();
            SoundMetaDataFile = data.ReadString();
            Skeleton = data.ReadString();
            Blend = data.ReadSingle();
            Wight = data.ReadSingle();
            Unknown0 = data.ReadInt32();
            Unknown1 = data.ReadInt32();
            Unknown3 = data.ReadString();
            Unknown4 = data.ReadBool();
        }

        public void Write(BinaryWriter writer)
        {

            writer.Write(Slot.Id);
            writer.Write(Slot.Id);

            writer.Write(ByteParsers.String.WriteCaString(AnimationFile));
            writer.Write(ByteParsers.String.WriteCaString(MetaDataFile));
            writer.Write(ByteParsers.String.WriteCaString(SoundMetaDataFile));
            writer.Write(ByteParsers.String.WriteCaString(Skeleton));

            writer.Write(Blend);
            writer.Write(Wight);

            writer.Write(Unknown0);
            writer.Write(Unknown1);
            writer.Write(ByteParsers.String.WriteCaString(Unknown3));
            //writer.Write(ByteParsers.Bool.Write(Unknown4));
        }


        public AnimationFragmentItem()
        {
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
