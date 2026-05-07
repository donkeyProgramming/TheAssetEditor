using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles.Utility
{
    public sealed record DirectoryFileEntry(string FileName, PackFile File);

    public sealed record DirectoryEntriesSplitResult(
        string DirectoryPath,
        IReadOnlyList<string> SubFolders,
        IReadOnlyList<DirectoryFileEntry> Files);

    public static class PackFileServiceUtility
    {
        public static DirectoryEntriesSplitResult SplitDirectoryEntries(IPackFileContainer container, string directoryPath)
        {
            var files = container.GetDirectoryContent(directoryPath)
                .Select(entry => new DirectoryFileEntry(entry.File.Name, entry.File))
                .OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var subFolders = container.GetSubDirectories(directoryPath)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new DirectoryEntriesSplitResult(
                directoryPath,
                subFolders,
                files);
        }

        public static List<PackFile> GetAllAnimPacks(IPackFileService pfs)
        {
            var animPacks = FindAllWithExtention(pfs, @".animpack");
            var itemsToRemove = animPacks.Where(x => pfs.GetFullPath(x).Contains("animation_culture_packs", StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var item in itemsToRemove)
                animPacks.Remove(item);

            return animPacks;
        }

        public static List<PackFile> FindAllWithExtention(IPackFileService pfs, string extention, IPackFileContainer? packFileContainer = null)
        {
            return FindAllWithExtentionIncludePaths(pfs, extention, packFileContainer).Select(x => x.Item2).ToList();
        }

        public static List<(string FileName, PackFile Pack)> FindAllWithExtentionIncludePaths(IPackFileService pfs, string extention, IPackFileContainer? packFileContainer = null)
        {
            if (packFileContainer != null)
                return packFileContainer.FindAllWithExtention(extention);

            var output = new List<(string, PackFile)>();
            foreach (var pf in pfs.GetAllPackfileContainers())
                output.AddRange(pf.FindAllWithExtention(extention));
            return output;
        }


    }
}
