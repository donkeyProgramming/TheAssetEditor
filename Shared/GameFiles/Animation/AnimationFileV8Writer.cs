using Shared.GameFormats.RigidModel.Transforms;
using static Shared.GameFormats.Animation.AnimationFile;

namespace Shared.GameFormats.Animation
{
    // Writes the version 8 body. A v8 part packs each bone's channel at a per-bone "rate" whose sign is also the
    // mapping (negative static, positive dynamic, zero none):
    //     translations: 12 = three floats, 3 = three sbytes through a range
    //     rotations:     8 = four int16/32767, 4 = four sbytes through a range
    // A part read from a file keeps its rates and range tables so it saves back as it was. A range is only refitted
    // when an edited value falls outside it. A part built from scratch is written uncompressed (12/8, no ranges).
    // Follows io_scene_rmv2/anim_format.py (the RMV2 Blender plugin), which writes the same files.
    static class AnimationFileV8Writer
    {
        const sbyte TranslationRaw = 12;
        const sbyte TranslationRanged = 3;
        const sbyte RotationRaw = 8;
        const sbyte RotationRanged = 4;

        // How far outside its range a value may sit before the range is refitted; only float noise should land here
        const float RangeSlack = 1e-6f;

        public static void Write(BinaryWriter writer, AnimationFile input)
        {
            var boneCount = input.Bones.Length;
            writer.Write(input.Header.UnknownValue_v8);
            writer.Write((uint)input.AnimationParts.Count);

            for (var partIndex = 0; partIndex < input.AnimationParts.Count; partIndex++)
            {
                var part = input.AnimationParts[partIndex];
                ValidateVersionEightPart(part, boneCount, partIndex);

                var translationRates = GetRates(part.V8Packing?.TranslationBitRate, part.TranslationMappings, TranslationRaw, TranslationRanged, boneCount);
                var rotationRates = GetRates(part.V8Packing?.RotationBitRate, part.RotationMappings, RotationRaw, RotationRanged, boneCount);

                var translationRanges = GetRanges(part, part.V8Packing?.Range_map_translations, translationRates, TranslationRanged,
                    f => f.Transforms, v => [v.X, v.Y, v.Z], (min, max) => (new RmvVector3(min[0], min[1], min[2]), new RmvVector3(max[0], max[1], max[2])),
                    r => ([r.Min.X, r.Min.Y, r.Min.Z], [r.Max.X, r.Max.Y, r.Max.Z]));
                var rotationRanges = GetRanges(part, part.V8Packing?.Range_map_quaternion, rotationRates, RotationRanged,
                    f => f.Quaternion, v => [v.X, v.Y, v.Z, v.W], (min, max) => (new RmvVector4(min[0], min[1], min[2], min[3]), new RmvVector4(max[0], max[1], max[2], max[3])),
                    r => ([r.Min.X, r.Min.Y, r.Min.Z, r.Min.W], [r.Max.X, r.Max.Y, r.Max.Z, r.Max.W]));

                foreach (var rate in translationRates)
                    writer.Write(rate);
                foreach (var rate in rotationRates)
                    writer.Write(rate);

                writer.Write((uint)translationRanges.Length);
                writer.Write((uint)rotationRanges.Length);
                foreach (var (min, max) in translationRanges)
                    WriteFloats(writer, min.X, min.Y, min.Z, max.X, max.Y, max.Z);
                foreach (var (min, max) in rotationRanges)
                    WriteFloats(writer, min.X, min.Y, min.Z, min.W, max.X, max.Y, max.Z, max.W);

                var staticFrame = part.StaticFrame;
                if (staticFrame != null)
                {
                    writer.Write((uint)staticFrame.Transforms.Count);
                    writer.Write((uint)staticFrame.Quaternion.Count);
                    WriteFrame(writer, staticFrame, translationRates, rotationRates, translationRanges, rotationRanges, false);
                }
                else
                {
                    writer.Write(0u);
                    writer.Write(0u);
                }

                if (part.DynamicFrames.Count != 0)
                {
                    writer.Write((uint)part.DynamicFrames[0].Transforms.Count);
                    writer.Write((uint)part.DynamicFrames[0].Quaternion.Count);
                    writer.Write((uint)part.DynamicFrames.Count);
                    foreach (var frame in part.DynamicFrames)
                        WriteFrame(writer, frame, translationRates, rotationRates, translationRanges, rotationRanges, true);
                }
                else
                {
                    writer.Write(0u);
                    writer.Write(0u);
                    writer.Write((uint)(part.DeclaredFrameCount ?? 0));
                }
            }
        }

        // The file's rates where they still agree with the mappings; otherwise the uncompressed rate for the mapping
        static sbyte[] GetRates(sbyte[]? stored, List<AnimationBoneMapping> mappings, sbyte raw, sbyte ranged, int boneCount)
        {
            var useStored = stored != null && stored.Length == boneCount;
            var rates = new sbyte[boneCount];
            for (var bone = 0; bone < boneCount; bone++)
            {
                var mapping = mappings[bone];
                var storedRate = useStored ? stored![bone] : (sbyte)0;
                if (mapping.IsDynamic)
                    rates[bone] = storedRate == raw || storedRate == ranged ? storedRate : raw;
                else if (mapping.IsStatic)
                    rates[bone] = storedRate == -raw || storedRate == -ranged ? storedRate : (sbyte)-raw;
                else
                    rates[bone] = 0;
            }
            return rates;
        }

        // The range table for one channel kind: the file's table, with a bone refitted only where its values left the range
        static (TVec Min, TVec Max)[] GetRanges<TVec>(AnimationPart part, (TVec Min, TVec Max)[]? stored, sbyte[] rates, sbyte ranged,
            Func<Frame, List<TVec>> channel, Func<TVec, float[]> components, Func<float[], float[], (TVec, TVec)> build, Func<(TVec Min, TVec Max), (float[] Min, float[] Max)> split)
        {
            var needed = RangedValues(part, rates, ranged, channel);
            if (needed.Count == 0)
                return stored ?? [];  // nothing ranged now, but a table read from the file is still the file's

            var size = Math.Max(stored?.Length ?? 0, needed.Keys.Max() + 1);
            var table = new (TVec Min, TVec Max)[size];
            var width = components(default!).Length;
            for (var i = 0; i < size; i++)
                table[i] = stored != null && i < stored.Length ? stored[i] : build(new float[width], new float[width]);

            foreach (var (bone, values) in needed)
            {
                var (scale, centre) = split(table[bone]);
                var fits = stored != null && bone < stored.Length;
                var low = new float[width];
                var high = new float[width];
                for (var c = 0; c < width; c++)
                {
                    low[c] = float.MaxValue;
                    high[c] = float.MinValue;
                }

                foreach (var value in values)
                {
                    var v = components(value);
                    for (var c = 0; c < width; c++)
                    {
                        low[c] = Math.Min(low[c], v[c]);
                        high[c] = Math.Max(high[c], v[c]);
                        var halfWidth = Math.Abs(scale[c]);
                        if (v[c] < centre[c] - halfWidth - RangeSlack || v[c] > centre[c] + halfWidth + RangeSlack)
                            fits = false;
                    }
                }

                if (fits)
                    continue;

                var newScale = new float[width];
                var newCentre = new float[width];
                for (var c = 0; c < width; c++)
                {
                    newScale[c] = (high[c] - low[c]) / 2.0f;
                    newCentre[c] = (high[c] + low[c]) / 2.0f;
                }
                table[bone] = build(newScale, newCentre);
            }

            return table;
        }

        // {bone: every value it stores} for the bones packed through a range, found the way the reader walks a frame
        static Dictionary<int, List<TVec>> RangedValues<TVec>(AnimationPart part, sbyte[] rates, sbyte ranged, Func<Frame, List<TVec>> channel)
        {
            var output = new Dictionary<int, List<TVec>>();
            foreach (var isDynamic in new[] { false, true })
            {
                var frames = isDynamic ? part.DynamicFrames : (part.StaticFrame != null ? [part.StaticFrame] : new List<Frame>());
                if (frames.Count == 0)
                    continue;

                var column = 0;
                for (var bone = 0; bone < rates.Length; bone++)
                {
                    var rate = isDynamic ? rates[bone] : -rates[bone];
                    if (rate <= 0)
                        continue;
                    if (rate == ranged)
                    {
                        if (!output.TryGetValue(bone, out var list))
                            output[bone] = list = [];
                        list.AddRange(frames.Select(f => channel(f)[column]));
                    }
                    column++;
                }
            }
            return output;
        }

        static void WriteFrame(BinaryWriter writer, Frame frame, sbyte[] translationRates, sbyte[] rotationRates,
            (RmvVector3 Min, RmvVector3 Max)[] translationRanges, (RmvVector4 Min, RmvVector4 Max)[] rotationRanges, bool isDynamic)
        {
            var column = 0;
            for (var bone = 0; bone < translationRates.Length; bone++)
            {
                var rate = isDynamic ? translationRates[bone] : -translationRates[bone];
                if (rate <= 0)
                    continue;

                var original = OriginalBytes(frame.V8TranslationBytes, column, 3);
                var value = frame.Transforms[column++];
                if (rate == TranslationRaw)
                {
                    WriteFloats(writer, value.X, value.Y, value.Z);
                }
                else
                {
                    var (scale, centre) = translationRanges[bone];
                    writer.Write(AnimationFileLoadHelpers.Encode(value.X, scale.X, centre.X, original?[0]));
                    writer.Write(AnimationFileLoadHelpers.Encode(value.Y, scale.Y, centre.Y, original?[1]));
                    writer.Write(AnimationFileLoadHelpers.Encode(value.Z, scale.Z, centre.Z, original?[2]));
                }
            }

            column = 0;
            for (var bone = 0; bone < rotationRates.Length; bone++)
            {
                var rate = isDynamic ? rotationRates[bone] : -rotationRates[bone];
                if (rate <= 0)
                    continue;

                var original = OriginalBytes(frame.V8QuaternionBytes, column, 4);
                var value = frame.Quaternion[column++];
                if (rate == RotationRaw)
                {
                    writer.Write(QuaternionComponentToShort(value.X));
                    writer.Write(QuaternionComponentToShort(value.Y));
                    writer.Write(QuaternionComponentToShort(value.Z));
                    writer.Write(QuaternionComponentToShort(value.W));
                }
                else
                {
                    var (scale, centre) = rotationRanges[bone];
                    writer.Write(AnimationFileLoadHelpers.Encode(value.X, scale.X, centre.X, original?[0]));
                    writer.Write(AnimationFileLoadHelpers.Encode(value.Y, scale.Y, centre.Y, original?[1]));
                    writer.Write(AnimationFileLoadHelpers.Encode(value.Z, scale.Z, centre.Z, original?[2]));
                    writer.Write(AnimationFileLoadHelpers.Encode(value.W, scale.W, centre.W, original?[3]));
                }
            }
        }

        static sbyte[]? OriginalBytes(List<sbyte[]?>? bytes, int column, int width)
        {
            var original = bytes != null && column < bytes.Count ? bytes[column] : null;
            return original?.Length == width ? original : null;
        }

        // Rounded, and clamped after scaling: a stored -32768 reads back as -1.00003 and must write back as -32768
        static short QuaternionComponentToShort(float value) => (short)Math.Clamp(Math.Round(value * 32767.0f, MidpointRounding.ToEven), short.MinValue, short.MaxValue);

        static void WriteFloats(BinaryWriter writer, params float[] values)
        {
            foreach (var value in values)
                writer.Write(value);
        }

        static void ValidateVersionEightPart(AnimationPart part, int boneCount, int partIndex)
        {
            if (part.TranslationMappings.Count != boneCount || part.RotationMappings.Count != boneCount)
                throw new InvalidDataException($"Part {partIndex}: version 8 animation mappings must match the bone count ({boneCount}).");

            var staticTransformCount = ValidateMappingIds(part.TranslationMappings, AnimationBoneMappingType.Static, partIndex);
            var staticQuaternionCount = ValidateMappingIds(part.RotationMappings, AnimationBoneMappingType.Static, partIndex);
            var dynamicTransformCount = ValidateMappingIds(part.TranslationMappings, AnimationBoneMappingType.Dynamic, partIndex);
            var dynamicQuaternionCount = ValidateMappingIds(part.RotationMappings, AnimationBoneMappingType.Dynamic, partIndex);

            if ((staticTransformCount != 0 || staticQuaternionCount != 0) && part.StaticFrame == null)
                throw new InvalidDataException($"Part {partIndex}: version 8 static mappings require a static frame.");
            if (part.StaticFrame != null && (part.StaticFrame.Transforms.Count != staticTransformCount || part.StaticFrame.Quaternion.Count != staticQuaternionCount))
                throw new InvalidDataException($"Part {partIndex}: version 8 static frame data does not match its mappings.");
            if (part.StaticFrame != null && staticTransformCount == 0 && staticQuaternionCount == 0)
                throw new InvalidDataException($"Part {partIndex}: version 8 static frame must contain a mapped track.");
            if ((dynamicTransformCount != 0 || dynamicQuaternionCount != 0) && part.DynamicFrames.Count == 0)
                throw new InvalidDataException($"Part {partIndex}: version 8 dynamic mappings require at least one frame.");
            if (dynamicTransformCount == 0 && dynamicQuaternionCount == 0 && part.DynamicFrames.Count != 0)
                throw new InvalidDataException($"Part {partIndex}: version 8 dynamic frames must contain a mapped track.");

            foreach (var frame in part.DynamicFrames)
            {
                if (frame.Transforms.Count != dynamicTransformCount || frame.Quaternion.Count != dynamicQuaternionCount)
                    throw new InvalidDataException($"Part {partIndex}: version 8 dynamic frame data does not match its mappings.");
            }
        }

        // A v8 file has no mapping table: ids are implied by bone order, so they must count up from 0 in that order
        static int ValidateMappingIds(IReadOnlyList<AnimationBoneMapping> mappings, AnimationBoneMappingType mappingType, int partIndex)
        {
            var expectedId = 0;
            foreach (var mapping in mappings)
            {
                if (mapping.MappingType != mappingType)
                    continue;
                if (mapping.Id != expectedId)
                    throw new InvalidDataException($"Part {partIndex}: version 8 animation mapping ids must follow bone order.");
                expectedId++;
            }
            return expectedId;
        }
    }
}
