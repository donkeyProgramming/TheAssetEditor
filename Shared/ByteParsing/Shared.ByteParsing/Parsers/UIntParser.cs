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
    }
}
