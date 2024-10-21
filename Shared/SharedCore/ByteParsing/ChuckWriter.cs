namespace Shared.Core.ByteParsing
{
    public class ChuckWriter
    {
        readonly List<byte> _bytes = new List<byte>();

        public void Write<T>(T value, SpesificByteParser<T> parser)
        {
            var bytes = parser.EncodeValue(value, out _);
            _bytes.AddRange(bytes);
        }

        public void WriteStringTableIndex(string str, ref List<string> stringTable, bool forceLowercase = true)
        {
            if (string.IsNullOrEmpty(str))
                str = "";
            if(forceLowercase)
                str = str.ToLower().Trim();
            var writeIndex = stringTable.IndexOf(str);
            if (writeIndex == -1)
            {
                stringTable.Add(str);
                writeIndex = stringTable.Count - 1;
            }
            Write(writeIndex, ByteParsers.Int32);
        }

        public void AddBytes(byte[] bytes)
        {
            _bytes.AddRange(bytes);
        }

        public byte[] GetBytes()
        {
            return _bytes.ToArray();
        }
    }
}
