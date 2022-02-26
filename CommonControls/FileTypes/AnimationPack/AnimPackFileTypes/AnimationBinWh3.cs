using CommonControls.Common;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonControls.FileTypes.AnimationPack.AnimPackFileTypes
{
    public class AnimationBinW3 : IAnimationPackFile
    {
        public string FileName { get; set; }
        public AnimationPackFile Parent { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        public uint TableVersion { get; set; } = 2;
        public List<AnimationBinEntryW3> AnimationTableEntries { get; set; } = new List<AnimationBinEntryW3>();

        public AnimationBinW3(string fileName, byte[] data = null)
        {
            FileName = fileName;
            if (data != null)
                CreateFromBytes(data);
        }

        public byte[] ToByteArray()
        {
            throw new System.Exception("Not supported right now!");
           //using MemoryStream memStream = new MemoryStream();
           //
           //memStream.Write(ByteParsers.Int32.EncodeValue(TableVersion, out _));
           //memStream.Write(ByteParsers.Int32.EncodeValue(AnimationTableEntries.Count, out _));
           //
           //foreach (var tableEntry in AnimationTableEntries)
           //    memStream.Write(tableEntry.ToByteArray());
           //
           //return memStream.ToArray();
        }

        public void CreateFromBytes(byte[] bytes)
        {
            var chunk = new ByteChunk(bytes);

            var TableVersion = chunk.ReadUInt32();
            var tableSubVersion = chunk.ReadInt32();
            var name = chunk.ReadString();
            var mountBin = chunk.ReadString();
            var unkValue1 = chunk.ReadString(); // Always empty, could be a short
            var skeletonName = chunk.ReadString();
            var locomotion_graph_xml = chunk.ReadString();
            var unkValue2 = chunk.ReadShort();

            var count = chunk.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                var animID = chunk.ReadUInt32();
                var blend0 = chunk.ReadSingle();
                var blend1 = chunk.ReadSingle();

                var boneWeaponbools = chunk.ReadByte();
                var frgUnk0 = chunk.ReadUInt32();
                var numVariants = chunk.ReadUInt32();
                for (int varientCounter = 0; varientCounter < numVariants; varientCounter++)
                {
                    var animation_path = chunk.ReadString();
                    var animation_meta_path = chunk.ReadString();
                    var animation_sound_meta_path = chunk.ReadString();
                }
            }

            if (chunk.BytesLeft != 0)
                throw new Exception("bytes left");

            //AnimationTableEntries = new List<AnimationBinEntryW3>();
            //for (int i = 0; i < rowCount; i++)
            //    AnimationTableEntries.Add(new AnimationBinEntryW3(chunk));
        }
    }

    public class AnimationBinEntryW3
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
                using MemoryStream memStream = new MemoryStream();
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

        public AnimationBinEntryW3(ByteChunk chunk)
        {

            var version0 = chunk.ReadUInt32();
            var version1 = chunk.ReadUInt32();
            var paddedStr = chunk.ReadFixedLength(4);

            //Name = chunk.ReadString();
            SkeletonName = chunk.ReadString();
            var locomotionGraph = chunk.ReadString();
            var unkown0 = chunk.ReadByte();



            //MountName = chunk.ReadString();
            var count = chunk.ReadUInt32();
            for (int i = 0; i < count; i++)
                FragmentReferences.Add(new FragmentReference(chunk));
            Unknown = chunk.ReadShort();
        }

        public AnimationBinEntryW3(string name, string skeletonName, string mountName = "")
        {
            Name = name;
            SkeletonName = skeletonName;
            MountName = mountName;
        }

        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();

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
