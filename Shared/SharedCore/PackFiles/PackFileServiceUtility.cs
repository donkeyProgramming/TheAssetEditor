using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public static class PackFileServiceUtility
    {
        public static List<PackFile> FindAllFilesInDirectory(IPackFileService pfs, string dir, bool includeSubFolders = true)
        {
            dir = dir.Replace('/', '\\').ToLower();
            var output = new List<PackFile>();

            foreach (var pf in pfs.GetAllPackfileContainers())
            {
                foreach (var file in pf.FileList)
                {
                    var includeFile = false;
                    if (includeSubFolders)
                    {
                        includeFile = file.Key.IndexOf(dir) == 0;
                    }
                    else
                    {
                        var dirName = Path.GetDirectoryName(file.Key);
                        var compareResult = string.Compare(dirName, dir, StringComparison.InvariantCultureIgnoreCase);
                        if (compareResult == 0)
                            includeFile = true;
                    }

                    if (includeFile)
                        output.Add(file.Value);
                }
            }


            return output;
        }

        public static List<string> SearchForFile(IPackFileService pfs, string partOfFileName)
        {
            var output = new List<string>();
            foreach (var pf in pfs.GetAllPackfileContainers())
            {
                foreach (var file in pf.FileList)
                {
                    if (file.Key.Contains(partOfFileName, StringComparison.InvariantCultureIgnoreCase))
                        output.Add(file.Key);
                }
            }

            return output;
        }

        public static List<PackFile> GetAllAnimPacks(IPackFileService pfs)
        {
            var animPacks = FindAllWithExtention(pfs, @".animpack");
            var itemsToRemove = animPacks.Where(x => pfs.GetFullPath(x).Contains("animation_culture_packs", StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var item in itemsToRemove)
                animPacks.Remove(item);

            return animPacks;
        }


        public static List<PackFile> FindAllWithExtention(IPackFileService pfs, string extention, PackFileContainer? packFileContainer = null)
        {
            return FindAllWithExtentionIncludePaths(pfs, extention, packFileContainer).Select(x => x.Item2).ToList();
        }


        public static List<(string FileName, PackFile Pack)> FindAllWithExtentionIncludePaths(IPackFileService pfs, string extention, PackFileContainer? packFileContainer = null)
        {
            extention = extention.ToLower();
            var output = new List<ValueTuple<string, PackFile>>();
            if (packFileContainer == null)
            {
                foreach (var pf in pfs.GetAllPackfileContainers())
                {
                    foreach (var file in pf.FileList)
                    {
                        var fileExtention = Path.GetExtension(file.Key);
                        if (fileExtention == extention)
                            output.Add(new ValueTuple<string, PackFile>(file.Key, file.Value));
                    }
                }
            }
            else
            {
                foreach (var file in packFileContainer.FileList)
                {
                    var fileExtention = Path.GetExtension(file.Key);
                    if (fileExtention == extention)
                        output.Add(new ValueTuple<string, PackFile>(file.Key, file.Value));
                }
            }

            return output;
        }


    }
}
