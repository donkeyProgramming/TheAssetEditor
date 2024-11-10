using Shared.Core.ByteParsing;
using Shared.Core.Misc;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes
{
    public class AnimationBin : IAnimationPackFile
    {
        public string FileName { get; set; }
        public AnimationPackFile Parent { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        public int TableVersion { get; set; } = 2;
        public List<AnimationBinEntry> AnimationTableEntries { get; set; } = new();

        public AnimationBin(string fileName, byte[]? data = null)
        {
            FileName = fileName;
            if (data != null)
                CreateFromBytes(data);
        }

        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.Int32.EncodeValue(TableVersion, out _));
            memStream.Write(ByteParsers.Int32.EncodeValue(AnimationTableEntries.Count, out _));

            foreach (var tableEntry in AnimationTableEntries)
                memStream.Write(tableEntry.ToByteArray());

            return memStream.ToArray();
        }

        public void CreateFromBytes(byte[] bytes)
        {
            var data = new ByteChunk(bytes);

            TableVersion = data.ReadInt32();
            var rowCount = data.ReadInt32();
            AnimationTableEntries = new List<AnimationBinEntry>();
            for (var i = 0; i < rowCount; i++)
                AnimationTableEntries.Add(new AnimationBinEntry(data));
        }
    }

    public class AnimationBinEntry
    {
        public class FragmentReference
        {
            public string Name { get; set; }
            public int Unknown { get; set; } = 0;
            public FragmentReference() { }

            public FragmentReference(ByteChunk data)
            {
                Name = data.ReadString();
                Unknown = data.ReadInt32();
            }

            public byte[] ToByteArray()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.String.WriteCaString(Name.ToLower()));
                memStream.Write(ByteParsers.Int32.EncodeValue(Unknown, out _));
                return memStream.ToArray();
            }

        }

        public string Name { get; set; }
        public string SkeletonName { get; set; }
        public string MountName { get; set; }

        public List<FragmentReference> FragmentReferences { get; set; } = new List<FragmentReference>();
        public short Unknown { get; set; } = 0;

        public AnimationBinEntry(ByteChunk data)
        {
            Name = data.ReadString();
            SkeletonName = data.ReadString();
            MountName = data.ReadString();
            var count = data.ReadInt32();
            for (var i = 0; i < count; i++)
                FragmentReferences.Add(new FragmentReference(data));
            Unknown = data.ReadShort();
        }

        public AnimationBinEntry(string name, string skeletonName, string mountName = "")
        {
            Name = name;
            SkeletonName = skeletonName;
            MountName = mountName;
        }

        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.String.WriteCaString(Name.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(SkeletonName.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(MountName.ToLower()));

            memStream.Write(ByteParsers.Int32.EncodeValue(FragmentReferences.Count, out _));
            foreach (var fragment in FragmentReferences)
                memStream.Write(fragment.ToByteArray());

            memStream.Write(ByteParsers.Short.EncodeValue(Unknown, out _));

            return memStream.ToArray();
        }
    }
}
