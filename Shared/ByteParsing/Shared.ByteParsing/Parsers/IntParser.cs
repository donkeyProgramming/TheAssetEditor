namespace Shared.ByteParsing.Parsers
{
    public class IntParser : NumberParser<int>
    {
        public override string TypeName { get { return "Int32"; } }
        public override DbTypesEnum Type => DbTypesEnum.Integer;

        protected override int FieldSize => 4;

        protected override int Decode(byte[] buffer, int index)
        {
            return BitConverter.ToInt32(buffer, index);
        }

        public override byte[] EncodeValue(int value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!int.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }
}
