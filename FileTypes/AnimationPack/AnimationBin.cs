using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.AnimationPack
{
    public class AnimationBin
    {
        public int TableVersion { get; set; } = 2;
        public int RowCount { get; set; } = 0;
        public string FileName { get; set; }

        public List<AnimationBinEntry> AnimationTableEntries { get; set; } = new List<AnimationBinEntry>();
       

        public AnimationBin(string filename, ByteChunk data)
        {
            FileName = filename;
            TableVersion = data.ReadInt32();
            RowCount = data.ReadInt32();
            AnimationTableEntries = new List<AnimationBinEntry>(RowCount);
            for (int i = 0; i < RowCount; i++)
                AnimationTableEntries.Add(new AnimationBinEntry(data));
        }
    }

    public class AnimationBinEntry
    {
        public class FragmentReference
        {
            public string Name { get; set; }
            public int Unknown { get; set; }

            public override string ToString()
            {
                return $"{Name}, Unk = {Unknown}";
            }
        }

        public string Name { get; set; }
        public string SkeletonName { get; set; }
        public string MountName { get; set; }

        public List<FragmentReference> FragmentReferences { get; set; } = new List<FragmentReference>();
        public short Unknown0 { get; set; }
        public short Unknown1 { get; set; }

        public AnimationBinEntry(ByteChunk data)
        {
            Name = data.ReadString();
            SkeletonName = data.ReadString();
            MountName = data.ReadString();

            LoadAnimationSets(data);
        }

        void LoadAnimationSets(ByteChunk data)
        {
            var count = data.ReadInt32();
           // Unknown0 = data.ReadShort();
            for (int i = 0; i < count; i++)
            {
                var animationSet = new FragmentReference()
                {
                    Name = data.ReadString(),
                    Unknown = data.ReadInt32()
                };
                FragmentReferences.Add(animationSet);
            }
            Unknown1 = data.ReadShort();
        }

        public override string ToString()
        {
            var str = $"{Name}, Skeleton = {SkeletonName}";
            if (MountName.Length != 0)
                str += $", Mount = {MountName}";
            str += $", AnimationSets = {FragmentReferences.Count}";
            return str;
        }
    }
}
