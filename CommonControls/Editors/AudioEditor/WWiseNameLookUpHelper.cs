using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public class WWiseNameLookUpHelper
    {
        Dictionary<uint, string> _hashValueMap = new Dictionary<uint, string>();

        public void AddNames(string[] names)
        {
            foreach (var name in names)
            {
                var hashVal = ComputeWWiseHash(name);
                _hashValueMap[hashVal] = name;
            }
        }

        public void AddNames(List<string> names)
        {
            foreach (var name in names)
            {
                var hashVal = ComputeWWiseHash(name);
                _hashValueMap[hashVal] = name;
            }
        }

        public static uint ComputeWWiseHash(string name)
        {
            var lower = name.ToLower().Trim();
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

        public string GetName(uint value, out bool found)
        {
            found = _hashValueMap.ContainsKey(value);
            if (found)
                return _hashValueMap[value];
            return value.ToString();
        }

        public string GetName(uint value) => GetName(value, out var _);
    }
}
