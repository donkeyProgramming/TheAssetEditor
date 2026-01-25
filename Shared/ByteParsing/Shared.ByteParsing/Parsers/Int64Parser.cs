namespace Shared.ByteParsing.Parsers
{
    public class Int64Parser : NumberParser<long>
    {
        public override string TypeName { get { return "Int64"; } }
        public override DbTypesEnum Type => DbTypesEnum.Int64;

        protected override int FieldSize => 8;

        protected override long Decode(byte[] buffer, int index)
        {
            return BitConverter.ToInt64(buffer, index);
        }

        public override byte[] EncodeValue(long value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!long.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }
}
