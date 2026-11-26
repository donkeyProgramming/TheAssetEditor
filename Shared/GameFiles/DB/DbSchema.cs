using Shared.EmbeddedResources;

namespace Shared.GameFormats.DB
{
    public record DbField(string Name, string Type, bool IsKey);

    public static class DbSchema
    {
        static Dictionary<string, Dictionary<int, List<DbField>>>? _tables;

        public static IReadOnlyDictionary<int, List<DbField>> Versions(string table) =>
            (_tables ??= Parse(ResourceLoader.LoadString("Resources.Db.schema_wh3_extract.txt"))).TryGetValue(table, out var versions)
                ? versions
                : new Dictionary<int, List<DbField>>();

        public static Dictionary<string, Dictionary<int, List<DbField>>> Parse(string text)
        {
            var tables = new Dictionary<string, Dictionary<int, List<DbField>>>();
            List<DbField>? fields = null;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;
                var cells = line.TrimStart('@').Split(';');
                if (line.StartsWith('@'))
                {
                    if (!tables.TryGetValue(cells[0], out var versions))
                        tables[cells[0]] = versions = [];
                    versions[int.Parse(cells[1])] = fields = [];
                }
                else
                {
                    fields!.Add(new DbField(cells[0], cells[1], cells[2] == "1"));
                }
            }
            return tables;
        }
    }
}
