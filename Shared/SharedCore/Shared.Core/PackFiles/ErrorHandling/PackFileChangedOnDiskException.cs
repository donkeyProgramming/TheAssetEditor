namespace Shared.Core.PackFiles.ErrorHandling
{
    // AE reads a pack's files from their offsets in the file on disk. Once another program
    // (RPFM, a second AE) rewrites the pack, those offsets point at other data.
    public class PackFileChangedOnDiskException(string packFilePath)
        : IOException($"'{Path.GetFileName(packFilePath)}' was changed on disk by another program after AssetEditor loaded it, " +
                      $"so AssetEditor can no longer read or save its files safely. Right-click the pack and choose \"Reload from disk\" to pick up the changes.\n\n{packFilePath}")
    {
        public string PackFilePath { get; } = packFilePath;
    }
}
