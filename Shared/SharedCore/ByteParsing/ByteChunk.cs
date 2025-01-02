using Shared.Core.Misc;
using Half = SharpDX.Half;

namespace Shared.Core.ByteParsing
{
    public class ByteChunk
    {
        readonly byte[] _buffer;

        int _currentIndex = 0;
        public int CurrentIndex
        {
            get => _currentIndex;
            private set
            {
                _currentIndex = value;
                if (_currentIndex < 0)
                    throw new IndexOutOfRangeException();
            }
        }
        public ByteChunk(byte[] buffer, int index = 0)
        {
            _buffer = buffer;
            CurrentIndex = index;
        }

        public void Reset()
        {
            Index = 0;
        }


        public void SaveToFile(string path)
        {
            var folder = Path.GetDirectoryName(path);
            DirectoryHelper.EnsureCreated(folder);

            File.WriteAllBytes(path, _buffer);
        }

        public static ByteChunk FromFile(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);
            return new ByteChunk(bytes);
        }

        public int BytesLeft => _buffer.Length - CurrentIndex;
        public int Index { get { return CurrentIndex; } set { CurrentIndex = value; } }

        public byte[] Buffer { get { return _buffer; } }

        T Read<T>(SpesificByteParser<T> parser)
        {
            if (!parser.TryDecodeValue(_buffer, CurrentIndex, out var value, out var bytesRead, out var error))
                throw new Exception("Unable to parse :" + error);

            CurrentIndex += bytesRead;
            return value;
        }

        string ReadFixedLengthString(StringParser parser, int length)
        {
            if (!parser.TryDecodeFixedLength(_buffer, CurrentIndex, length, out var value, out var bytesRead))
                throw new Exception("Unable to parse");

            CurrentIndex += bytesRead;
            return value;
        }

        string ReadZeroTerminatedString(StringParser parser)
        {
            if (!parser.TryDecodeZeroTerminatedString(_buffer, CurrentIndex, out var value, out var bytesRead))
                throw new Exception("Unable to parse");

            CurrentIndex += bytesRead;
            return value;

        }

        T Peak<T>(SpesificByteParser<T> parser)
        {
            if (!parser.TryDecodeValue(_buffer, CurrentIndex, out var value, out var bytesRead, out var error))
                throw new Exception("Unable to parse :" + error);

            return value;
        }

        public byte[] ReadBytesUntil(int index)
        {
            var length = index - CurrentIndex;
            var destination = new byte[length];
            Array.Copy(_buffer, index, destination, 0, length);
            CurrentIndex += length;
            return destination;
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = GetBytesFromBuffer(CurrentIndex, count);
            CurrentIndex += count;
            return bytes;
        }

        public sbyte[] ReadSBytes(int count)
        {
            var destination = new sbyte[count];
            Array.Copy(_buffer, CurrentIndex, destination, 0, count);
            CurrentIndex += count;
            return destination;
        }

        public byte[] GetBytesFromBuffer(int start, int count)
        {
            var destination = new byte[count];
            Array.Copy(_buffer, start, destination, 0, count);

            return destination;
        }

        public void Advance(int byteCount)
        {
            CurrentIndex += byteCount;
        }

        public void Read(IByteParser parser, out string value, out string error)
        {
            if (!parser.TryDecode(_buffer, CurrentIndex, out value, out var bytesRead, out error))
                throw new Exception("Unable to parse :" + error);

            CurrentIndex += bytesRead;
        }

        public void Read<T>(SpesificByteParser<T> parser, out T value, out string error)
        {
            if (!parser.TryDecodeValue(_buffer, CurrentIndex, out value, out var bytesRead, out error))
                throw new Exception("Unable to parse :" + error);

            CurrentIndex += bytesRead;
        }

        public string ReadStringAscii() => Read(ByteParsers.StringAscii);
        public string ReadString() => Read(ByteParsers.String);
        public string ReadOptionalString() => Read(ByteParsers.OptString);
        public int ReadInt32() => Read(ByteParsers.Int32);
        public uint ReadUInt32() => Read(ByteParsers.UInt32);
        public long ReadInt64() => Read(ByteParsers.Int64);
        public float ReadSingle() => Read(ByteParsers.Single);
        public Half ReadFloat16() => Read(ByteParsers.Float16);
        public short ReadShort() => Read(ByteParsers.Short);
        public ushort ReadUShort() => Read(ByteParsers.UShort);
        public bool ReadBool() => Read(ByteParsers.Bool);
        public byte ReadByte() => Read(ByteParsers.Byte);
        public string ReadStringTableIndex(IEnumerable<string> stringTable) => stringTable.ElementAt(ReadInt32());



        public uint PeakUint32() => Peak(ByteParsers.UInt32);
        public long PeakInt64() => Peak(ByteParsers.Int64);
        public byte PeakByte() => Peak(ByteParsers.Byte);


        public object ReadObject(DbTypesEnum type)
        {
            var result = ByteParserFactory.Create(type).GetValueAsObject(_buffer, CurrentIndex, out var byteRead);
            CurrentIndex += byteRead;
            return result;
        }

        public UnknownParseResult PeakUnknown()
        {
            var parsers = ByteParsers.GetAllParsers();
            var output = new List<UnknownParseResult.Item>();
            foreach (var parser in parsers)
            {
                var result = parser.TryDecode(_buffer, CurrentIndex, out var value, out var bytesRead, out var error);
                var item = new UnknownParseResult.Item()
                {
                    Result = result,
                    ErrorMessage = error,
                    Value = value,
                    Type = parser.Type,
                };

                output.Add(item);
            }

            return new UnknownParseResult(output.ToArray());
        }

        public UnknownParseResult[] PeakUnknown(int numBytes)
        {
            var index = CurrentIndex;

            var output = new UnknownParseResult[numBytes];
            for (var i = 0; i < numBytes; i++)
            {
                output[i] = PeakUnknown();
                ReadByte();
            }

            CurrentIndex = index;
            return output;
        }

        public ByteChunk CreateSub(int size)
        {
            var data = ReadBytes(size);
            return new ByteChunk(data);
        }


        public ByteChunk PeekChunk(int size)
        {
            var currentIndex = CurrentIndex;
            var chunk = CreateSub(size);
            CurrentIndex = currentIndex;
            return chunk;
        }

        public string ReadFixedLength(int length) => ReadFixedLengthString(ByteParsers.String, length);
        public string ReadZeroTerminatedStr() => ReadZeroTerminatedString(ByteParsers.String);


        public class UnknownParseResult
        {
            public Item[] Data { get; private set; }

            public UnknownParseResult(Item[] data)
            {
                Data = data;
            }

            public override string ToString()
            {
                return string.Join(" \n", Data.Select(x => x.DisplayStr()));
            }

            public class Item
            {
                public bool Result { get; set; }
                public string Value { get; set; } = "";
                public string ErrorMessage { get; set; } = "";
                public DbTypesEnum Type { get; set; }

                public string DisplayStr()
                {
                    if (!Result)
                        return $"{Type} - Failed:{ErrorMessage}";
                    else
                        return $"{Type} - {Value}";
                }

                public override string ToString()
                {
                    return DisplayStr();
                }
            }
        }


        public byte[]? Debug_LookForDataAfterFixedStr(int size)
        {
            var dataCpy = new ByteChunk(ReadBytes(size));
            Index -= size;

            var str = dataCpy.ReadFixedLength(size);
            var strClean = StringSanitizer.FixedString(str);

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

        public string? Debug_LookForStrAfterFixedStr(int size)
        {
            var dataCpy = new ByteChunk(ReadBytes(size));
            Index -= size;

            var str = dataCpy.ReadFixedLength(size);
            var strClean = StringSanitizer.FixedString(str);

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
            return $"ByteParser[Size = {_buffer?.Length}, index = {CurrentIndex}]";
        }
    }
}
