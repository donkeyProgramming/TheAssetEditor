namespace Shared.ByteParsing.Parsers
{
    public class ByteParser : NumberParser<byte>
    {
        public override string TypeName { get { return "Byte"; } }
        public override DbTypesEnum Type => DbTypesEnum.Byte;

        protected override int FieldSize => 1;

        protected override byte Decode(byte[] buffer, int index)
        {
            return buffer[index];
        }

        public override byte[]? EncodeValue(byte value, out string? error)
        {
            error = null;
            return new byte[] { value };
        }

        public override byte[]? Encode(string value, out string? error)
        {
            if (!byte.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }

        public byte[] ReadArray(byte[] buffer, int index, int count)
        {
            var destination = new byte[count];
            Array.Copy(buffer, index, destination, 0, count);
            return destination;
        }
    }
}
