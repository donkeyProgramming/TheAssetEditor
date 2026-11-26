using System.IO;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.DB
{
    public record DbRow(IReadOnlyDictionary<string, object> Cells, int Version, string Pack, string File)
    {
        public string Text(string column) => Cells.TryGetValue(column, out var value) ? value as string ?? "" : "";
        public bool Flag(string column, bool missing = false) => Cells.TryGetValue(column, out var value) ? value is true : missing;
        public int Number(string column, int missing = 0) => Cells.TryGetValue(column, out var value) && value is int number ? number : missing;
    }

    public static class DbTables
    {
        public static List<DbRow> Read(IPackFileService pfs, string table, List<string>? problems = null)
        {
            var folder = @"db\" + table;
            var packs = pfs.GetAllPackfileContainers();
            // The game keeps a mod's row over CA's, and among mods the file whose name sorts first; one mod's file per path.
            // Pack names stand in for the mod list's order, which isn't known here.
            var mods = packs.Where(x => !x.IsCaPackFile).OrderBy(x => x.Name, StringComparer.Ordinal)
                .SelectMany(x => Files(x, folder))
                .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Name, StringComparer.Ordinal);
            var rows = new List<DbRow>();
            foreach (var (container, name) in mods.Concat(packs.Where(x => x.IsCaPackFile).SelectMany(x => Files(x, folder))))
            {
                var path = folder + @"\" + name;
                if (container.FindFile(path) is not { } file)
                    continue;
                var shown = path.Replace('\\', '/');
                try
                {
                    var (version, read) = DbTableReader.Read(file.DataSource.ReadData(), table, $"{container.Name}: {shown}");
                    rows.AddRange(read.Select(x => new DbRow(x, version, container.Name, shown)));
                }
                catch (InvalidDataException e) when (problems != null)
                {
                    problems.Add(e.Message);
                }
            }
            return rows;
        }

        static IEnumerable<(IPackFileContainer Pack, string Name)> Files(IPackFileContainer pack, string folder) =>
            pack.GetAllFilesByFolder().TryGetValue(folder, out var names) ? names.Select(x => (pack, x)) : [];
    }
}
