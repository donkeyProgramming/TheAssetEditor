using System.Collections.Generic;
using System.Text;

namespace Audio.Utility
{
    public class NameLookupHelper
    {
        Dictionary<uint, string> _hashValueMap = new Dictionary<uint, string>();

        public NameLookupHelper(string[] names)
        {
            foreach (var name in names)
            {
                var hashVal = Hash(name);
                _hashValueMap[hashVal] = name;
            }
        }

        public static uint Hash(string value)
        {
            var lower = value.ToLower();
            var bytes = Encoding.UTF8.GetBytes(lower);

            uint hashValue = 2166136261;
            for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var nameByte = bytes[byteIndex];
                hashValue = hashValue * 16777619;
                hashValue = hashValue ^ nameByte;
                hashValue = hashValue & 0xFFFFFFFF;
            }

            return hashValue;
        }

        public string GetName(uint value)
        {
            if (_hashValueMap.ContainsKey(value))
                return _hashValueMap[value];
            return value.ToString();
        }
    }
}
