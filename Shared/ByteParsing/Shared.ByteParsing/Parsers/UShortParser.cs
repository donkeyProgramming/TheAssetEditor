namespace Shared.ByteParsing.Parsers
{
    public class UShortParser : NumberParser<ushort>
    {
        public override string TypeName { get { return "UShort"; } }
        public override DbTypesEnum Type => DbTypesEnum.UShort;
        protected override int FieldSize => 2;

        protected override ushort Decode(byte[] buffer, int index)
        {
            return BitConverter.ToUInt16(buffer, index);
        }

        public override byte[]? EncodeValue(ushort value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[]? Encode(string value, out string? error)
        {
            if (!ushort.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }
}
