namespace Shared.ByteParsing.Parsers
{
    public class ShortParser : NumberParser<short>
    {
        public override string TypeName { get { return "Short"; } }
        public override DbTypesEnum Type => DbTypesEnum.Short;
        protected override int FieldSize => 2;

        protected override short Decode(byte[] buffer, int index)
        {
            return BitConverter.ToInt16(buffer, index);
        }

        public override byte[] EncodeValue(short value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!short.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }
}
