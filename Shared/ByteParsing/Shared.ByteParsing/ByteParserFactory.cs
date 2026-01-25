using Microsoft.Xna.Framework;
using Shared.ByteParsing.Parsers;

namespace Shared.ByteParsing
{
    public static class ByteParserFactory
    {
        static Dictionary<Type, IByteParser>? s_typeToParserMap;
        static Dictionary<DbTypesEnum, IByteParser>? s_enumToParserMap;

        static void CreateTypeMap()
        {
            if (s_typeToParserMap != null)
                return;

            var items = new List<(DbTypesEnum DbEnum, Type type, IByteParser parser)>
            {
                (DbEnum: DbTypesEnum.String, typeof(string), ByteParsers.String),
                (DbEnum: DbTypesEnum.String_ascii, null, ByteParsers.StringAscii),
                (DbEnum: DbTypesEnum.FixedString, null, new FixedStringParser(1)),
                (DbEnum: DbTypesEnum.FixedStringAcii, null, new FixedAciiStringParser(1)),
                (DbEnum: DbTypesEnum.Optstring, null, ByteParsers.OptString),
                (DbEnum: DbTypesEnum.Optstring_ascii, null, ByteParsers.OptStringAscii),
                (DbEnum: DbTypesEnum.Short, typeof(short), ByteParsers.Short),
                (DbEnum: DbTypesEnum.UShort, typeof(ushort), ByteParsers.UShort),
                (DbEnum: DbTypesEnum.Integer, typeof(int), ByteParsers.Int32),
                (DbEnum: DbTypesEnum.uint32, typeof(uint), ByteParsers.UInt32),
                (DbEnum: DbTypesEnum.Int64, typeof(long), ByteParsers.Int64),
                (DbEnum: DbTypesEnum.Single, typeof(float), ByteParsers.Single),
                (DbEnum: DbTypesEnum.Float16, null, ByteParsers.Float16),
                (DbEnum: DbTypesEnum.Vector3, typeof(Vector3), ByteParsers.Vector3),
                (DbEnum: DbTypesEnum.Vector4, typeof(Vector4), ByteParsers.Vector4),
                (DbEnum: DbTypesEnum.Boolean, typeof(bool), ByteParsers.Bool),
                (DbEnum: DbTypesEnum.Byte, typeof(byte), ByteParsers.Byte)
            };

            s_typeToParserMap = [];
            s_enumToParserMap = [];

            foreach (var (dbEnum, type, parser) in items)
            {
                s_enumToParserMap.Add(dbEnum, parser);
                if (type != null)
                    s_typeToParserMap.Add(type, parser);
            }
        }


        public static IByteParser Create(Type type)
        {
            CreateTypeMap();
            return s_typeToParserMap![type];
        }
    }
}
