using Microsoft.Xna.Framework;

namespace Shared.Core.ByteParsing
{
    public static class ByteParserFactory
    {
        static Dictionary<Type, IByteParser> _typeToParserMap;
        static Dictionary<DbTypesEnum, IByteParser> _enumToParserMap;

        static void CreateTypeMap()
        {
            if (_typeToParserMap != null)
                return;

            var items = new List<(DbTypesEnum DbEnum, Type type, IByteParser parser)>();
            items.Add((DbTypesEnum.String, typeof(string), ByteParsers.String));
            items.Add((DbTypesEnum.String_ascii, null, ByteParsers.StringAscii));
            items.Add((DbTypesEnum.FixedString, null, new FixedStringParser(1)));
            items.Add((DbTypesEnum.FixedStringAcii, null, new FixedAciiStringParser(1)));
            items.Add((DbTypesEnum.Optstring, null, ByteParsers.OptString));
            items.Add((DbTypesEnum.Optstring_ascii, null, ByteParsers.OptStringAscii));

            items.Add((DbTypesEnum.Short, typeof(short), ByteParsers.Short));
            items.Add((DbTypesEnum.UShort, typeof(ushort), ByteParsers.UShort));
            items.Add((DbTypesEnum.Integer, typeof(int), ByteParsers.Int32));
            items.Add((DbTypesEnum.uint32, typeof(uint), ByteParsers.UInt32));
            items.Add((DbTypesEnum.Int64, typeof(long), ByteParsers.Int64));

            items.Add((DbTypesEnum.Single, typeof(float), ByteParsers.Single));
            items.Add((DbTypesEnum.Float16, null, ByteParsers.Float16));

            items.Add((DbTypesEnum.Vector3, typeof(Vector3), ByteParsers.Vector3));
            items.Add((DbTypesEnum.Vector4, typeof(Vector4), ByteParsers.Vector4));

            items.Add((DbTypesEnum.Boolean, typeof(bool), ByteParsers.Bool));
            items.Add((DbTypesEnum.Byte, typeof(byte), ByteParsers.Byte));

            _typeToParserMap = new Dictionary<Type, IByteParser>();
            _enumToParserMap = new Dictionary<DbTypesEnum, IByteParser>();

            foreach (var item in items)
            {
                _enumToParserMap.Add(item.DbEnum, item.parser);
                if (item.type != null)
                    _typeToParserMap.Add(item.type, item.parser);
            }
        }

        public static IByteParser Create(DbTypesEnum typeEnum)
        {
            CreateTypeMap();
            return _enumToParserMap[typeEnum];
        }

        public static IByteParser Create(Type type)
        {
            CreateTypeMap();
            return _typeToParserMap[type];
        }
    }
}
