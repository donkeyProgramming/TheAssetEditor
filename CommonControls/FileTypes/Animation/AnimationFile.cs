using Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel.Transforms;
using Filetypes.ByteParsing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CommonControls.FileTypes.Animation
{

    /*
     static inline constexpr float SNORM16_To_Float(int16_t in)
    {
        if (in == 32767)
            return 1.f;
        
        if (in == -32768)
            return -1.f;

        float c = in;
        return ( c / ( 32767.0f ) );
    }
     */

    [DebuggerDisplay("AnimationFile - {Header.SkeletonName}[{DynamicFrames.Count}]")]
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
            public List<RmvVector3> Transforms { get; set; } = new List<RmvVector3>();
            public List<RmvVector4> Quaternion { get; set; } = new List<RmvVector4>();
        }

        public class AnimationHeader
        {
            public uint AnimationFormat { get; set; }
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

        public static AnimationHeader GetAnimationHeader(PackFile file)
        {
            var data = file.DataSource.ReadData(100);
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
            header.AnimationFormat = chunk.ReadUInt32();
            header.Unknown0_alwaysOne = chunk.ReadUInt32();        // Always 1?
            header.FrameRate = chunk.ReadSingle();
            var nameLength = chunk.ReadShort();
            header.SkeletonName = chunk.ReadFixedLength(nameLength);
            header.Unknown1_alwaysZero = chunk.ReadUInt32();        // Always 0? padding?

            if (header.AnimationFormat == 7)
                header.AnimationTotalPlayTimeInSec = chunk.ReadSingle(); // Play time

            bool isSupportedAnimationFile = header.AnimationFormat == 5 || header.AnimationFormat == 6 || header.AnimationFormat == 7 || header.AnimationFormat == 4;
            if (!isSupportedAnimationFile)
                throw new Exception($"Unsuported animation format: {header.AnimationFormat}");

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
                    else if (IsDynamic)
                        return AnimationBoneMappingType.Dynamic;
                    return AnimationBoneMappingType.None;
                }
            }

            public AnimationBoneMapping Clone()
            {
                return new AnimationBoneMapping(_value);
            }

            public bool IsStatic { get { return _value > 9999 && HasValue; } }
            public bool IsDynamic { get { return !IsStatic && HasValue; } }
            public int Id
            {
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
                var outputStr = $"{MappingType}";
                if (MappingType != AnimationBoneMappingType.None)
                    outputStr += $" - {Id}";
                return outputStr;
            }
        }

        public static AnimationFile Create(PackFile file)
        {
            ILogger logger = Logging.Create<AnimationFile>();
            var data = file.DataSource.ReadData();
            logger.Here().Information($"Loading animation: {file} Size:{data.Length}");
            return Create(new ByteChunk(data));
        }

        public static AnimationFile Create(ByteChunk chunk)
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
            if (output.Header.AnimationFormat == 7)
            {
                var staticPosCount = chunk.ReadUInt32();
                var staticRotCount = chunk.ReadUInt32();
                if (staticPosCount != 0 || staticRotCount != 0)
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

            if (output.Header.AnimationFormat != 7)
                output.Header.AnimationTotalPlayTimeInSec = output.DynamicFrames.Count() / output.Header.FrameRate;


            return output;
        }

        public static byte[] GetBytes(AnimationFile input)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    // Header
                    writer.Write(input.Header.AnimationFormat);                     // Animtype
                    writer.Write(input.Header.Unknown0_alwaysOne);                  // Uknown_always 1
                    writer.Write(input.Header.FrameRate);                           // Framerate
                    writer.Write((short)input.Header.SkeletonName.Length);          // SkeletonNAme length
                    for (int i = 0; i < input.Header.SkeletonName.Length; i++)      // SkeletonNAme
                        writer.Write(input.Header.SkeletonName[i]);
                    writer.Write(input.Header.Unknown1_alwaysZero);                  // Uknown_always 0

                    if (input.Header.AnimationFormat == 7)
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
                    if (input.Header.AnimationFormat == 7)
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
                        writer.Write(0);   // animPosCount
                        writer.Write(0);   // animRotCount
                        writer.Write(3);   // Frame count, why 3 when empty?
                    }

                    var bytes = memoryStream.ToArray();
                    var tempResult = Create(new ByteChunk(bytes));  // Throws excetion and stop the save if the animation is corrupt for some reason

                    return bytes;
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
                var vector = new RmvVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                frame.Transforms.Add(vector);
            }

            for (int j = 0; j < rotations; j++)
            {
                var maxValue = 1.0f / short.MaxValue;
                var quat = new short[4] { chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort() };

                var quaternion = new RmvVector4(quat[0] * maxValue, quat[1] * maxValue, quat[2] * maxValue, quat[3] * maxValue);
                frame.Quaternion.Add(quaternion);
            }
            return frame;
        }

    }
}
