using System.Text;

namespace Shared.ByteParsing.Parsers
{
    public class FixedStringParser : NumberParser<string>
    {
        public override string TypeName { get { return "FixedString[" + FieldSize / 2 + "]"; } }
        public override DbTypesEnum Type => DbTypesEnum.FixedString;

        protected override int FieldSize => _stringLength * 2;

        int _stringLength;

        public FixedStringParser(int length)
        {
            _stringLength = length;
        }

        protected override string Decode(byte[] buffer, int index)
        {
            return Encoding.Unicode.GetString(buffer, index, FieldSize);
        }

        public override byte[] EncodeValue(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode(string value, out string? error)
        {
            throw new NotImplementedException();
        }
    }
}
