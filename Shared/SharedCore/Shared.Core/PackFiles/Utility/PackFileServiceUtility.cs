using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles.Utility
{
    public static class PackFileServiceUtility
    {
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
            {
                var container = PackFileService.CastContainer(packFileContainer);
                return container.FindAllWithExtention(extention);
            }

            var output = new List<(string, PackFile)>();
            foreach (var pf in pfs.GetAllPackfileContainers())
            {
                var container = PackFileService.CastContainer(pf);
                output.AddRange(container.FindAllWithExtention(extention));
            }
            return output;
        }


    }
}
