using Common;
using Filetypes.ByteParsing;
using FileTypes.AnimationPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.FileTypes.AnimationPack
{
    public class AnimationFragment
    {
        public AnimationPackFile ParentAnimationPack { get; set; }

        public class StringArrayTable
        {
            public List<string> Values { get; set; } = new List<string>();

            public StringArrayTable(params string[] items)
            {
                if (items != null)
                {
                    foreach (var item in items)
                        Values.Add(item);
                }
            }

            public StringArrayTable() { }

            public StringArrayTable(ByteChunk data)
            {
                var count = data.ReadInt32();
                Values = new List<string>(count);
                for (int i = 0; i < count; i++)
                    Values.Add(data.ReadString());
            }

            public byte[] ToByteArray()
            {
                using MemoryStream memStream = new MemoryStream();
                memStream.Write(ByteParsers.Int32.EncodeValue(Values.Count, out _));
                foreach (var item in Values)
                    memStream.Write(ByteParsers.String.WriteCaString(item.ToLower()));

                return memStream.ToArray();
            }
        }

        public string FileName { get; set; }
        public StringArrayTable Skeletons { get; set; } = new StringArrayTable();
        public int MinSlotId { get; set; }
        public int MaxSlotId { get; set; }
        public List<AnimationFragmentEntry> Fragments { get; set; } = new List<AnimationFragmentEntry>();
        public AnimationFragment(string fileName, ByteChunk data = null)
        {
            FileName = fileName;
            if (data != null)
            {
                Skeletons = new StringArrayTable(data);
                MinSlotId = data.ReadInt32();
                MaxSlotId = data.ReadInt32();
                var numFragItems = data.ReadInt32();
                for (int i = 0; i < numFragItems; i++)
                    Fragments.Add(new AnimationFragmentEntry(data));
            }
        }

        public AnimationFragment() { }

        public byte[] ToByteArray()
        {
            // Ensure it ok
            UpdateMinAndMaxSlotIds();
            Fragments = Fragments.OrderBy(x => x.Slot.Id).ToList();
            foreach (var fragment in Fragments)
            {
                fragment.AnimationFile = fragment.AnimationFile.Replace("\\", "/").ToLower();
                fragment.MetaDataFile = fragment.MetaDataFile.Replace("\\", "/").ToLower();
                fragment.SoundMetaDataFile = fragment.SoundMetaDataFile.Replace("\\", "/").ToLower();
            }

            // Save
            using MemoryStream memStream = new MemoryStream();
            memStream.Write(Skeletons.ToByteArray());

            memStream.Write(ByteParsers.Int32.EncodeValue(MinSlotId, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(MaxSlotId, out _));

            memStream.Write(ByteParsers.Int32.EncodeValue(Fragments.Count, out _));
            foreach (var item in Fragments)
                memStream.Write(item.ToByteArray());

            return memStream.ToArray();
        }

        AnimationFragmentEntry GetFragment(AnimationSlotType slot)
        {
            return Fragments.FirstOrDefault(x => x.Slot.Id == slot.Id);
        }

        public void AddFragmentCollection(AnimationFragment other)
        {
            int itemsAdded = 0;
            foreach (var otherFragment in other.Fragments)
            {
                var existingFragment = GetFragment(otherFragment.Slot);
                if (existingFragment == null)
                {
                    var cpy = ObjectHelper.DeepClone(otherFragment);
                    Fragments.Add(cpy);
                    itemsAdded++;
                }
            }

            Fragments = Fragments
                .OrderBy(x => x.Slot.Id)
                .ToList();

            UpdateMinAndMaxSlotIds();
        }

        public void UpdateMinAndMaxSlotIds()
        {
            MinSlotId = 0;
            MaxSlotId = 0;
            if (Fragments.Count != 0)
            {
                MinSlotId = Fragments.Min(x => x.Slot.Id);
                MaxSlotId = Fragments.Max(x => x.Slot.Id);
            }
        }

        public void ChangeAnimationFileName(string prefix)
        {
            foreach (var fragment in Fragments)
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

        public void SetSkeletonForAllFragments(string skeletonName)
        {
            foreach (var fragment in Fragments)
            {
                fragment.Skeleton = skeletonName;
            }
        }
    }
}
