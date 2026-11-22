using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Animation
{
    static class AnimationFileLoadHelpers
    {
        public static RmvVector3 Decode_translation_24_888_ranged(sbyte[] bytes, AnimationFile.AnimationV8OptimizationData optimizationData, int boneIndex)
        {
            var (scale, centre) = optimizationData.Range_map_translations[boneIndex];
            return new RmvVector3(Decode(bytes[0], scale.X, centre.X), Decode(bytes[1], scale.Y, centre.Y), Decode(bytes[2], scale.Z, centre.Z));
        }

        public static RmvVector4 Decode_quaternion_32_s8888_ranged(sbyte[] bytes, AnimationFile.AnimationV8OptimizationData optimizationData, int boneIndex)
        {
            var (scale, centre) = optimizationData.Range_map_quaternion[boneIndex];
            return new RmvVector4(Decode(bytes[0], scale.X, centre.X), Decode(bytes[1], scale.Y, centre.Y), Decode(bytes[2], scale.Z, centre.Z), Decode(bytes[3], scale.W, centre.W));
        }

        // centre + (n / 127) * scale, in double and rounded once. Some vanilla ranges have a centre millions of times
        // their scale, where doing this in float rounds twice and lands on the neighbouring byte's value.
        static float Decode(sbyte value, float scale, float centre) => (float)(centre + value / 127.0 * scale);

        // The inverse of Decode, clamped to a signed byte. Also in double, for the same reason.
        // The byte the value was read from wins while it still decodes to the value: near an extreme range several
        // bytes decode to the same float, and only the original says which one the file had.
        public static sbyte Encode(float value, float scale, float centre, sbyte? original)
        {
            if (scale == 0)
                return 0;   // the channel can only say its centre (a refit of a constant channel)
            if (original.HasValue && Decode(original.Value, scale, centre) == value)
                return original.Value;
            var n = Math.Round(((double)value - centre) / scale * 127.0, MidpointRounding.ToEven);
            return (sbyte)Math.Clamp(n, -128, 127);
        }
    }
}
