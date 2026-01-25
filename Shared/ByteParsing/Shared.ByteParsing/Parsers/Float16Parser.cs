using Half = SharpDX.Half;

namespace Shared.ByteParsing.Parsers
{
    public class Float16Parser : NumberParser<Half>
    {
        public override string TypeName { get { return "Float16"; } }
        public override DbTypesEnum Type => DbTypesEnum.Float16;
        protected override int FieldSize => 2;

        protected override Half Decode(byte[] buffer, int index)
        {
            var u = BitConverter.ToUInt16(buffer, index);
            return new Half(u);
        }

        public override byte[] EncodeValue(Half value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value.RawValue);
        }

        public override byte[] Encode(string value, out string? error)
        {

            if (!float.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(new Half(spesificValue), out error);
        }

        public override bool TryDecodeValue(byte[] buffer, int index, out Half value, out int bytesRead, out string? _error)
        {
            var res = base.TryDecodeValue(buffer, index, out value, out bytesRead, out _error);
            if (res)
            {
                if (float.IsNaN(value))
                {
                    bytesRead = 0;
                    _error = "Value is NAN";
                    return false;
                }
            }

            return res;
        }
    }
}
