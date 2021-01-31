using Common;
using Filetypes.ByteParsing;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Filetypes.RigidModel
{
    public class AnimationFile
    {
        public class BoneInfo
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public int ParentId { get; set; }
        }

        public class Frame
        {
            public List<FileVector3> Transforms { get; set; } = new List<FileVector3>();
            public List<FileVector4> Quaternion { get; set; } = new List<FileVector4>();
        }

        public class AnimationHeader
        {
            public uint AnimationType { get; set; } 
            public uint Unknown0_alwaysOne { get; set; } = 1;
            public float FrameRate { get; set; } = 20;
            public string SkeletonName { get; set; }
            public uint Unknown1_alwaysZero { get; set; } = 0;
            public float AnimationTotalPlayTimeInSec { get; set; }
        }

        public BoneInfo[] Bones;

        // Version 7 spesific 
        
        public Frame StaticFrame { get; set; } = null;
        public List<Frame> DynamicFrames = new List<Frame>();


        public AnimationHeader Header { get; set; } = new AnimationHeader();

        public static AnimationHeader GetAnimationHeader(PackedFile file)
        {
            var data = file?.Data;
            try
            {
                return GetAnimationHeader(new ByteChunk(data));
            }
            catch (Exception e)
            {
                ILogger logger = Logging.Create<AnimationFile>();
                logger.Here().Information($"Loading animation failed: {file} Size:{data.Length} Error:\n{e.ToString()}");
                throw;
            }
        }

        static AnimationHeader GetAnimationHeader(ByteChunk chunk)
        {
            if (chunk.BytesLeft == 0)
                throw new Exception("Trying to load animation header with no data, chunk size = 0");

            var header = new AnimationHeader();
            header.AnimationType = chunk.ReadUInt32();
            header.Unknown0_alwaysOne = chunk.ReadUInt32();        // Always 1?
            header.FrameRate = chunk.ReadSingle();
            var nameLength = chunk.ReadShort();
            header.SkeletonName = chunk.ReadFixedLength(nameLength);
            header.Unknown1_alwaysZero = chunk.ReadUInt32();        // Always 0? padding?

            if (header.AnimationType == 7)
                header.AnimationTotalPlayTimeInSec = chunk.ReadSingle(); // Play time

            bool isSupportedAnimationFile = header.AnimationType == 5 || header.AnimationType == 6 || header.AnimationType == 7;
            if (!isSupportedAnimationFile)
                throw new Exception($"Unsuported animation format: {header.AnimationType}");

            return header;
        }


        public List<AnimationBoneMapping> RotationMappings { get; set; } = new List<AnimationBoneMapping>();
        public List<AnimationBoneMapping> TranslationMappings { get; set; } = new List<AnimationBoneMapping>();


        public enum AnimationBoneMappingType
        { 
            Dynamic = 0,
            Static,
            None
        }

        public class AnimationBoneMapping
        {
            int _value;
            public AnimationBoneMapping(int gameFormatValue)
            {
                _value = gameFormatValue;
            }

            public bool HasValue { get { return _value != -1; } }

            public AnimationBoneMappingType MappingType
            {
                get
                {
                    if (IsStatic)
                        return AnimationBoneMappingType.Static;
                    else if(IsDynamic)
                        return AnimationBoneMappingType.Dynamic;
                    return AnimationBoneMappingType.None;
                }
            }

            public bool IsStatic { get { return (_value > 9999) && HasValue; } }
            public bool IsDynamic { get { return !IsStatic && HasValue; } }
            public int Id { 
                get 
                {
                    if (IsStatic)
                        return _value - 10000;
                    else
                        return _value;
                } 
            }

            public int FileWriteValue { get { return _value; } }

            public override string ToString()
            {
                var outputStr =  $"{MappingType}";
                if (MappingType != AnimationBoneMappingType.None)
                    outputStr += $" - {Id}";
                return outputStr;
            }
        }


        public static AnimationFile Create(PackedFile file)
        {
            ILogger logger = Logging.Create<AnimationFile>();
            var data = file?.Data;
            logger.Here().Information($"Loading animation: {file} Size:{data.Length}");
            return Create(new ByteChunk(data));
        }


        static AnimationFile Create(ByteChunk chunk)
        {
            if (chunk.BytesLeft == 0)
                throw new Exception("Trying to load animation with no data, chunk size = 0");
            var output = new AnimationFile();
            chunk.Reset();
            output.Header = GetAnimationHeader(chunk);

            var boneCount = chunk.ReadUInt32();
            output.Bones = new BoneInfo[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var boneNameSize = chunk.ReadShort();
                output.Bones[i] = new BoneInfo()
                {
                    Name = chunk.ReadFixedLength(boneNameSize),
                    ParentId = chunk.ReadInt32(),
                    Id = i
                };
            }

            // Remapping tables, not sure how they really should be used, but this works.
            for (int i = 0; i < boneCount; i++)
            {
                int mappingValue = chunk.ReadInt32();
                output.TranslationMappings.Add(new AnimationBoneMapping(mappingValue));
            }

            for (int i = 0; i < boneCount; i++)
            {
                int mappingValue = chunk.ReadInt32();
                output.RotationMappings.Add(new AnimationBoneMapping(mappingValue));
            }

            // A single static frame - Can be inverse, a pose or empty. Not sure? Hand animations are stored here
            if (output.Header.AnimationType == 7)
            {
                var staticPosCount = chunk.ReadUInt32();
                var staticRotCount = chunk.ReadUInt32();
                if(staticPosCount != 0 || staticRotCount != 0)
                    output.StaticFrame = ReadFrame(chunk, staticPosCount, staticRotCount);
            }

            // Animation Data
            var animPosCount = chunk.ReadInt32();
            var animRotCount = chunk.ReadInt32();
            var frameCount = chunk.ReadInt32();    // Always 3 when there is no data? Why?

            if (animPosCount != 0 || animRotCount != 0)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    var frame = ReadFrame(chunk, (uint)animPosCount, (uint)animRotCount);
                    output.DynamicFrames.Add(frame);
                }
            }
            // ----------------------
            
            return output;
        }

        public static void Write(AnimationFile input, string path)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    // Header
                    writer.Write(input.Header.AnimationType);                       // Animtype
                    writer.Write((uint)1);                                          // Uknown_always 1
                    writer.Write(input.Header.FrameRate);                           // Framerate
                    writer.Write((short)input.Header.SkeletonName.Length);          // SkeletonNAme length
                    for (int i = 0; i < input.Header.SkeletonName.Length; i++)      // SkeletonNAme
                        writer.Write(input.Header.SkeletonName[i]);
                    writer.Write((uint)0);                                          // Uknown_always 0

                    if (input.Header.AnimationType == 7)
                        writer.Write(input.Header.AnimationTotalPlayTimeInSec);

                    //Body - Bones
                    writer.Write((uint)input.Bones.Length);
                    foreach (var bone in input.Bones)
                    {
                        writer.Write((short)bone.Name.Length);

                        for (int i = 0; i < bone.Name.Length; i++)
                            writer.Write(bone.Name[i]);
                        writer.Write(bone.ParentId);
                    }

                    // Body - remapping
                    for (int i = 0; i < input.TranslationMappings.Count; i++)
                        writer.Write(input.TranslationMappings[i].FileWriteValue);

                    for (int i = 0; i < input.TranslationMappings.Count; i++)
                        writer.Write(input.RotationMappings[i].FileWriteValue);

                    // Static frame
                    if (input.Header.AnimationType == 7)
                    {
                        if (input.StaticFrame != null)
                        {
                            writer.Write((uint)input.StaticFrame.Transforms.Count());   //staticPosCount
                            writer.Write((uint)input.StaticFrame.Quaternion.Count());   //staticRotCount
                            WriteFrame(writer, input.StaticFrame);
                        }
                        else
                        {
                            writer.Write((uint)0);   //staticPosCount
                            writer.Write((uint)0);   //staticRotCount
                        }
                    }

                    // Dyamic frame
                    if (input.DynamicFrames.Any())
                    {
                        writer.Write(input.DynamicFrames.First().Transforms.Count());   // animPosCount
                        writer.Write(input.DynamicFrames.First().Quaternion.Count());   // animRotCount
                        writer.Write(input.DynamicFrames.Count());                      // Frame count
                        for (int i = 0; i < input.DynamicFrames.Count(); i++)
                            WriteFrame(writer, input.DynamicFrames[i]);
                    }
                    else
                    {
                        writer.Write((int)0);   // animPosCount
                        writer.Write((int)0);   // animRotCount
                        writer.Write((int)3);   // Frame count, why 3 when empty?
                    }


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


        static void WriteFrame(BinaryWriter writer, Frame frame)
        {
            for (int i = 0; i < frame.Transforms.Count(); i++)
            {
                writer.Write(frame.Transforms[i].X);
                writer.Write(frame.Transforms[i].Y);
                writer.Write(frame.Transforms[i].Z);
            }

            for (int i = 0; i < frame.Quaternion.Count(); i++)
            {
                writer.Write((short)(frame.Quaternion[i].X * short.MaxValue));
                writer.Write((short)(frame.Quaternion[i].Y * short.MaxValue));
                writer.Write((short)(frame.Quaternion[i].Z * short.MaxValue));
                writer.Write((short)(frame.Quaternion[i].W * short.MaxValue));
            }
        }

        static Frame ReadFrame(ByteChunk chunk, uint positions, uint rotations)
        {
            var frame = new Frame();
            for (int j = 0; j < positions; j++)
            {
                var vector = new FileVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                frame.Transforms.Add(vector);
            }

            for (int j = 0; j < rotations; j++)
            {
                var maxValue = 1.0f / (float)short.MaxValue;
                var quat = new short[4] { chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort() };

                var quaternion = new FileVector4(quat[0] * maxValue, quat[1] * maxValue, quat[2] * maxValue, quat[3] * maxValue);
                frame.Quaternion.Add(quaternion);
            }
            return frame;
        }

    }
}
