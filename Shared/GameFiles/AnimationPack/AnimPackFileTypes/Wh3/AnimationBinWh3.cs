using Shared.Core.ByteParsing;
using Shared.Core.Misc;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3
{
    public class AnimationBinWh3 : IAnimationPackFile, IAnimationBinGenericFormat
    {
        public string FileName { get; set; }
        public AnimationPackFile Parent { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);
        public List<AnimationBinEntry> AnimationTableEntries { get; set; } = new List<AnimationBinEntry>();

        public uint TableVersion { get; set; } = 4;
        public uint TableSubVersion { get; set; } = 3;
        public string Name { get; set; } = "";
        public string MountBin { get; set; } = "";
        public string Unknown { get; set; } = "";    // Name of the rider bin
        public string SkeletonName { get; set; } = "";
        public string LocomotionGraph { get; set; } = "";
        public short UnknownValue1 { get; set; } = 0;   // bool 2x, IsSimpleFlight and IsLarge

        public AnimationBinWh3(string fileName, byte[] data = null)
        {
            FileName = fileName;
            if (data != null)
                CreateFromBytes(data);
        }

        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue(TableVersion, out _));
            if (TableVersion == 4)
                memStream.Write(ByteParsers.UInt32.EncodeValue(TableSubVersion, out _));

            memStream.Write(ByteParsers.String.WriteCaString(Name.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(MountBin.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(Unknown.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(SkeletonName.ToLower()));
            if (TableVersion == 4)
                memStream.Write(ByteParsers.String.WriteCaString(LocomotionGraph.ToLower()));

            memStream.Write(ByteParsers.Short.EncodeValue(UnknownValue1, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)AnimationTableEntries.Count, out _));
            foreach (var animationEntry in AnimationTableEntries)
            {
                memStream.Write(ByteParsers.UInt32.EncodeValue(animationEntry.AnimationId, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(animationEntry.BlendIn, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(animationEntry.SelectionWeight, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(animationEntry.WeaponBools, out _));
                memStream.Write(ByteParsers.Bool.EncodeValue(animationEntry.Unk, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)animationEntry.AnimationRefs.Count, out _));

                foreach (var animation in animationEntry.AnimationRefs)
                {
                    memStream.Write(ByteParsers.String.WriteCaString(animation.AnimationFile.ToLower()));
                    memStream.Write(ByteParsers.String.WriteCaString(animation.AnimationMetaFile.ToLower()));
                    memStream.Write(ByteParsers.String.WriteCaString(animation.AnimationSoundMetaFile.ToLower()));
                }
            }

            return memStream.ToArray();
        }

        public void CreateFromBytes(byte[] bytes)
        {
            var chunk = new ByteChunk(bytes);

            TableVersion = chunk.ReadUInt32();
            if (TableVersion == 4)
                LoadVersion4(chunk);
            else if (TableVersion == 2)
                LoadVersion2(chunk);
            else
                throw new Exception($"Unexpceted table version, got {TableVersion}, supports 2 (3k) and 4 (Wh3)");

            if (chunk.BytesLeft != 0)
                throw new Exception($"{chunk.BytesLeft} bytes left");
        }

        private void LoadVersion2(ByteChunk chunk)
        {
            TableSubVersion = 0;
            Name = chunk.ReadString();
            MountBin = chunk.ReadString();
            Unknown = chunk.ReadString();
            SkeletonName = chunk.ReadString();

            UnknownValue1 = chunk.ReadShort();  // Two bools? IsSimpleFlight and IsLarge

            LoadAnimationBinEntry(chunk);
        }

        private void LoadVersion4(ByteChunk chunk)
        {
            var tableSubVersion = chunk.ReadInt32();

            if (tableSubVersion != TableSubVersion)
                throw new Exception($"Unexpceted table version, expected {TableSubVersion}, got {tableSubVersion}");

            Name = chunk.ReadString();
            MountBin = chunk.ReadString();
            Unknown = chunk.ReadString(); // Always empty, could be a short
            SkeletonName = chunk.ReadString();
            LocomotionGraph = chunk.ReadString();
            UnknownValue1 = chunk.ReadShort();

            LoadAnimationBinEntry(chunk);
        }

        private void LoadAnimationBinEntry(ByteChunk chunk)
        {
            AnimationTableEntries.Clear();

            var slotCount = chunk.ReadUInt32();
            for (var i = 0; i < slotCount; i++)
            {
                var animID = chunk.ReadUInt32();
                var blend0 = chunk.ReadSingle();
                var selectionWeight = chunk.ReadSingle();
                var boneWeaponbools = chunk.ReadInt32();
                var frgUnk0 = chunk.ReadBool();
                var numVariants = chunk.ReadUInt32();

                var entry = new AnimationBinEntry()
                {
                    AnimationId = animID,
                    BlendIn = blend0,
                    SelectionWeight = selectionWeight,
                    WeaponBools = boneWeaponbools,
                    Unk = frgUnk0,
                };

                for (var varientCounter = 0; varientCounter < numVariants; varientCounter++)
                {
                    var animation_path = chunk.ReadString();
                    var animation_meta_path = chunk.ReadString();
                    var animation_sound_meta_path = chunk.ReadString();

                    entry.AnimationRefs.Add(new AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = animation_path,
                        AnimationMetaFile = animation_meta_path,
                        AnimationSoundMetaFile = animation_sound_meta_path
                    });
                }

                AnimationTableEntries.Add(entry);
            }
        }

        string IAnimationBinGenericFormat.Name { get => Name; }
        string IAnimationBinGenericFormat.SkeletonName { get => SkeletonName; }
        List<AnimationBinEntryGenericFormat> IAnimationBinGenericFormat.Entries
        {
            get
            {
                var output = new List<AnimationBinEntryGenericFormat>();
                foreach (var item in AnimationTableEntries)
                {
                    var index = -1;
                    if (item.AnimationRefs.Count != 1)
                        index = 1;
                    foreach (var animation in item.AnimationRefs)
                    {
                        var slotName = string.Empty;
                        if (TableVersion == 2)
                            slotName = AnimationSlotTypeHelper3k.GetFromId((int)item.AnimationId).Value;
                        else
                            slotName = AnimationSlotTypeHelperWh3.GetFromId((int)item.AnimationId).Value;

                        output.Add(new AnimationBinEntryGenericFormat()
                        {
                            AnimationFile = animation.AnimationFile,
                            MetaFile = animation.AnimationMetaFile,
                            SoundFile = animation.AnimationSoundMetaFile,
                            SlotIndex = (int)item.AnimationId,
                            SlotName = slotName,
                            BlendInTime = item.BlendIn,
                            SelectionWeight = item.SelectionWeight,
                            WeaponBools = item.WeaponBools,
                            Index = index
                        });
                    }
                }

                return output;
            }
        }

        string IAnimationBinGenericFormat.FullPath => FileName;

        AnimationPackFile IAnimationBinGenericFormat.PackFileReference => Parent;
    }

    public class AnimationBinEntry
    {
        public uint AnimationId { get; set; }
        public float BlendIn { get; set; }
        public float SelectionWeight { get; set; }
        public int WeaponBools { get; set; }
        public bool Unk { get; set; } = false;
        public List<AnimationRef> AnimationRefs { get; set; } = new List<AnimationRef>();

        public class AnimationRef
        {
            public string AnimationFile { get; set; }
            public string AnimationMetaFile { get; set; }
            public string AnimationSoundMetaFile { get; set; }
        }
    }
}
