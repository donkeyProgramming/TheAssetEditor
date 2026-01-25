using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Shared.ByteParsing.Parsers
{
    public class Vector3Parser : SpesificByteParser<Vector3>
    {
        public string TypeName => "Vector3";

        public DbTypesEnum Type => DbTypesEnum.Vector3;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error)
        {
            return TryDecodeValue(buffer, index, out var _, out bytesRead, out error);
        }

        public byte[]? Encode(string value, out string? error)
        {
            var split = value.Split("|");
            if (split.Length != 3)
            {
                error = "Value must contain 3 numbers seperated by '|'";
                return null;
            }

            var x = ByteParsers.Single.Encode(split[0], out error);
            if (x == null)
                return null;

            var y = ByteParsers.Single.Encode(split[1], out error);
            if (y == null)
                return null;

            var z = ByteParsers.Single.Encode(split[2], out error);
            if (z == null)
                return null;

            var combined = new byte[12];
            Array.Copy(x, 0, combined, 0, 4);
            Array.Copy(y, 0, combined, 4, 4);
            Array.Copy(z, 0, combined, 8, 4);
            return combined;
        }

        public byte[]? EncodeValue(Vector3 value, out string? error)
        {
            var x = ByteParsers.Single.EncodeValue(value.X, out error);
            if (x == null)
                return null;

            var y = ByteParsers.Single.EncodeValue(value.Y, out error);
            if (y == null)
                return null;

            var z = ByteParsers.Single.EncodeValue(value.Z, out error);
            if (z == null)
                return null;

            var combined = new byte[12];
            Array.Copy(x, 0, combined, 0, 4);
            Array.Copy(y, 0, combined, 4, 4);
            Array.Copy(z, 0, combined, 8, 4);
            return combined;
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            var result = TryDecodeValue(buffer, index, out var typedValue, out bytesRead, out error);
            value = $"{typedValue.X},{typedValue.Y},{typedValue.Z}";
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out Vector3 value, out int bytesRead, out string? error)
        {
            var x = ByteParsers.Single.TryDecodeValue(buffer, index + 0, out var xValue, out bytesRead, out error);
            var y = ByteParsers.Single.TryDecodeValue(buffer, index + 4, out var yValue, out bytesRead, out error);
            var z = ByteParsers.Single.TryDecodeValue(buffer, index + 8, out var zValue, out bytesRead, out error);
            bytesRead = 12;
            value = new Vector3(xValue, yValue, zValue);
            return x && y && z;
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
