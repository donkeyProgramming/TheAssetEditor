namespace Shared.ByteParsing.Parsers
{
    public class BoolParser : SpesificByteParser<bool>
    {
        public DbTypesEnum Type => DbTypesEnum.Boolean;

        public string TypeName { get { return "Bool"; } }

        protected int FieldSize => 1;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }
            var value = buffer[index];
            if (!(value == 1 || value == 0))
            {
                bytesRead = 0;
                _error = value + " is not a valid bool";
                return false;
            }

            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        public byte[] Encode(string value, out string? error)
        {
            if (!bool.TryParse(value, out var _res))
            {
                error = "Unable to convert value to bool";
                return null;
            }
            error = null;
            return Write(_res);
        }

        public byte[] EncodeValue(bool value, out string? error)
        {
            error = null;
            return Write(value);
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? _error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out _error);
            value = temp.ToString();
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out bool value, out int bytesRead, out string? _error)
        {
            value = false;
            var canDecode = CanDecode(buffer, index, out bytesRead, out _error);
            if (canDecode)
                value = buffer[index] == 1;
            return canDecode;
        }

        byte[] Write(bool value)
        {
            if (value)
                return new byte[1] { 1 };
            else
                return new byte[1] { 0 };
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = TryDecodeValue(buffer, index, out var value, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            return value;
        }
    }
}
