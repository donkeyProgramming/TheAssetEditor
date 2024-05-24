using System.Text;
using Half = SharpDX.Half;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Shared.Core.ByteParsing
{
    public enum DbTypesEnum
    {
        Byte,
        String,
        String_ascii,
        FixedString,
        FixedStringAcii,
        Optstring,
        Optstring_ascii,
        Int64,
        Integer,
        uint32,
        Short,
        UShort,
        Single,
        Float16,
        Boolean,
        StringLookup,
        List,
        Vector3,
        Vector4
    }

    public interface IByteParser
    {
        string TypeName { get; }
        DbTypesEnum Type { get; }
        bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error);
        bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error);
        byte[] Encode(string value, out string? error);
        string DefaultValue();
        object GetValueAsObject(byte[] buffer, int index, out int bytesRead);
    }

    public interface SpesificByteParser<T> : IByteParser
    {
        bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string? error);
        byte[] EncodeValue(T value, out string? error);
    }

    public abstract class NumberParser<T> : SpesificByteParser<T>
    {
        protected abstract int FieldSize { get; }
        public abstract DbTypesEnum Type { get; }

        public abstract string TypeName { get; }

        protected abstract T Decode(byte[] buffer, int index);
        public abstract byte[] EncodeValue(T value, out string? error);
        public abstract byte[] Encode(string value, out string? error);

        public virtual bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }
            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        public virtual bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out error);
            value = temp?.ToString();
            return result;
        }

        public virtual bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string? error)
        {
            value = default;
            var canDecode = CanDecode(buffer, index, out bytesRead, out error);
            if (canDecode)
                value = Decode(buffer, index);
            return canDecode;
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = CanDecode(buffer, index, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            var value = Decode(buffer, index) as object;
            return value;
        }

        public string DefaultValue()
        {
            return "0";
        }
    }

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

    public class UIntParser : NumberParser<uint>
    {
        public override string TypeName { get { return "UInt32"; } }
        public override DbTypesEnum Type => DbTypesEnum.Integer;

        protected override int FieldSize => 4;

        protected override uint Decode(byte[] buffer, int index)
        {
            return BitConverter.ToUInt32(buffer, index);
        }

        public override byte[] EncodeValue(uint value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!uint.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }

    public class ByteParser : NumberParser<byte>
    {
        public override string TypeName { get { return "Byte"; } }
        public override DbTypesEnum Type => DbTypesEnum.Byte;

        protected override int FieldSize => 1;

        protected override byte Decode(byte[] buffer, int index)
        {
            return buffer[index];
        }

        public override byte[] EncodeValue(byte value, out string? error)
        {
            error = null;
            return new byte[] { value };
        }

        public override byte[] Encode(string value, out string? error)
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


    public class Vector3Parser : SpesificByteParser<Vector3>
    {
        public string TypeName => "Vector3";

        public DbTypesEnum Type => DbTypesEnum.Vector3;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error)
        {
            return TryDecodeValue(buffer, index, out var _, out bytesRead, out error);
        }

        public string DefaultValue()
        {
            return "0|0|0";
        }

        public byte[] Encode(string value, out string? error)
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

        public byte[] EncodeValue(Vector3 value, out string? error)
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

    public class Vector4Parser : SpesificByteParser<Vector4>
    {
        public string TypeName => "Vector4";

        public DbTypesEnum Type => DbTypesEnum.Vector4;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error)
        {
            return TryDecodeValue(buffer, index, out var _, out bytesRead, out error);
        }

        public string DefaultValue()
        {
            return "0|0|0|1";
        }

        public byte[] Encode(string value, out string? error)
        {
            var split = value.Split("|");
            if (split.Length != 4)
            {
                error = "Value must contain 4 numbers seperated by '|'";
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

            var w = ByteParsers.Single.Encode(split[3], out error);
            if (w == null)
                return null;

            var combined = new byte[16];
            Array.Copy(x, 0, combined, 0, 4);
            Array.Copy(y, 0, combined, 4, 4);
            Array.Copy(z, 0, combined, 8, 4);
            Array.Copy(w, 0, combined, 12, 4);
            return combined;
        }

        public byte[] EncodeValue(Vector4 value, out string? error)
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

            var w = ByteParsers.Single.EncodeValue(value.W, out error);
            if (w == null)
                return null;

            var combined = new byte[16];
            Array.Copy(x, 0, combined, 0, 4);
            Array.Copy(y, 0, combined, 4, 4);
            Array.Copy(z, 0, combined, 8, 4);
            Array.Copy(w, 0, combined, 12, 4);
            return combined;
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            var result = TryDecodeValue(buffer, index, out var typedValue, out bytesRead, out error);
            value = $"{typedValue.X},{typedValue.Y},{typedValue.Z},{typedValue.W}";
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out Vector4 value, out int bytesRead, out string? error)
        {
            var x = ByteParsers.Single.TryDecodeValue(buffer, index + 0, out var xValue, out bytesRead, out error);
            var y = ByteParsers.Single.TryDecodeValue(buffer, index + 4, out var yValue, out bytesRead, out error);
            var z = ByteParsers.Single.TryDecodeValue(buffer, index + 8, out var zValue, out bytesRead, out error);
            var w = ByteParsers.Single.TryDecodeValue(buffer, index + 12, out var wValue, out bytesRead, out error);
            bytesRead = 16;
            value = new Vector4(xValue, yValue, zValue, wValue);
            return x && y && z && w;
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = TryDecodeValue(buffer, index, out var value, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            return value;
        }
    }

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

    public class UShortParser : NumberParser<ushort>
    {
        public override string TypeName { get { return "UShort"; } }
        public override DbTypesEnum Type => DbTypesEnum.UShort;
        protected override int FieldSize => 2;

        protected override ushort Decode(byte[] buffer, int index)
        {
            return BitConverter.ToUInt16(buffer, index);
        }

        public override byte[] EncodeValue(ushort value, out string? error)
        {
            error = null;
            return BitConverter.GetBytes(value);
        }

        public override byte[] Encode(string value, out string? error)
        {
            if (!ushort.TryParse(value, out var spesificValue))
            {
                error = "Unable to convert string to value";
                return null;
            }

            return EncodeValue(spesificValue, out error);
        }
    }

    public class BoolParser : SpesificByteParser<bool>
    {
        public DbTypesEnum Type => DbTypesEnum.Boolean;

        public string TypeName { get { return "Bool"; } }

        protected int FieldSize => 1;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }
            var value = buffer[index];
            if (!(value == 1 || value == 0))
            {
                bytesRead = 0;
                _error = value + " is not a valid bool";
                return false;
            }

            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        public string DefaultValue()
        {
            return "false";
        }

        public byte[] Encode(string value, out string? error)
        {
            if (!bool.TryParse(value, out var _res))
            {
                error = "Unable to convert value to bool";
                return null;
            }
            error = null;
            return Write(_res);
        }

        public byte[] EncodeValue(bool value, out string? error)
        {
            error = null;
            return Write(value);
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? _error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out _error);
            value = temp.ToString();
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out bool value, out int bytesRead, out string? _error)
        {
            value = false;
            var canDecode = CanDecode(buffer, index, out bytesRead, out _error);
            if (canDecode)
                value = buffer[index] == 1;
            return canDecode;
        }

        byte[] Write(bool value)
        {
            if (value)
                return new byte[1] { 1 };
            else
                return new byte[1] { 0 };
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = TryDecodeValue(buffer, index, out var value, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            return value;
        }
    }

    public class StringParser : SpesificByteParser<string>
    {
        virtual public DbTypesEnum Type => DbTypesEnum.String;

        virtual protected Encoding StringEncoding => Encoding.UTF8;
        virtual protected bool IsOptStr => false;

        public virtual string TypeName { get { return "String"; } }

        bool TryReadReadCAStringAsArray(byte[] buffer, int index, Encoding encoding, bool isOptString,
             out string? errorMessage, out int stringStart, out int stringLength, out int bytesInString)
        {
            stringStart = 0;
            stringLength = 0;
            bytesInString = 0;
            var bytesLeft = buffer.Length - index;

            var offset = 0;
            var readTheString = true;
            if (isOptString)
            {
                if (bytesLeft < 1)
                {
                    errorMessage = $"Cannot read optString flag {bytesLeft} bytes left";
                    return false;
                }

                var flag = buffer[index];
                if (flag == 0)
                {
                    readTheString = false;
                }
                else if (flag != 1)
                {
                    errorMessage = $"Invalid flag {flag} at beginnning of optStr";
                    return false;
                }
                offset += 1;
                bytesLeft -= 1;
            }

            if (readTheString)
            {
                if (bytesLeft < 2)
                {
                    errorMessage = $"Cannot read length of string {bytesLeft} bytes left";
                    return false;
                }

                int num = BitConverter.ToInt16(buffer, index + offset);
                bytesLeft -= 2;
                if (0 > num)
                {
                    errorMessage = "Negative file length";
                    return false;
                }

                // Unicode is 2 bytes per character; UTF8 is variable, but the number stored is the number of bytes, so use that
                var bytes = (encoding == Encoding.Unicode ? 2 : 1) * num;
                // enough data left?
                if (bytesLeft < bytes)
                {
                    errorMessage = string.Format("Cannot read string of length {0}: only {1} bytes left", bytes, bytesLeft);
                    return false;
                }

                if (isOptString && bytes == 0)
                {
                    errorMessage = "Opstring with size = 0";
                    return false;
                }

                stringStart = index + 2 + offset;
                stringLength = bytes;
                bytesInString = bytes + 2;
            }

            bytesInString += offset;
            errorMessage = null;
            return true;
        }

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error)
        {
            return TryReadReadCAStringAsArray(buffer, index, StringEncoding, IsOptStr, out error, out _, out _, out bytesRead);
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            return TryDecodeValue(buffer, index, out value, out bytesRead, out error);
        }

        public bool TryDecodeValue(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            value = null;
            var result = TryReadReadCAStringAsArray(buffer, index, StringEncoding, IsOptStr, out error, out var stringStrt, out var stringLength, out bytesRead);
            if (result)
            {
                if (stringLength != 0)
                    value = StringEncoding.GetString(buffer, stringStrt, stringLength);
                else
                    value = "";
            }


            return result;
        }

        public bool TryDecodeFixedLength(byte[] buffer, int index, int length, out string value, out int bytesRead)
        {
            value = StringEncoding.GetString(buffer, index, length);
            bytesRead = length;
            return true;
        }


        public bool TryDecodeZeroTerminatedString(byte[] buffer, int index, out string value, out int bytesRead)
        {
            bytesRead = 1;
            while (buffer[index] != 0)
                bytesRead++;

            value = BitConverter.ToString(buffer, index, bytesRead);
            return true;
        }

        public byte[] WriteCaString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (IsOptStr)
                    return new byte[] { 0 };
                else
                    return BitConverter.GetBytes((short)0);
            }



            var byteLength = BitConverter.GetBytes((short)value.Length);
            var byteStr = StringEncoding.GetBytes(value);

            var stringWithCountAtFront = byteLength.Concat(byteStr).ToArray();

            if (IsOptStr)
            {
                if (value == null || value.Length == 0)
                    return new byte[] { 0 };

                return new byte[] { 1 }.Concat(stringWithCountAtFront).ToArray();
            }
            else
            {
                return stringWithCountAtFront;
            }
        }

        public byte[] EncodeValue(string value, out string? error)
        {
            error = null;
            return WriteCaString(value);
        }

        public byte[] Encode(string value, out string? error)
        {
            return EncodeValue(value, out error);
        }

        public string DefaultValue()
        {
            return "";
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = TryDecodeValue(buffer, index, out var value, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            return value;
        }
    }

    public class FixedAciiStringParser : NumberParser<string>
    {
        public override string TypeName { get { return "FixedAsciiString[" + FieldSize + "]"; } }
        public override DbTypesEnum Type => DbTypesEnum.FixedStringAcii;

        protected override int FieldSize => _stringLength;

        int _stringLength;

        public FixedAciiStringParser(int length)
        {
            _stringLength = length;
        }

        protected override string Decode(byte[] buffer, int index)
        {
            return Encoding.ASCII.GetString(buffer, index, FieldSize);
        }

        public override byte[] EncodeValue(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }

            for (var i = 0; i < FieldSize; i++)
            {
                var b = buffer[index + i];
                //var t = Encoding.ASCII.GetString(buffer, index + i, 1)[0];
                var isAscii = b < 128;
                if (!isAscii)
                {
                    bytesRead = 0;
                    _error = "Contains invalid ANCII chars : " + b;
                    return false;
                }
            }

            var value = Decode(buffer, index);
            var hasInvalidChars = FilePathHasInvalidChars(value);
            if (hasInvalidChars)
                _error = "Contains invalid chars for path : " + value;

            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }
    }

    public class FixedStringParser : NumberParser<string>
    {
        public override string TypeName { get { return "FixedString[" + FieldSize / 2 + "]"; } }
        public override DbTypesEnum Type => DbTypesEnum.FixedString;

        protected override int FieldSize => _stringLength * 2;

        int _stringLength;

        public FixedStringParser(int length)
        {
            _stringLength = length;
        }

        protected override string Decode(byte[] buffer, int index)
        {
            return Encoding.Unicode.GetString(buffer, index, FieldSize);
        }

        public override byte[] EncodeValue(string value, out string? error)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode(string value, out string? error)
        {
            throw new NotImplementedException();
        }
    }

    public class StringAsciiParser : StringParser
    {
        public override string TypeName { get { return "StringAscii"; } }
        public override DbTypesEnum Type => DbTypesEnum.String_ascii;
        protected override Encoding StringEncoding => Encoding.Unicode;
        protected override bool IsOptStr => false;
    }

    public class OptionalStringParser : StringParser
    {
        public override string TypeName { get { return "Optstring"; } }
        public override DbTypesEnum Type => DbTypesEnum.Optstring;
        protected override Encoding StringEncoding => Encoding.UTF8;
        protected override bool IsOptStr => true;
    }

    public class OptionalStringAsciiParser : StringParser
    {
        public override string TypeName { get { return "OptStringAscii"; } }
        public override DbTypesEnum Type => DbTypesEnum.Optstring_ascii;
        protected override Encoding StringEncoding => Encoding.Unicode;
        protected override bool IsOptStr => true;
    }

    public static class ByteParsers
    {
        public static ByteParser Byte { get; set; } = new ByteParser();
        public static IntParser Int32 { get; set; } = new IntParser();

        public static Int64Parser Int64 { get; set; } = new Int64Parser();
        public static UIntParser UInt32 { get; set; } = new UIntParser();
        public static SingleParser Single { get; set; } = new SingleParser();
        public static Vector3Parser Vector3 { get; set; } = new Vector3Parser();
        public static Vector4Parser Vector4 { get; set; } = new Vector4Parser();
        public static Float16Parser Float16 { get; set; } = new Float16Parser();
        public static ShortParser Short { get; set; } = new ShortParser();
        public static UShortParser UShort { get; set; } = new UShortParser();
        public static BoolParser Bool { get; set; } = new BoolParser();
        public static OptionalStringParser OptString { get; set; } = new OptionalStringParser();
        public static StringParser String { get; set; } = new StringParser();
        public static OptionalStringAsciiParser OptStringAscii { get; set; } = new OptionalStringAsciiParser();
        public static StringAsciiParser StringAscii { get; set; } = new StringAsciiParser();

        public static IByteParser[] GetAllParsers() { return new IByteParser[] { Byte, Int32, Int64, UInt32, Single, Float16, Short, UShort, Bool, OptString, String, OptStringAscii, StringAscii, new FixedAciiStringParser(1), new FixedStringParser(1) }; }
    }
}
