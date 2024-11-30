using System.Collections;
using System.Diagnostics;
using Shared.Core.ByteParsing;
using Shared.Core.Misc;
using Shared.Core.Settings;
using Shared.GameFormats.DB;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes
{
    [DebuggerDisplay("AnimationFragmentFile - {FileName}")]
    public class AnimationFragmentFile : IAnimationPackFile
    {
        GameTypeEnum _preferedGame = GameTypeEnum.Warhammer2;

        public string FileName { get; set; }
        public AnimationPackFile Parent { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        public StringArrayTable Skeletons { get; set; } = new StringArrayTable();
        public int MinSlotId { get; set; }
        public int MaxSlotId { get; set; }
        public List<AnimationSetEntry> Fragments { get; set; } = new List<AnimationSetEntry>();



        public AnimationFragmentFile() { }
        public AnimationFragmentFile(string fileName, byte[] bytes, GameTypeEnum preferedGame)
        {
            FileName = fileName;
            if (bytes != null)
                CreateFromBytes(bytes);
        }

        public void CreateFromBytes(byte[] bytes)
        {
            var data = new ByteChunk(bytes);

            Skeletons = new StringArrayTable(data);
            MinSlotId = data.ReadInt32();
            MaxSlotId = data.ReadInt32();
            var numFragItems = data.ReadInt32();

            Fragments.Clear();
            for (var i = 0; i < numFragItems; i++)
                Fragments.Add(new AnimationSetEntry(data, _preferedGame));
        }

        public byte[] ToByteArray()
        {
            MinSlotId = 0;
            MaxSlotId = 0;
            if (Fragments.Count != 0)
            {
                MinSlotId = Fragments.Min(x => x.Slot.Id);
                MaxSlotId = Fragments.Max(x => x.Slot.Id);
            }

            Fragments = Fragments.OrderBy(x => x.Slot.Id).ToList();
            foreach (var fragment in Fragments)
            {
                fragment.AnimationFile = fragment.AnimationFile.Replace("\\", "/").ToLower();
                fragment.MetaDataFile = fragment.MetaDataFile.Replace("\\", "/").ToLower();
                fragment.SoundMetaDataFile = fragment.SoundMetaDataFile.Replace("\\", "/").ToLower();
            }

            // Save
            using var memStream = new MemoryStream();
            memStream.Write(Skeletons.ToByteArray());

            memStream.Write(ByteParsers.Int32.EncodeValue(MinSlotId, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(MaxSlotId, out _));

            memStream.Write(ByteParsers.Int32.EncodeValue(Fragments.Count, out _));
            foreach (var fragment in Fragments)
                memStream.Write(fragment.ToByteArray());

            return memStream.ToArray();
        }
    }

    public class AnimationSetEntry
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
        public int WeaponBone { get; set; } = 0;
        public string Comment { get; set; } = string.Empty;
        public bool Ignore { get; set; } = false;

        public AnimationSetEntry(ByteChunk data, GameTypeEnum preferedGame)
        {
            _id = data.ReadInt32();
            _slot = data.ReadInt32();

            if (preferedGame == GameTypeEnum.Troy)
                Slot = DefaultAnimationSlotTypeHelper.GetFromId(_slot);
            else
                Slot = AnimationSlotTypeHelperTroy.GetFromId(_slot);

            AnimationFile = data.ReadString();
            MetaDataFile = data.ReadString();
            SoundMetaDataFile = data.ReadString();
            Skeleton = data.ReadString();
            BlendInTime = data.ReadSingle();
            SelectionWeight = data.ReadSingle();
            Unknown0 = data.ReadInt32();
            WeaponBone = data.ReadInt32();
            Comment = data.ReadString();
            Ignore = data.ReadBool();
        }

        public AnimationSetEntry()
        { }

        public AnimationSetEntry Clone()
        {
            return new AnimationSetEntry()
            {
                Slot = Slot.Clone(),
                AnimationFile = AnimationFile,
                MetaDataFile = MetaDataFile,
                SoundMetaDataFile = SoundMetaDataFile,
                Skeleton = Skeleton,
                BlendInTime = BlendInTime,
                SelectionWeight = SelectionWeight,
                Unknown0 = Unknown0,
                WeaponBone = WeaponBone,
                Comment = Comment,
                Ignore = Ignore
            };
        }


        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(Slot.Id, out _));

            memStream.Write(ByteParsers.String.WriteCaString(AnimationFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(MetaDataFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(SoundMetaDataFile.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(Skeleton.ToLower()));

            memStream.Write(ByteParsers.Single.EncodeValue(BlendInTime, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(SelectionWeight, out _));

            memStream.Write(ByteParsers.Int32.EncodeValue(Unknown0, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(WeaponBone, out _));
            memStream.Write(ByteParsers.String.WriteCaString(Comment));
            memStream.Write(ByteParsers.Bool.EncodeValue(Ignore, out _));

            return memStream.ToArray();
        }

        public void SetWeaponBoneFlags(int index, bool value)
        {
            var b = new BitArray(new int[] { WeaponBone });
            b[index] = value;
            var array = new int[1];
            b.CopyTo(array, 0);
            WeaponBone = array[0];
        }

        int getIntFromBitArray(BitArray bitArray)
        {
            var value = 0;

            for (var i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i])
                    value += Convert.ToInt16(Math.Pow(2, i));
            }

            return value;
        }


    }
}
