using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsing
{
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
        public static FixedAciiStringParser FixedAciiString1 { get; } = new FixedAciiStringParser(1);
        public static FixedStringParser FixedString1 { get; } = new FixedStringParser(1);

        public static IByteParser[] GetAllParsers() { return [Byte, Int32, Int64, UInt32, Single, Float16, Short, UShort, Bool, OptString, String, OptStringAscii, StringAscii, FixedAciiString1, FixedString1]; }

        public static IByteParser GetParser(DbTypesEnum value)
        {
            foreach (var parser in GetAllParsers())
            {
                if (parser.Type == value)
                    return parser;
            }

            throw new Exception($"No parser found for type {value}");
        }

    }
}
