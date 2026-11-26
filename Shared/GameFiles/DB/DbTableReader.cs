using System.IO;
using System.Text;

namespace Shared.GameFormats.DB
{
    public static class DbTableReader
    {
        static readonly byte[] GuidMarker = [0xFD, 0xFE, 0xFC, 0xFF];
        static readonly byte[] VersionMarker = [0xFC, 0xFD, 0xFE, 0xFF];

        public static (int Version, List<Dictionary<string, object>> Rows) Read(byte[] data, string table, string file)
        {
            var p = 0;
            if (Starts(data, p, GuidMarker))
                p += 6 + BitConverter.ToUInt16(data, p + 4) * 2;
            var version = 0;
            if (Starts(data, p, VersionMarker))
            {
                version = BitConverter.ToInt32(data, p + 4);
                p += 8;
            }
            if (!DbSchema.Versions(table).TryGetValue(version, out var fields))
                throw new InvalidDataException($"{file}: {table} v{version} is not in the schema extract.");

            var rows = new List<Dictionary<string, object>>();
            try
            {
                p += 1;
                var count = BitConverter.ToInt32(data, p);
                p += 4;
                for (var i = 0; i < count; i++)
                {
                    var row = new Dictionary<string, object>(fields.Count);
                    foreach (var field in fields)
                        row[field.Name] = Cell(data, ref p, field.Type, table);
                    rows.Add(row);
                }
            }
            catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException)
            {
                throw new InvalidDataException($"{file}: {table} v{version} runs past the end of the file.");
            }
            if (p != data.Length)
                throw new InvalidDataException($"{file}: {table} v{version} decoded {p} of {data.Length} bytes.");
            return (version, rows);
        }

        static bool Starts(byte[] data, int at, byte[] marker) => data.Length >= at + marker.Length && data.AsSpan(at, marker.Length).SequenceEqual(marker);

        static object Cell(byte[] data, ref int p, string type, string table)
        {
            switch (type)
            {
                case "OptionalStringU8":
                    return data[p++] == 0 ? "" : Text(data, ref p);
                case "StringU8":
                    return Text(data, ref p);
                case "Boolean":
                    return data[p++] != 0;
                case "I32":
                    p += 4;
                    return BitConverter.ToInt32(data, p - 4);
                case "F32":
                    p += 4;
                    return BitConverter.ToSingle(data, p - 4);
                case "F64":
                    p += 8;
                    return BitConverter.ToDouble(data, p - 8);
                default:
                    throw new NotSupportedException($"{table}: the field type {type} is not read; DbTableReader reads the six the extract's tables use.");
            }
        }

        static string Text(byte[] data, ref int p)
        {
            var length = BitConverter.ToUInt16(data, p);
            var text = Encoding.UTF8.GetString(data, p + 2, length);
            p += 2 + length;
            return text;
        }
    }
}
