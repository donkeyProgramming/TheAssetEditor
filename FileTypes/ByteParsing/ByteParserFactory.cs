using System;

namespace Filetypes.ByteParsing
{
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

                case DbTypesEnum.Vector3:
                    return ByteParsers.Vector3;

                case DbTypesEnum.Vector4:
                    return ByteParsers.Vector4;

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
    }
}
