namespace Shared.ByteParsing.Parsers
{
    public class SingleParser : NumberParser<float>
    {
        public override string TypeName { get { return "Float"; } }
        public override DbTypesEnum Type => DbTypesEnum.Single;
        protected override int FieldSize => 4;

        protected override float Decode(byte[] buffer, int index)
        {
            return BitConverter.ToSingle(buffer, index);
        }

        public override bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? _error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out _error);
            value = temp.ToString("0.00000000");
            //value = temp.ToString();
            return result;
        }

        public override byte[] EncodeValue(float value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!float.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }
}
