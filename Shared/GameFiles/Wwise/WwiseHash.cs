using System.Text;

namespace Shared.GameFormats.Wwise
{
    public static class WwiseHash
    {
        private const uint FnvOffsetBasis = 2166136261u;
        private const uint FnvPrime = 16777619u;

        public static uint Compute(string key)
        {
            var normalisedKey = key.ToLower().Trim();
            var bytes = Encoding.UTF8.GetBytes(normalisedKey);
            return Compute(bytes);
        }

        public static uint Compute(byte[] bytes)
        {
            var hash = FnvOffsetBasis;
            foreach (var b in bytes)
                hash = (hash * FnvPrime) ^ b;
            return hash;
        }
    }
}
