namespace Shared.ByteParsing.Parsers
{
    public class UIntParser : NumberParser<uint>
    {
        public override string TypeName { get { return "UInt32"; } }
        public override DbTypesEnum Type => DbTypesEnum.Integer;

        protected override int FieldSize => 4;

        protected override uint Decode(byte[] buffer, int index)
        {
            return BitConverter.ToUInt32(buffer, index);
        }

        public override byte[]? EncodeValue(uint value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[]? Encode(string value, out string? error)
        {
            if (!uint.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }

        public override byte[] Encode(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is uint v)
            {
                var bytes = EncodeValue(v, out var error);
                if (bytes == null) throw new Exception(error);
                return bytes;
            }

            if (value is string s)
            {
                var bytes = Encode(s, out var error);
                if (bytes == null) throw new Exception(error);
                return bytes;
            }

            try
            {
                var converted = Convert.ToUInt32(value);
                var bytes = EncodeValue(converted, out var error);
                if (bytes == null) throw new Exception(error);
                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to convert object to UInt32", ex);
            }
        }
    }
}
