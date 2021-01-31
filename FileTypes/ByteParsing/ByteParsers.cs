using Common.SystemHalf;
using Filetypes.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Filetypes.ByteParsing
{
    public enum DbTypesEnum
    {
        Byte,
        String,
        String_ascii,
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
        List,
    }

    public interface IByteParser
    {
        string TypeName { get; }
        DbTypesEnum Type { get; }
        bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string error);
        bool CanDecode(byte[] buffer, int index, out int bytesRead, out string error);
        //public abstract bool Encode();
    }

    public interface SpesificByteParser<T> : IByteParser
    {
        bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string error);
    }

    public abstract class NumberParser<T> : SpesificByteParser<T>
    {
        protected abstract int FieldSize { get; }
        public abstract DbTypesEnum Type { get; }

        public abstract string TypeName { get; }

        protected abstract T Decode(byte[] buffer, int index);

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string _error)
        {
            if (buffer.Length - index < FieldSize)
            {
                bytesRead = 0;
                _error = "Not enough space in stream";
                return false;
            }
            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string _error)
        {
            var result = TryDecodeValue(buffer, index, out T temp, out bytesRead, out _error);
            value = temp.ToString();
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string _error)
        {
            value = default;
            bool canDecode = CanDecode(buffer, index, out bytesRead, out _error);
            if (canDecode)
                value = Decode(buffer, index);
            return canDecode;
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
    }

    public class Float16Parser : NumberParser<Half>
    {
        public override string TypeName { get { return "Float16"; } }
        public override DbTypesEnum Type => DbTypesEnum.Float16;
        protected override int FieldSize => 2;

        protected override Half Decode(byte[] buffer, int index)
        {
            return Half.ToHalf(buffer, index);
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
    }

    public class BoolParser : SpesificByteParser<bool>
    {
        public DbTypesEnum Type => DbTypesEnum.Boolean;

        public string TypeName { get { return "Bool"; } }

        protected int FieldSize => 1;

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string _error)
        {
            if (buffer.Length - index < FieldSize)
            {
                bytesRead = 0;
                _error = "Not enough space in stream";
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

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string _error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out _error);
            value = temp.ToString();
            return result;
        }

        public bool TryDecodeValue(byte[] buffer, int index, out bool value, out int bytesRead, out string _error)
        {
            value = false;
            bool canDecode = CanDecode(buffer, index, out bytesRead, out _error);
            if (canDecode)
                value = (buffer[index] == 1);
            return canDecode;
        }

        public byte[] Write(bool value)
        {
            if (value)
                return new byte[1] { 1 };
            else
                return new byte[1] { 0 };
        }
    }

    public class StringParser : SpesificByteParser<string>
    {
        virtual public DbTypesEnum Type => DbTypesEnum.String;

        virtual protected Encoding StringEncoding => Encoding.UTF8;
        virtual protected bool IsOptStr => false;

        public virtual string TypeName { get { return "String"; } }

        bool TryReadReadCAStringAsArray(byte[] buffer, int index, Encoding encoding, bool isOptString,
             out string errorMessage, out int stringStart, out int stringLength, out int bytesInString)
        {
            stringStart = 0;
            stringLength = 0;
            bytesInString = 0;
            var bytesLeft = buffer.Length - index;

            int offset = 0;
            bool readTheString = true;
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
                int bytes = (encoding == Encoding.Unicode ? 2 : 1) * num;
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

                stringStart = (index + 2 + offset);
                stringLength = bytes;
                bytesInString = bytes + 2;
            }

            bytesInString += offset;
            errorMessage = null;
            return true;
        }

        public bool CanDecode(byte[] buffer, int index, out int bytesRead, out string error)
        {
            return TryReadReadCAStringAsArray(buffer, index, StringEncoding, IsOptStr, out error, out _, out _, out bytesRead);
        }

        public bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string error)
        {
            return TryDecodeValue(buffer, index, out value, out bytesRead, out error);
        }

        public bool TryDecodeValue(byte[] buffer, int index, out string value, out int bytesRead, out string error)
        {
            value = null;
            var result = TryReadReadCAStringAsArray(buffer, index, StringEncoding, IsOptStr, out error, out int stringStrt, out int stringLength, out bytesRead);
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
            if (IsOptStr)
                throw new NotImplementedException();
            if(StringEncoding != Encoding.UTF8)
                throw new NotImplementedException();

            if (string.IsNullOrWhiteSpace(value))
                return BitConverter.GetBytes((Int16)0);

            var byteLength = BitConverter.GetBytes((Int16)value.Length);
            var byteStr = StringEncoding.GetBytes(value);

            return byteLength.Concat(byteStr).ToArray();
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
        public static Float16Parser Float16 { get; set; } = new Float16Parser();
        public static ShortParser Short { get; set; } = new ShortParser();
        public static UShortParser UShort { get; set; } = new UShortParser();
        public static BoolParser Bool { get; set; } = new BoolParser();
        public static OptionalStringParser OptString { get; set; } = new OptionalStringParser();
        public static StringParser String { get; set; } = new StringParser();
        public static OptionalStringAsciiParser OptStringAscii { get; set; } = new OptionalStringAsciiParser();
        public static StringAsciiParser StringAscii { get; set; } = new StringAsciiParser();

        public static IByteParser[] GetAllParsers() {  return new IByteParser[]{ Byte , Int32 , Int64 , UInt32 ,Single, Float16, Short, UShort, Bool, OptString, String, OptStringAscii, StringAscii}; }
    }

    public static class ByteParserFactory
    {
        public static IByteParser Create(DbTypesEnum typeEnum)
        {
            switch (typeEnum)
            {
                case DbTypesEnum.String:
                    return ByteParsers.String;

                case DbTypesEnum.String_ascii:
                    return ByteParsers.StringAscii;

                case DbTypesEnum.Optstring:
                    return ByteParsers.OptString;

                case DbTypesEnum.Optstring_ascii:
                    return ByteParsers.OptStringAscii;

                case DbTypesEnum.Integer:
                    return ByteParsers.Int32;

                case DbTypesEnum.Int64:
                    return ByteParsers.Int64;

                case DbTypesEnum.Short:
                    return ByteParsers.Short;

                case DbTypesEnum.UShort:
                    return ByteParsers.UShort;

                case DbTypesEnum.Single:
                    return ByteParsers.Single;

                case DbTypesEnum.Float16:
                    return ByteParsers.Float16;

                case DbTypesEnum.Boolean:
                    return ByteParsers.Bool;

                case DbTypesEnum.uint32:
                    return ByteParsers.UInt32;

                case DbTypesEnum.Byte:
                    return ByteParsers.Byte;

                case DbTypesEnum.List:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
        // From string?
        // FromEnum
        // From Ca type
        // ??
    }

    public class ByteChunk
    {
        byte[] _buffer;
        int _currentIndex;
        public ByteChunk(byte[] buffer, int index = 0)
        {
            _buffer = buffer;
            _currentIndex = index;
        }

        public void Reset()
        {
            Index = 0;
        }

        public static ByteChunk FromFile(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);
            return new ByteChunk(bytes);
        }

        public int BytesLeft => _buffer.Length - _currentIndex;
        public int Index { get { return _currentIndex; } set { _currentIndex = value;} }

        public byte[] Buffer { get { return _buffer; } }

        T Read<T>(SpesificByteParser<T> parser)
        {
            if (!parser.TryDecodeValue(_buffer, _currentIndex, out T value, out int bytesRead, out string error))
                throw new Exception("Unable to parse :" + error);

            _currentIndex += bytesRead;
            return value;
        }

        string ReadFixedLengthString(StringParser parser, int length)
        {
            if (!parser.TryDecodeFixedLength(_buffer, _currentIndex, length, out var value, out int bytesRead))
                throw new Exception("Unable to parse");

            _currentIndex += bytesRead;
            return value;
        }

        string ReadZeroTerminatedString(StringParser parser)
        {
            if (!parser.TryDecodeZeroTerminatedString(_buffer, _currentIndex, out var value, out int bytesRead))
                throw new Exception("Unable to parse");

            _currentIndex += bytesRead;
            return value;
            
        }

        T Peak<T>(SpesificByteParser<T> parser)
        {
            if (!parser.TryDecodeValue(_buffer, _currentIndex, out T value, out int bytesRead, out string error))
                throw new Exception("Unable to parse :" + error);

            return value;
        }

        public byte[] ReadBytesUntil(int index)
        {
            var length = index - _currentIndex;
            byte[] destination = new byte[length];
            Array.Copy(_buffer, index, destination, 0, length);
            _currentIndex += length;
            return destination;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] destination = new byte[count];
            Array.Copy(_buffer, _currentIndex, destination, 0, count);
            _currentIndex += count;
            return destination;
        }

        public void Advance(int byteCount)
        {
            _currentIndex += byteCount;
        }

        public void Read(IByteParser parser, out string value, out string error)
        {
            if (!parser.TryDecode(_buffer, _currentIndex, out  value, out int bytesRead, out error))
                throw new Exception("Unable to parse :" + error);

            _currentIndex += bytesRead;
        }

        public string ReadStringAscii() => Read(ByteParsers.StringAscii);
        public string ReadString() => Read(ByteParsers.String);
        public int ReadInt32() => Read(ByteParsers.Int32);
        public uint ReadUInt32() => Read(ByteParsers.UInt32);
        public long ReadInt64() => Read(ByteParsers.Int64);
        public float ReadSingle() => Read(ByteParsers.Single);
        public Half ReadFloat16() => Read(ByteParsers.Float16);
        public short ReadShort() => Read(ByteParsers.Short);
        public ushort ReadUShort() => Read(ByteParsers.UShort);
        public bool ReadBool() => Read(ByteParsers.Bool);
        public byte ReadByte() => Read(ByteParsers.Byte);
        


        public uint PeakUint32() => Peak(ByteParsers.UInt32);
        public long PeakInt64() => Peak(ByteParsers.Int64);


        public UnknownParseResult PeakUnknown()
        {
            var parsers = ByteParsers.GetAllParsers();
            var output = new List<string>();
            foreach (var parser in parsers)
            {
             
                    var result = parser.TryDecode(_buffer, _currentIndex, out string value, out int bytesRead, out string error);
                    if (!result)
                        output.Add($"{parser.TypeName} - Failed:{error}");
                    else
                        output.Add($"{parser.TypeName} - {value}");

            }

            return new UnknownParseResult() { Data = output.ToArray()};
        }


        public ByteChunk CreateSub(int size)
        {
            var data = ReadBytes(size);
            return new ByteChunk(data);
        }
        public string ReadFixedLength(int length) => ReadFixedLengthString(ByteParsers.String, length);
        public string ReadZeroTerminatedStr() => ReadZeroTerminatedString(ByteParsers.String);


        public class UnknownParseResult
        {
            public string[] Data { get; set; }
            public override string ToString()
            {
                var strOuput = "";
                if (Data != null)
                {
                    foreach (var s in Data)
                        strOuput += s + "\n";
                }
                return strOuput;

            }
        }


       
        public byte[] Debug_LookForDataAfterFixedStr(int size)
        {
            var dataCpy = new ByteChunk(ReadBytes(size));
            Index -= size;

            var str = dataCpy.ReadFixedLength(size);
            var strClean = Util.SanatizeFixedString(str);

            dataCpy.Reset();
            dataCpy.Index = strClean.Length;
            var bytesAfterClean = dataCpy.ReadBytes(dataCpy.BytesLeft);
            var nonZeroBytes = bytesAfterClean.Count(x => x != 0);
            if (nonZeroBytes != 0)
            {
                return bytesAfterClean;
            }

            return null;
        }


        public string Debug_LookForStrAfterFixedStr(int size)
        {
            var dataCpy = new ByteChunk(ReadBytes(size));
            Index -= size;

            var str = dataCpy.ReadFixedLength(size);
            var strClean = Util.SanatizeFixedString(str);

            dataCpy.Reset();
            dataCpy.Index = strClean.Length;
            var bytesAfterClean = dataCpy.ReadBytes(dataCpy.BytesLeft);
            var nonZeroBytes = bytesAfterClean.Count(x => x != 0);
            if (nonZeroBytes != 0)
            {
                dataCpy.Index = strClean.Length;
                var strAfterClean = dataCpy.ReadFixedLength(dataCpy.BytesLeft);
                return strAfterClean;
            }

            return null;
        }


        public override string ToString()
        {
            return $"ByteParser[Size = {_buffer?.Length}, index = {_currentIndex}]";
        }
    }
}
