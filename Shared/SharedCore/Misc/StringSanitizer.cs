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
    }
}
