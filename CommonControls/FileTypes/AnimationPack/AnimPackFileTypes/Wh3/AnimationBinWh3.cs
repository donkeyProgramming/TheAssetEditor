using CommonControls.Common;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3
{
    public class AnimationBin : IAnimationPackFile
    {
        public string FileName { get; set; }
        public AnimationPackFile Parent { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        public List<AnimationBinEntry> AnimationTableEntries { get; set; } = new List<AnimationBinEntry>();

        public uint TableVersion { get; set; } = 4;
        public uint TableSubVersion { get; set; } = 3;
        public string Name { get; set; }
        public string MountBin { get; set; }
        public string UnknownValue0 { get; set; }
        public string SkeletonName { get; set; }
        public string LocomotionGraph { get; set; }
        public short UnknownValue1 { get; set; }


        public AnimationBin(string fileName, byte[] data = null)
        {
            FileName = fileName;
            if (data != null)
                CreateFromBytes(data);
        }

        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue(TableVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(TableSubVersion, out _));
            memStream.Write(ByteParsers.String.WriteCaString(Name.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(MountBin.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(UnknownValue0.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(SkeletonName.ToLower()));
            memStream.Write(ByteParsers.String.WriteCaString(LocomotionGraph.ToLower()));
            memStream.Write(ByteParsers.Short.EncodeValue(UnknownValue1, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)AnimationTableEntries.Count, out _));
            foreach (var animationEntry in AnimationTableEntries)
            {
                memStream.Write(ByteParsers.UInt32.EncodeValue(animationEntry.AnimationId, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(animationEntry.BlendIn, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(animationEntry.BlendOut, out _));
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

            var tableVersion = chunk.ReadUInt32();
            var tableSubVersion = chunk.ReadInt32();

            if (tableVersion != TableVersion)
                throw new Exception($"Unexpceted table version, expected {TableVersion}, got {tableVersion}");

            if (tableSubVersion != TableSubVersion)
                throw new Exception($"Unexpceted table version, expected {TableSubVersion}, got {tableSubVersion}");

            Name = chunk.ReadString();
            MountBin = chunk.ReadString();
            UnknownValue0 = chunk.ReadString(); // Always empty, could be a short
            SkeletonName = chunk.ReadString();
            LocomotionGraph = chunk.ReadString();
            UnknownValue1 = chunk.ReadShort();

            var count = chunk.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                var animID = chunk.ReadUInt32();
                var blend0 = chunk.ReadSingle();
                var blend1 = chunk.ReadSingle();
                var boneWeaponbools = chunk.ReadInt32();
                var frgUnk0 = chunk.ReadBool();
                var numVariants = chunk.ReadUInt32();
              
                AnimationBinEntry entry = new AnimationBinEntry()
                {
                    AnimationId = animID,
                    BlendIn = blend0,
                    BlendOut = blend1,
                    WeaponBools = boneWeaponbools,
                    Unk = frgUnk0,
                };
                //
                //if (frgUnk0 != 0)
                //{ 
                //
                //}


                //

                if (frgUnk0 == true)
                { 
                
                }

                for (int varientCounter = 0; varientCounter < numVariants; varientCounter++)
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

            if (chunk.BytesLeft != 0)
                throw new Exception($"{chunk.BytesLeft} bytes left");
        }
    }

    public class AnimationBinEntry
    {
        public uint AnimationId { get; set; }
        public float BlendIn { get; set; }
        public float BlendOut{ get; set; }
        public int WeaponBools { get; set; }
        public bool Unk { get; set; }
        public List<AnimationRef> AnimationRefs { get; set; } = new List<AnimationRef>();

        public class AnimationRef 
        {
            public string AnimationFile { get; set; }
            public string AnimationMetaFile { get; set; }
            public string AnimationSoundMetaFile { get; set; }
        }
    }
}
