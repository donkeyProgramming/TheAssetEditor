using System.Diagnostics;
using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Animation
{
    [DebuggerDisplay("AnimationFile - {Header.SkeletonName}[{DynamicFrames.Count}]")]
    public class AnimationFile
    {
        public const int InvalidBoneIndex = -1;
        public const int BoneIndexNoParent = InvalidBoneIndex;

        #region Sub classes
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
            public uint Version { get; set; }
            public uint Unknown0_alwaysOne { get; set; } = 1;
            public float FrameRate { get; set; } = 20;
            public string SkeletonName { get; set; }
            public uint FlagCount { get; set; } = 0;
            public List<string> FlagVariables { get; set; } = new List<string>();
            public float AnimationTotalPlayTimeInSec { get; set; }
            public uint UnknownValue_v8 { get; set; } = 0;
        }

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

            public AnimationBoneMapping Clone() => new AnimationBoneMapping(_value);

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

        public class AnimationPart
        {
            public Frame StaticFrame { get; set; } = null;
            public List<Frame> DynamicFrames = new List<Frame>();
            public List<AnimationBoneMapping> RotationMappings { get; set; } = new List<AnimationBoneMapping>();
            public List<AnimationBoneMapping> TranslationMappings { get; set; } = new List<AnimationBoneMapping>();
        }

        public class AnimationV8OptimizationData
        {
            public uint BoneCount { get; private set; }
            public sbyte[] TranslationBitRate { get; set; }
            public sbyte[] RotationBitRate { get; set; }

            public (RmvVector3 Min, RmvVector3 Max)[] Range_map_translations { get; set; }
            public (RmvVector4 Min, RmvVector4 Max)[] Range_map_quaternion { get; set; }

            public AnimationV8OptimizationData(uint boneCount)
            {
                BoneCount = boneCount;
                TranslationBitRate = new sbyte[BoneCount];
                RotationBitRate = new sbyte[BoneCount];
            }
        }

        #endregion

        public AnimationHeader Header { get; set; } = new AnimationHeader();
        public BoneInfo[] Bones;
        public List<AnimationPart> AnimationParts { get; set; } = new List<AnimationPart>();

        public static AnimationHeader GetAnimationHeader(PackFile file)
        {
            var data = file.DataSource.ReadData(100);
            try
            {
                return GetAnimationHeader(new ByteChunk(data));
            }
            catch (Exception e)
            {
                var logger = Logging.Create<AnimationFile>();
                logger.Here().Information($"Loading animation failed: {file} Size:{data.Length} Error:\n{e}");
                throw;
            }
        }

       public static string GetAnimationName(byte[] animationFileByteBuffer)
       {
            var offsetToName = 12;
            var stringLenth = BitConverter.ToInt16(animationFileByteBuffer, offsetToName);
            return Encoding.UTF8.GetString(animationFileByteBuffer, 12+2, stringLenth);
        }

        public static AnimationHeader GetAnimationHeader(ByteChunk chunk)
        {
            if (chunk.BytesLeft == 0)
                throw new Exception("Trying to load animation header with no data, chunk size = 0");

            var header = new AnimationHeader();
            header.Version = chunk.ReadUInt32();
            header.Unknown0_alwaysOne = chunk.ReadUInt32();        // Some type of type?
            header.FrameRate = chunk.ReadSingle();
            header.SkeletonName = chunk.ReadString();

            if (header.Version > 6)
            {
                header.FlagCount = chunk.ReadUInt32();
                for (var i = 0; i < header.FlagCount; i++)
                    header.FlagVariables.Add(chunk.ReadString());
            }

            header.AnimationTotalPlayTimeInSec = chunk.ReadSingle();

            var isSupportedAnimationFile = header.Version == 4 || header.Version == 5 || header.Version == 6 || header.Version == 7 || header.Version == 8;
            if (!isSupportedAnimationFile)
                throw new Exception($"Unsuported animation format: {header.Version}");

            return header;
        }

        public static AnimationFile Create(PackFile file)
        {
            var logger = Logging.Create<AnimationFile>();
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
            for (var i = 0; i < boneCount; i++)
            {
                var boneNameSize = chunk.ReadShort();
                output.Bones[i] = new BoneInfo()
                {
                    Name = chunk.ReadFixedLength(boneNameSize),
                    ParentId = chunk.ReadInt32(),
                    Id = i
                };
            }

            if (output.Header.Version == 8)
            {
                output.Header.UnknownValue_v8 = chunk.ReadUInt32();
                output.AnimationParts = LoadAnimationParts_v8(chunk, boneCount);
            }
            else
            {
                output.AnimationParts = LoadAnimationParts_Default(chunk, boneCount, output.Header.Version);
            }

            if (chunk.BytesLeft != 0)
                throw new Exception($"{chunk.BytesLeft} bytes left in animation");

            return output;
        }


        static List<AnimationPart> LoadAnimationParts_Default(ByteChunk chunk, uint boneCount, uint animationVersion)
        {
            var animPart = new AnimationPart();

            for (var i = 0; i < boneCount; i++)
            {
                var mappingValue = chunk.ReadInt32();
                animPart.TranslationMappings.Add(new AnimationBoneMapping(mappingValue));
            }

            for (var i = 0; i < boneCount; i++)
            {
                var mappingValue = chunk.ReadInt32();
                animPart.RotationMappings.Add(new AnimationBoneMapping(mappingValue));
            }

            // A single static frame - Can be inverse, a pose or empty. Not sure? Hand animations are stored here
            if (animationVersion == 7)
            {
                var staticPosCount = chunk.ReadUInt32();
                var staticRotCount = chunk.ReadUInt32();
                if (staticPosCount != 0 || staticRotCount != 0)
                    animPart.StaticFrame = ReadFrame(chunk, staticPosCount, staticRotCount);
            }

            // Animation Data
            var animPosCount = chunk.ReadInt32();
            var animRotCount = chunk.ReadInt32();
            var frameCount = chunk.ReadInt32();    // Always 3 when there is no data? Why?

            if (animPosCount != 0 || animRotCount != 0)
            {
                for (var i = 0; i < frameCount; i++)
                {
                    var frame = ReadFrame(chunk, (uint)animPosCount, (uint)animRotCount);
                    animPart.DynamicFrames.Add(frame);
                }
            }

            return new List<AnimationPart>() { animPart };
        }

        static List<AnimationPart> LoadAnimationParts_v8(ByteChunk chunk, uint boneCount)
        {
            var output = new List<AnimationPart>();

            var animationParts = chunk.ReadUInt32();

            for (var animationPartIndex = 0; animationPartIndex < animationParts; animationPartIndex++)
            {
                var animationPart = new AnimationPart();

                var optimizationData = new AnimationV8OptimizationData(boneCount);
                var translationCounter = 0;
                var translationCounterStatic = 0;
                for (var i = 0; i < boneCount; i++)
                {
                    optimizationData.TranslationBitRate[i] = (sbyte)chunk.ReadByte();
                    var value = -1;
                    if (optimizationData.TranslationBitRate[i] < 0)
                        value = 10000 + translationCounterStatic++;
                    else if (optimizationData.TranslationBitRate[i] > 0)
                        value = translationCounter++;

                    animationPart.TranslationMappings.Add(new AnimationBoneMapping(value));
                }

                var roationCounter = 0;
                var roationCounterStatic = 0;
                for (var i = 0; i < boneCount; i++)
                {
                    optimizationData.RotationBitRate[i] = (sbyte)chunk.ReadByte();
                    var value = -1;
                    if (optimizationData.RotationBitRate[i] < 0)
                        value = 10000 + roationCounterStatic++;
                    else if (optimizationData.RotationBitRate[i] > 0)
                        value = roationCounter++;

                    animationPart.RotationMappings.Add(new AnimationBoneMapping(value));
                }

                var range_map_translation_length = chunk.ReadUInt32();
                var range_map_quaterion_length = chunk.ReadUInt32();
                optimizationData.Range_map_translations = new (RmvVector3 Min, RmvVector3 Max)[range_map_translation_length];
                optimizationData.Range_map_quaternion = new (RmvVector4 Min, RmvVector4 Max)[range_map_quaterion_length];

                for (var i = 0; i < range_map_translation_length; i++)
                {
                    optimizationData.Range_map_translations[i].Min = new RmvVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                    optimizationData.Range_map_translations[i].Max = new RmvVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                }

                for (var i = 0; i < range_map_quaterion_length; i++)
                {
                    optimizationData.Range_map_quaternion[i].Min = new RmvVector4(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                    optimizationData.Range_map_quaternion[i].Max = new RmvVector4(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                }

                // Load static frame
                var const_track_tranlations_count = chunk.ReadUInt32();
                var const_track_quaternions_count = chunk.ReadUInt32();
                if (const_track_tranlations_count != 0 || const_track_quaternions_count != 0)
                    animationPart.StaticFrame = ReadFrameV8(chunk, const_track_tranlations_count, const_track_quaternions_count, false, optimizationData);

                var frame_tracks_tranlations_count = chunk.ReadUInt32();
                var frame_tracks_quaternions_count = chunk.ReadUInt32();
                var frame_count = chunk.ReadUInt32();

                if (frame_tracks_tranlations_count != 0 || frame_tracks_quaternions_count != 0)
                {
                    for (var frameIndex = 0; frameIndex < frame_count; frameIndex++)
                    {
                        var frame = ReadFrameV8(chunk, frame_tracks_tranlations_count, frame_tracks_quaternions_count, true, optimizationData);
                        animationPart.DynamicFrames.Add(frame);
                    }
                }

                output.Add(animationPart);
            }

            return output;
        }


        static Frame ReadFrameV8(ByteChunk chunk, uint positions, uint rotations, bool isDynamic, AnimationV8OptimizationData optimizationData)
        {
            var frame = new Frame();
            for (var i = 0; i < optimizationData.BoneCount; i++)
            {
                var bitRate = isDynamic ? optimizationData.TranslationBitRate[i] : -optimizationData.TranslationBitRate[i];
                switch (bitRate)
                {
                    case 12:
                        var vector = new RmvVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                        frame.Transforms.Add(vector);
                        break;

                    case 3:
                        var d0 = chunk.ReadSBytes(3);
                        var vecto2r = AnimationFileLoadHelpers.Decode_translation_24_888_ranged(d0, optimizationData, i);
                        frame.Transforms.Add(vecto2r);
                        break;

                    case 0:
                    case -12:
                    case -3:
                        break;

                    default:
                        throw new Exception("Unknown bit optimization");
                }
            }

            for (var i = 0; i < optimizationData.BoneCount; i++)
            {
                var bitRate = isDynamic ? optimizationData.RotationBitRate[i] : -optimizationData.RotationBitRate[i];
                switch (bitRate)
                {
                    case 8:
                        var maxValue = 1.0f / short.MaxValue;
                        var quat = new short[4] { chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort() };

                        var quaternion = new RmvVector4(quat[0] * maxValue, quat[1] * maxValue, quat[2] * maxValue, quat[3] * maxValue);
                        frame.Quaternion.Add(quaternion);
                        break;

                    case 4:
                        var d0 = chunk.ReadSBytes(4);
                        var q = AnimationFileLoadHelpers.Decode_quaternion_32_s8888_ranged(d0, optimizationData, i);
                        frame.Quaternion.Add(q);
                        break;

                    case 0:
                    case -8:
                    case -4:
                        break;

                    default:
                        throw new Exception("Unknown bit optimization");
                }
            }
            return frame;
        }

        static byte[] ConvertToBytesInvernal(AnimationFile input)
        {
            if (input.AnimationParts.Count != 1 || input.Header.Version == 8)
                throw new Exception("Animations with multiple parts or version 8 can not be saved!");

            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            // Header
            writer.Write(input.Header.Version);
            writer.Write(input.Header.Unknown0_alwaysOne);
            writer.Write(input.Header.FrameRate);
            writer.Write((short)input.Header.SkeletonName.Length);
            for (var i = 0; i < input.Header.SkeletonName.Length; i++)
                writer.Write(input.Header.SkeletonName[i]);

            if (input.Header.Version == 7 || input.Header.Version == 6)
            {
                writer.Write(input.Header.FlagCount);
                foreach (var flag in input.Header.FlagVariables)
                    writer.Write(ByteParsers.String.WriteCaString(flag));
            }

            writer.Write(input.Header.AnimationTotalPlayTimeInSec);

            //Body - Bones
            writer.Write((uint)input.Bones.Length);
            foreach (var bone in input.Bones)
            {
                writer.Write((short)bone.Name.Length);

                for (var i = 0; i < bone.Name.Length; i++)
                    writer.Write(bone.Name[i]);
                writer.Write(bone.ParentId);
            }

            foreach (var animationPart in input.AnimationParts)
            {
                // Body - remapping
                for (var i = 0; i < animationPart.TranslationMappings.Count; i++)
                    writer.Write(animationPart.TranslationMappings[i].FileWriteValue);

                for (var i = 0; i < animationPart.TranslationMappings.Count; i++)
                    writer.Write(animationPart.RotationMappings[i].FileWriteValue);

                // Static frame
                if (input.Header.Version == 7)
                {
                    if (animationPart.StaticFrame != null)
                    {
                        writer.Write((uint)animationPart.StaticFrame.Transforms.Count());   //staticPosCount
                        writer.Write((uint)animationPart.StaticFrame.Quaternion.Count());   //staticRotCount
                        WriteFrame(writer, animationPart.StaticFrame);
                    }
                    else
                    {
                        writer.Write((uint)0);   //staticPosCount
                        writer.Write((uint)0);   //staticRotCount
                    }
                }

                // Dyamic frame
                if (animationPart.DynamicFrames.Any())
                {
                    writer.Write(animationPart.DynamicFrames.First().Transforms.Count());   // animPosCount
                    writer.Write(animationPart.DynamicFrames.First().Quaternion.Count());   // animRotCount
                    writer.Write(animationPart.DynamicFrames.Count());                      // Frame count
                    for (var i = 0; i < animationPart.DynamicFrames.Count(); i++)
                        WriteFrame(writer, animationPart.DynamicFrames[i]);
                }
                else
                {
                    writer.Write(0);   // animPosCount
                    writer.Write(0);   // animRotCount
                    writer.Write(3);   // Frame count, why 3 when empty?
                }
            }

            return memoryStream.ToArray();
        }

        public static byte[] ConvertToBytes(AnimationFile input)
        {
            var bytes = ConvertToBytesInvernal(input);
            var tempResult = Create(new ByteChunk(bytes));  // Throws excetion and stop the save if the animation is corrupt for some reason

            return bytes;
        }


        static void WriteFrame(BinaryWriter writer, Frame frame)
        {
            for (var i = 0; i < frame.Transforms.Count(); i++)
            {
                writer.Write(frame.Transforms[i].X);
                writer.Write(frame.Transforms[i].Y);
                writer.Write(frame.Transforms[i].Z);
            }

            for (var i = 0; i < frame.Quaternion.Count(); i++)
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
            for (var j = 0; j < positions; j++)
            {
                var vector = new RmvVector3(chunk.ReadSingle(), chunk.ReadSingle(), chunk.ReadSingle());
                frame.Transforms.Add(vector);
            }

            for (var j = 0; j < rotations; j++)
            {
                var maxValue = 1.0f / short.MaxValue;
                var quat = new short[4] { chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort(), chunk.ReadShort() };

                var quaternion = new RmvVector4(quat[0] * maxValue, quat[1] * maxValue, quat[2] * maxValue, quat[3] * maxValue);
                frame.Quaternion.Add(quaternion);
            }
            return frame;
        }

        // Move this somewhere else - something like an animationManipulationService/AnimationEditor.
        public void ConvertToVersion(uint newAnimFormat, AnimationFile skeleton, IPackFileService pfs)
        {
            Header.Version = newAnimFormat;
            RemoveOptimizations(skeleton);
        }

        void RemoveOptimizations(AnimationFile skeleton)
        {
            foreach (var aninmationPart in AnimationParts)
            {
                var newDynamicFrames = new List<Frame>();

                var boneCount = aninmationPart.RotationMappings.Count;
                var frameCount = aninmationPart.DynamicFrames.Count;

                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var newKeyframe = new Frame();

                    for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
                    {
                        var translationLookup = aninmationPart.TranslationMappings[boneIndex];

                        if (translationLookup.IsDynamic)
                            newKeyframe.Transforms.Add(aninmationPart.DynamicFrames[frameIndex].Transforms[translationLookup.Id]);
                        else if (translationLookup.IsStatic)
                            newKeyframe.Transforms.Add(aninmationPart.StaticFrame.Transforms[translationLookup.Id]);
                        else
                            newKeyframe.Transforms.Add(skeleton.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex]);

                        var rotationLookup = aninmationPart.RotationMappings[boneIndex];
                        if (rotationLookup.IsDynamic)
                            newKeyframe.Quaternion.Add(aninmationPart.DynamicFrames[frameIndex].Quaternion[rotationLookup.Id]);
                        else if (rotationLookup.IsStatic)
                            newKeyframe.Quaternion.Add(aninmationPart.StaticFrame.Quaternion[rotationLookup.Id]);
                        else
                            newKeyframe.Quaternion.Add(skeleton.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex]);
                    }

                    newDynamicFrames.Add(newKeyframe);
                }

                // Update data
                var newRotMapping = new List<AnimationBoneMapping>();
                var newTransMappings = new List<AnimationBoneMapping>();

                for (var i = 0; i < boneCount; i++)
                {
                    newRotMapping.Add(new AnimationBoneMapping(i));
                    newTransMappings.Add(new AnimationBoneMapping(i));
                }

                aninmationPart.TranslationMappings = newTransMappings;
                aninmationPart.RotationMappings = newRotMapping;
                aninmationPart.DynamicFrames = newDynamicFrames;
                aninmationPart.StaticFrame = null;
            }
        }
    }
}
