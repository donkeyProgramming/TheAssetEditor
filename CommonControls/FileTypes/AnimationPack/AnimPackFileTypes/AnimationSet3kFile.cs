using CommonControls.Common;
using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CommonControls.FileTypes.AnimationPack.AnimPackFileTypes
{
    public class AnimationSet3kFile : IAnimationPackFile
    {
        public AnimationPackFile Parent { get; set; }
        public string FileName { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        public List<AnimationSetEntry> Entries { get; set; } = new List<AnimationSetEntry>();

        public AnimationSet3kFile(string fileName, byte[] bytes)
        {
            FileName = fileName;
            if (bytes != null)
                CreateFromBytes(bytes);
        }

        public uint Version { get; set; }
        public string MountFragment { get; set; }
        public string MountSkeleton { get; set; }
        public string FragmentName { get; set; }
        public string SkeletonName { get; set; }
        public bool IsSimpleFlight { get; set; }

        public void CreateFromBytes(byte[] bytes)
        {
                var chunk = new ByteChunk(bytes);

                Version = chunk.ReadUInt32();
                MountFragment = chunk.ReadString();
                MountSkeleton = chunk.ReadString();
                FragmentName = chunk.ReadString();
                SkeletonName = chunk.ReadString();
                IsSimpleFlight = chunk.ReadBool();

                Entries = new List<AnimationSetEntry>();
                var count = chunk.ReadUInt32();
                for (int i = 0; i < count; i++)
                    Entries.Add(new AnimationSetEntry(chunk));

                if (chunk.BytesLeft != 0)
                    throw new Exception("More data in stream - AnimationSet3kFile");
        }

        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue(Version, out _));
            memStream.Write(ByteParsers.String.EncodeValue(MountFragment, out _));
            memStream.Write(ByteParsers.String.EncodeValue(MountSkeleton, out _));
            memStream.Write(ByteParsers.String.EncodeValue(FragmentName, out _));
            memStream.Write(ByteParsers.String.EncodeValue(SkeletonName, out _));
            memStream.Write(ByteParsers.Bool.EncodeValue(IsSimpleFlight, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)Entries.Count, out _));
            foreach (var entry in Entries)
                memStream.Write(entry.ToBytes());

            return memStream.ToArray();
        }


        public class AnimationSetEntry
        {
            public uint Slot { get; set; }
            public float BlendWeight { get; set; }
            public float SelectionWeight { get; set; }
            public int WeaponBone { get; set; }
            public bool Flag { get; set; }  // SingleFrameVariant, Disabled or Ignore, always 0?
            public List<AnimationEntry> Animations { get; set; } = new List<AnimationEntry>();

            public class AnimationEntry
            {
                public string AnimationFile { get; set; }
                public string MetaFile { get; set; }
                public string SoundMeta { get; set; }
            }
            public AnimationSetEntry()
            { }

            public AnimationSetEntry(ByteChunk chunk)
            {
                Slot = chunk.ReadUInt32();

                BlendWeight = chunk.ReadSingle();
                SelectionWeight = chunk.ReadSingle();
                WeaponBone = chunk.ReadInt32();
                Flag = chunk.ReadBool();
                var itemCount = chunk.ReadUInt32();

                for (int i = 0; i < itemCount; i++)
                {
                    Animations.Add(new AnimationEntry()
                    {
                        AnimationFile = chunk.ReadString(),
                        MetaFile = chunk.ReadString(),
                        SoundMeta = chunk.ReadString(),
                    });
                }
            }

            public byte[] ToBytes()
            {
                using var memStream = new MemoryStream();

                memStream.Write(ByteParsers.UInt32.EncodeValue(Slot, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(BlendWeight, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(SelectionWeight, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(WeaponBone, out _));
                memStream.Write(ByteParsers.Bool.EncodeValue(Flag, out _));

                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)Animations.Count, out _));
                foreach (var entry in Animations)
                {
                    memStream.Write(ByteParsers.String.EncodeValue(entry.AnimationFile, out _));
                    memStream.Write(ByteParsers.String.EncodeValue(entry.MetaFile, out _));
                    memStream.Write(ByteParsers.String.EncodeValue(entry.SoundMeta, out _));
                }

                    return memStream.ToArray();
            }

            public void SetWeaponBoneFlags(int index, bool value)
            {
                BitArray b = new BitArray(new int[] { WeaponBone });
                b[index] = value;
                int[] array = new int[1];
                b.CopyTo(array, 0);
                WeaponBone = array[0];
            }
        }
    }
}
