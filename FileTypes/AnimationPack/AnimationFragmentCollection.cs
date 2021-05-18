using Common;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.AnimationPack
{
    public class AnimationFragmentCollection
    {
        public class StringArrayTable
        {
            public List<string> Values { get; set; } = new List<string>();

            public StringArrayTable()
            { }

            public StringArrayTable(ByteChunk data)
            {
                var count = data.ReadInt32();
                Values = new List<string>(count);
                for (int i = 0; i < count; i++)
                    Values.Add(data.ReadString());
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Values.Count);
                for (int i = 0; i < Values.Count; i++)
                {
                    var bytes = ByteParsers.String.WriteCaString(Values[i]);
                    writer.Write(bytes);
                }
            }
        }

        public string FileName { get; set; }
        public StringArrayTable Skeletons { get; set; } = new StringArrayTable();
        public int MinSlotId { get; set; }
        public int MaxSlotId { get; set; }
        public List<AnimationFragmentItem> AnimationFragments { get; set; } = new List<AnimationFragmentItem>();
        public AnimationFragmentCollection(string fileName, ByteChunk data = null)
        {
            FileName = fileName;
            if (data != null)
            {
                Skeletons = new StringArrayTable(data);
                MinSlotId = data.ReadInt32();
                MaxSlotId = data.ReadInt32();
                var numFragItems = data.ReadInt32();
                for (int i = 0; i < numFragItems; i++)
                    AnimationFragments.Add(new AnimationFragmentItem(data));
            }
        }


        AnimationFragmentItem GetFragment(AnimationSlotType slot)
        {
            return AnimationFragments.FirstOrDefault(x => x.Slot.Id == slot.Id);
        }



        public void AddFragmentCollection(AnimationFragmentCollection other)
        {
            int itemsAdded = 0;
            foreach (var otherFragment in other.AnimationFragments)
            {
                var existingFragment = GetFragment(otherFragment.Slot);
                if (existingFragment == null)
                {
                    var cpy = ObjectHelper.DeepClone(otherFragment);
                    AnimationFragments.Add(cpy);
                    itemsAdded++;
                }
            }

            AnimationFragments = AnimationFragments
                .OrderBy(x => x.Slot.Id)
                .ToList();

            UpdateMinAndMaxSlotIds();
        }

        void UpdateMinAndMaxSlotIds()
        {
            MinSlotId = AnimationFragments.Min(x => x.Slot.Id);
            MaxSlotId = AnimationFragments.Max(x => x.Slot.Id);
        }

        public void ChangeAnimationFileName(string prefix)
        {
            foreach (var fragment in AnimationFragments)
            {
                if (!string.IsNullOrWhiteSpace(fragment.AnimationFile))
                {
                    var dir = Path.GetDirectoryName(fragment.AnimationFile);
                    var fileName = Path.GetFileNameWithoutExtension(fragment.AnimationFile);
                    var ext = Path.GetExtension(fragment.AnimationFile);

                    fragment.AnimationFile = $"{dir}\\{prefix}{fileName}{ext}";
                }
            }
        }

        public void SetSkeleton(string skeletonName)
        {
            foreach (var fragment in AnimationFragments)
            {
                fragment.Skeleton = skeletonName;
            }
        }


        public void ChangeSkeleton(string newSkeletonName)
        {
            foreach (var fragment in AnimationFragments)
            {
                //fragment.AnimationFile.Replace(fragment.Skeleton)
                fragment.Skeleton = newSkeletonName;
            }

            for (int i = 0; i < Skeletons.Values.Count; i++)
                Skeletons.Values[i] = newSkeletonName;
        }

        public override string ToString()
        {
            return $"{FileName}";
        }

        public void Write(string path)
        {
            if (Skeletons.Values.Count == 1)
                Skeletons.Values.Add(Skeletons.Values[0]);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    Skeletons.Write(writer);
                    writer.Write(MinSlotId);
                    writer.Write(MaxSlotId);
                    writer.Write(AnimationFragments.Count);

                    foreach(var frag in AnimationFragments)
                        frag.Write(writer);
                        //writer.Write(Skeletons..Header.AnimationType);                       // Animtype
                    //writer.Write((uint)1);                                          // Uknown_always 1
                    //writer.Write(input.Header.FrameRate);                           // Framerate
                    //writer.Write((short)input.Header.SkeletonName.Length);          // SkeletonNAme length



                    var dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
            
                    using (var fileStream = File.Create(path))
                    {
                        memoryStream.WriteTo(fileStream);
                    }
                }
            }
        }
    }
}
