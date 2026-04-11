using Shared.ByteParsing;

namespace Shared.Core.Misc
{
    public static class StringSanitizer
    {
        public static string FixedString(string str)
        {
            var idx = str.IndexOf('\0');
            if (idx != -1)
                return str.Substring(0, idx);
            return str;
        }

        public static bool IsAllCapsCaString(int index, byte[] data)
        {
            if (ByteParsers.String.TryDecode(data, index, out var tagName, out _, out _))
            {
                if (string.IsNullOrWhiteSpace(tagName))
                    return false;
                if (tagName.Length < 4)
                    return false;
                var allCaps = tagName.All(c => char.IsUpper(c) || c == '_' || c == ' ' || char.IsNumber(c));
                return allCaps;
            }

            return false;
        }
    }
}
