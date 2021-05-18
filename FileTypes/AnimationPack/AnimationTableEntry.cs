using Filetypes.ByteParsing;
using System.Collections.Generic;

namespace Filetypes.AnimationPack
{
    public class AnimationTableEntry
    {
        public class AnimationSet
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

        public List<AnimationSet> AnimationSets { get; set; } = new List<AnimationSet>();
        public short Unknown0 { get; set; }
        public short Unknown1 { get; set; }

        public AnimationTableEntry(ByteChunk data)
        {
            Name = data.ReadString();
            SkeletonName = data.ReadString();
            MountName = data.ReadString();

            LoadAnimationSets(data);
        }

        void LoadAnimationSets(ByteChunk data)
        {
            var count = data.ReadShort();
            Unknown0 = data.ReadShort();
            for (int i = 0; i < count; i++)
            {
                var animationSet = new AnimationSet()
                {
                    Name = data.ReadString(),
                    Unknown = data.ReadInt32()
                };
                AnimationSets.Add(animationSet);
            }
            Unknown1 = data.ReadShort();
        }

        public override string ToString()
        {
            var str = $"{Name}, Skeleton = {SkeletonName}";
            if (MountName.Length != 0)
                str += $", Mount = {MountName}";
            str += $", AnimationSets = {AnimationSets.Count}";
            return str;
        }
    }
}
