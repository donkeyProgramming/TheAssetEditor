using System.Text;

namespace Shared.ByteParsing.Parsers
{
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

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = TryDecodeValue(buffer, index, out var value, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            return value;
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
}
