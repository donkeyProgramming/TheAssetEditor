using System.Text;

namespace Editors.Audio.Shared.Wwise
{
    public static class WwiseHash
    {
        public static uint Compute(string key)
        {
            var normalisedKey = key.ToLower().Trim();

            var fnvOffsetBasis = 2166136261; // FNV‑1 32‑bit offset basis (initial hash value)
            uint fnvPrime = 16777619; // FNV‑1 32‑bit prime (multiplier used in each round)

            var hash = fnvOffsetBasis;
            var bytes = Encoding.UTF8.GetBytes(normalisedKey);

            foreach (var b in bytes)
                hash = (hash * fnvPrime) ^ b;

            return hash;
        }
    }
}
