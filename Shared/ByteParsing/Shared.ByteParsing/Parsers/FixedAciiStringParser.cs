using System.Text;

namespace Shared.ByteParsing.Parsers
{
    public class FixedAciiStringParser : NumberParser<string>
    {
        public override string TypeName { get { return "FixedAsciiString[" + FieldSize + "]"; } }
        public override DbTypesEnum Type => DbTypesEnum.FixedStringAcii;

        protected override int FieldSize => _stringLength;

        int _stringLength;

        public FixedAciiStringParser(int length)
        {
            _stringLength = length;
        }

        protected override string Decode(byte[] buffer, int index)
        {
            return Encoding.ASCII.GetString(buffer, index, FieldSize);
        }

        public override byte[] EncodeValue(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }

            for (var i = 0; i < FieldSize; i++)
            {
                var b = buffer[index + i];
                //var t = Encoding.ASCII.GetString(buffer, index + i, 1)[0];
                var isAscii = b < 128;
                if (!isAscii)
                {
                    bytesRead = 0;
                    _error = "Contains invalid ANCII chars : " + b;
                    return false;
                }
            }

            var value = Decode(buffer, index);
            var hasInvalidChars = FilePathHasInvalidChars(value);
            if (hasInvalidChars)
                _error = "Contains invalid chars for path : " + value;

            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }
    }
}
