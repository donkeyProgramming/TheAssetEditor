using System.Text;

namespace Editors.Audio.Utility
{
    public static class WwiseHash
    {
        public static uint Compute(string name)
        {
            var lower = name.ToLower().Trim();
            var bytes = Encoding.UTF8.GetBytes(lower);

            var hashValue = 2166136261;
            for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var nameByte = bytes[byteIndex];
                hashValue = hashValue * 16777619;
                hashValue = hashValue ^ nameByte;
                hashValue = hashValue & 0xFFFFFFFF;
            }

            return hashValue;
        }
    }
}
