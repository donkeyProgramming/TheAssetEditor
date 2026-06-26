namespace Shared.Core.PackFiles.Utility
{
    public static class PackFileSortHelper
    {
        public static readonly StringComparer PathComparer = StringComparer.Ordinal;

        public static void SortFileList(List<string> fileNames)
        {
            fileNames.Sort(PathComparer);
        }

        public static SortedDictionary<string, List<string>> BuildSortedFilesByFolder(IEnumerable<string> fullPaths)
        {
            var result = new SortedDictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var fullPath in fullPaths)
            {
                var lastSep = fullPath.LastIndexOf('\\');
                var folder = lastSep == -1 ? string.Empty : fullPath.Substring(0, lastSep);
                var fileName = lastSep == -1 ? fullPath : fullPath.Substring(lastSep + 1);

                if (!result.TryGetValue(folder, out var files))
                {
                    files = new List<string>();
                    result[folder] = files;
                }

                files.Add(fileName);
            }

            foreach (var files in result.Values)
                SortFileList(files);

            return result;
        }
    }
}
